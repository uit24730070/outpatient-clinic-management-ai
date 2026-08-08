using System.Text.Json;
using ClinicManagement.Shared.Contracts;
using FluentValidation;

namespace ClinicManagement.WebApi.Middleware;

/// <summary>
/// Bắt mọi ngoại lệ chưa xử lý và trả về envelope <see cref="ApiResponse{T}"/> chuẩn.
/// Ánh xạ <see cref="ValidationException"/> của FluentValidation sang 400 kèm chi tiết theo trường.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var details = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteAsync(context, StatusCodes.Status400BadRequest, new ApiError
            {
                Code = "Validation.Failed",
                Message = "Dữ liệu đầu vào không hợp lệ.",
                Details = details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ chưa xử lý khi xử lý {Path}", context.Request.Path);

            await WriteAsync(context, StatusCodes.Status500InternalServerError, new ApiError
            {
                Code = "Server.Error",
                Message = "Đã xảy ra lỗi không mong muốn."
            });
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, ApiError error)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = ApiResponse<object>.Fail(error);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
