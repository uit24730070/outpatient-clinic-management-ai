namespace ClinicManagement.Shared.Contracts;

/// <summary>
/// Envelope thống nhất cho mọi phản hồi API. Thành công có <see cref="Data"/>,
/// thất bại có <see cref="Error"/>.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public object? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, object? meta = null)
        => new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(ApiError error)
        => new() { Success = false, Error = error };
}

/// <summary>
/// Thông tin lỗi trả về client. <see cref="Details"/> chứa lỗi validation theo từng trường.
/// </summary>
public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }
}
