using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicManagement.WebApi.Filters;

/// <summary>
/// Tự động validate các tham số action bằng FluentValidation nếu có validator tương ứng.
/// Ném <see cref="ValidationException"/> để middleware trả về 400 chuẩn.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }

        await next();
    }
}
