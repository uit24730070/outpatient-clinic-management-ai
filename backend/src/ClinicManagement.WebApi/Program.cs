using System.Text;
using System.Text.Json;
using ClinicManagement.Application;
using ClinicManagement.Infrastructure;
using ClinicManagement.Infrastructure.Authentication;
using ClinicManagement.Infrastructure.Persistence;
using ClinicManagement.Shared.Contracts;
using ClinicManagement.WebApi.Filters;
using ClinicManagement.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging có cấu trúc (Serilog): đọc cấu hình từ appsettings, ghi ra console.
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

const string CorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

// Đăng ký dịch vụ theo từng lớp.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health check: kiểm tra kết nối tới CSDL qua DbContext.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "database");

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// Đưa lỗi model-binding/model-state về cùng envelope ApiResponse.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var body = ApiResponse<object>.Fail(new ApiError
        {
            Code = "Validation.Failed",
            Message = "Dữ liệu đầu vào không hợp lệ.",
            Details = details
        });

        return new BadRequestObjectResult(body);
    };
});

// Xác thực JWT (Bearer). Đọc cấu hình từ section "Jwt".
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Thiếu cấu hình 'Jwt' trong appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Giữ chuẩn envelope ApiResponse cho 401/403 (mặc định JwtBearer trả body rỗng).
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteAuthErrorAsync(context.Response, StatusCodes.Status401Unauthorized,
                    "Auth.Unauthorized", "Bạn cần đăng nhập để truy cập tài nguyên này.");
            },
            OnForbidden = async context =>
                await WriteAuthErrorAsync(context.Response, StatusCodes.Status403Forbidden,
                    "Auth.Forbidden", "Bạn không có quyền truy cập tài nguyên này.")
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Tự động áp migration khi bật cấu hình (Database:AutoMigrate) — dùng cho Docker/triển khai.
// Mặc định tắt để không ảnh hưởng luồng dev/test (áp migration bằng dotnet ef thủ công).
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Ghi log tóm tắt mỗi request HTTP (method, path, status, thời lượng).
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint health check: GET /health -> "Healthy" khi DB kết nối được.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Ghi lỗi xác thực/phân quyền theo envelope ApiResponse thống nhất.
static async Task WriteAuthErrorAsync(HttpResponse response, int statusCode, string code, string message)
{
    if (response.HasStarted) return;

    response.StatusCode = statusCode;
    response.ContentType = "application/json";

    var body = ApiResponse<object>.Fail(new ApiError { Code = code, Message = message });
    var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    await response.WriteAsync(json);
}
