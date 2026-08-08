using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Doctors;

/// <summary>
/// Hồ sơ bác sĩ. Mã bác sĩ (<see cref="Code"/>) là định danh nghiệp vụ duy nhất,
/// thuộc một chuyên khoa qua khoá ngoại <see cref="SpecialtyId"/>.
/// </summary>
public class Doctor : Entity
{
    // EF Core cần constructor không tham số.
    private Doctor() { }

    public Doctor(
        string code,
        string fullName,
        Guid specialtyId,
        string? phoneNumber,
        string? email)
    {
        Code = code;
        FullName = fullName;
        SpecialtyId = specialtyId;
        PhoneNumber = phoneNumber;
        Email = email;
    }

    /// <summary>Mã bác sĩ duy nhất, ví dụ BS-000001.</summary>
    public string Code { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public Guid SpecialtyId { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    /// <summary>Cập nhật các trường có thể chỉnh sửa của hồ sơ bác sĩ.</summary>
    public void UpdateDetails(
        string fullName,
        Guid specialtyId,
        string? phoneNumber,
        string? email)
    {
        FullName = fullName;
        SpecialtyId = specialtyId;
        PhoneNumber = phoneNumber;
        Email = email;
    }
}
