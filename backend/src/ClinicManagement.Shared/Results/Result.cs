namespace ClinicManagement.Shared.Results;

/// <summary>
/// Kết quả của một thao tác không trả dữ liệu — thành công hoặc kèm <see cref="Error"/>.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Kết quả thành công không thể mang lỗi.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Kết quả thất bại phải mang lỗi.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>
/// Kết quả mang dữ liệu kiểu <typeparamref name="T"/> khi thành công.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    /// <summary>Giá trị kết quả; ném lỗi nếu truy cập khi thất bại.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không thể truy cập Value của kết quả thất bại.");

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
