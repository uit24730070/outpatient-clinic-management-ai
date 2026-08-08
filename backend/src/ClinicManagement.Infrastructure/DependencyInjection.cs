using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Infrastructure.Ai;
using ClinicManagement.Infrastructure.Authentication;
using ClinicManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Thiếu chuỗi kết nối 'Default' trong cấu hình.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npg => npg.UseVector()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IEncounterEmbeddingStore, PgEncounterEmbeddingStore>();

        // Xác thực: cấu hình JWT + băm mật khẩu + phát token.
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Trợ lý AI/LLM: chọn hiện thực theo cấu hình (fake khi thiếu khoá hoặc bật UseFake).
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        var aiSettings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>() ?? new AiSettings();

        if (aiSettings.IsRealClientConfigured)
        {
            // Giữ một HttpClient dùng lại toàn vòng đời (tránh cạn socket) — không cần AddHttpClient.
            services.AddSingleton<IChatCompletionService>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<AiSettings>>();
                var http = new HttpClient
                {
                    BaseAddress = new Uri(opts.Value.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(opts.Value.TimeoutSeconds)
                };
                return new ClaudeChatCompletionService(http, opts);
            });
        }
        else
        {
            services.AddSingleton<IChatCompletionService, FakeChatCompletionService>();
        }

        // Embedding cho RAG: client Voyage thật khi có khoá, ngược lại fake (vector tất định).
        if (aiSettings.IsRealEmbeddingConfigured)
        {
            services.AddSingleton<IEmbeddingService>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<AiSettings>>();
                var http = new HttpClient
                {
                    BaseAddress = new Uri(opts.Value.EmbeddingBaseUrl),
                    Timeout = TimeSpan.FromSeconds(opts.Value.TimeoutSeconds)
                };
                return new VoyageEmbeddingService(http, opts);
            });
        }
        else
        {
            services.AddSingleton<IEmbeddingService>(
                new FakeEmbeddingService(AiSettings.EmbeddingDimensions));
        }

        return services;
    }
}
