using ClinicManagement.Application.Rooms.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Rooms;

public interface IRoomService
{
    Task<Result<RoomDto>> CreateAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<RoomDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default);
    Task<Result<RoomDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<RoomDto>> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
