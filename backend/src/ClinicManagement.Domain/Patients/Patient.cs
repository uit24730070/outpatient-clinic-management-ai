using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Patients;

/// <summary>
/// Hồ sơ bệnh nhân. Mã bệnh nhân (<see cref="Code"/>) là định danh nghiệp vụ duy nhất.
/// </summary>
public class Patient : Entity
{
    // EF Core cần constructor không tham số.
    private Patient() { }

    public Patient(
        string code,
        string fullName,
        DateOnly? dateOfBirth,
        Gender gender,
        string? phoneNumber,
        string? address)
    {
        Code = code;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    /// <summary>Mã bệnh nhân duy nhất, ví dụ BN-000001.</summary>
    public string Code { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public DateOnly? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }

    /// <summary>Cập nhật các trường có thể chỉnh sửa của hồ sơ.</summary>
    public void UpdateDetails(
        string fullName,
        DateOnly? dateOfBirth,
        Gender gender,
        string? phoneNumber,
        string? address)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        PhoneNumber = phoneNumber;
        Address = address;
    }
}
