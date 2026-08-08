using ClinicManagement.Application;
using ClinicManagement.Infrastructure;
using ClinicManagement.Infrastructure.Persistence;
using ClinicManagement.Shared.Contracts;
using ClinicManagement.WebApi.Filters;
using ClinicManagement.WebApi.Middleware;
using Microsoft.AspNetCore.Mvc;
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

app.UseAuthorization();

app.MapControllers();

// Endpoint health check: GET /health -> "Healthy" khi DB kết nối được.
app.MapHealthChecks("/health");

app.Run();
