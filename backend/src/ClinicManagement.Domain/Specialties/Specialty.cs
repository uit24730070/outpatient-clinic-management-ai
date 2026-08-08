using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Specialties;

/// <summary>
/// Chuyên khoa khám chữa bệnh (bảng tra cứu). <see cref="Name"/> là định danh nghiệp vụ duy nhất.
/// </summary>
public class Specialty : Entity
{
    // EF Core cần constructor không tham số.
    private Specialty() { }

    public Specialty(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>Cập nhật thông tin chuyên khoa.</summary>
    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
