namespace ClinicManagement.Application.Rooms.Dtos;

/// <summary>Dữ liệu phòng khám trả về cho client.</summary>
public sealed record RoomDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
