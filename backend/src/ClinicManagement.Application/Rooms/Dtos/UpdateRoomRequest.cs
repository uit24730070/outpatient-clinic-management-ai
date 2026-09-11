namespace ClinicManagement.Application.Rooms.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật phòng khám.</summary>
public sealed record UpdateRoomRequest(
    string Name,
    string? Description);
