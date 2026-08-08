namespace ClinicManagement.Application.Common.Interfaces;

/// <summary>Trừu tượng băm & xác minh mật khẩu (hiện thực BCrypt ở Infrastructure).</summary>
public interface IPasswordHasher
{
    /// <summary>Băm mật khẩu thô thành chuỗi lưu trữ (đã kèm salt).</summary>
    string Hash(string password);

    /// <summary>Xác minh mật khẩu thô có khớp với chuỗi băm đã lưu.</summary>
    bool Verify(string password, string passwordHash);
}
