namespace ClinicManagement.Shared.Results;

/// <summary>
/// Phân loại lỗi nghiệp vụ, dùng để ánh xạ sang HTTP status code ở lớp WebApi.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
