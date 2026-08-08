using ClinicManagement.Shared.Contracts;
using ClinicManagement.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Ánh xạ <see cref="Result{T}"/> sang phản hồi HTTP với envelope <see cref="ApiResponse{T}"/>.</summary>
    protected IActionResult ToResponse<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatusCode, ApiResponse<T>.Ok(result.Value));

        return Problem(result.Error);
    }

    private IActionResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var body = ApiResponse<object>.Fail(new ApiError
        {
            Code = error.Code,
            Message = error.Message
        });

        return StatusCode(status, body);
    }
}
