using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Rooms.Dtos;
using ClinicManagement.Domain.Resources;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Rooms;

public sealed class RoomService : IRoomService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public RoomService(IAppDbContext db) => _db = db;

    public async Task<Result<RoomDto>> CreateAsync(
        CreateRoomRequest request, CancellationToken ct = default)
    {
        var code = await GenerateCodeAsync(ct);
        var room = new Room(code, request.Name.Trim(), NormalizeOptional(request.Description));

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);

        return ToDto(room);
    }

    public async Task<Result<PagedResult<RoomDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Rooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(term) ||
                r.Code.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await Project(ApplySort(query, sortBy, sortDesc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<RoomDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định theo Mã (giữ hành vi cũ).</summary>
    private static IOrderedQueryable<Room> ApplySort(IQueryable<Room> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "code" when desc => query.OrderByDescending(r => r.Code),
            "name" => desc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            _ => query.OrderBy(r => r.Code),
        };

    public async Task<Result<RoomDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await Project(_db.Rooms.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync(ct);
        return dto is null
            ? Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {id}.")
            : dto;
    }

    public async Task<Result<RoomDto>> UpdateAsync(
        Guid id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null)
            return Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {id}.");

        room.UpdateDetails(request.Name.Trim(), NormalizeOptional(request.Description));
        await _db.SaveChangesAsync(ct);
        return ToDto(room);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null)
            return Result.Failure(Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {id}."));

        room.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static IQueryable<RoomDto> Project(IQueryable<Room> query) =>
        query.Select(r => new RoomDto(r.Id, r.Code, r.Name, r.Description, r.CreatedAt, r.UpdatedAt));

    private static RoomDto ToDto(Room r) =>
        new(r.Id, r.Code, r.Name, r.Description, r.CreatedAt, r.UpdatedAt);

    /// <summary>Sinh mã phòng dạng PK-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Rooms.IgnoreQueryFilters().CountAsync(ct);
        return $"PK-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
