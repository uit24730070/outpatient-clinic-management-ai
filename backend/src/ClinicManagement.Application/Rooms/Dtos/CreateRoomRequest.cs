namespace ClinicManagement.Application.Rooms.Dtos;

/// <summary>Dữ liệu đầu vào để tạo phòng khám mới.</summary>
public sealed record CreateRoomRequest(
    string Name,
    string? Description);
