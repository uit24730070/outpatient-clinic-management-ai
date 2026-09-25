using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.Domain.Specialties;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Specialties;

public sealed class SpecialtyService : ISpecialtyService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public SpecialtyService(IAppDbContext db) => _db = db;

    public async Task<Result<SpecialtyDto>> CreateAsync(CreateSpecialtyRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await NameExistsAsync(name, null, ct))
            return Error.Conflict("Specialty.NameConflict", $"Chuyên khoa '{name}' đã tồn tại.");

        var specialty = new Specialty(name, NormalizeOptional(request.Description));

        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync(ct);

        return SpecialtyDto.FromEntity(specialty);
    }

    public async Task<Result<PagedResult<SpecialtyDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Specialties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await ApplySort(query, sortBy, sortDesc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => SpecialtyDto.FromEntity(s))
            .ToListAsync(ct);

        return new PagedResult<SpecialtyDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định theo Tên (giữ hành vi cũ).</summary>
    private static IOrderedQueryable<Specialty> ApplySort(IQueryable<Specialty> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "name" when desc => query.OrderByDescending(s => s.Name),
            "createdAt" => desc ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name),
        };

    public async Task<Result<SpecialtyDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var specialty = await _db.Specialties.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return specialty is null
            ? Error.NotFound("Specialty.NotFound", $"Không tìm thấy chuyên khoa với Id {id}.")
            : SpecialtyDto.FromEntity(specialty);
    }

    public async Task<Result<SpecialtyDto>> UpdateAsync(
        Guid id, UpdateSpecialtyRequest request, CancellationToken ct = default)
    {
        var specialty = await _db.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (specialty is null)
            return Error.NotFound("Specialty.NotFound", $"Không tìm thấy chuyên khoa với Id {id}.");

        var name = request.Name.Trim();
        if (await NameExistsAsync(name, id, ct))
            return Error.Conflict("Specialty.NameConflict", $"Chuyên khoa '{name}' đã tồn tại.");

        specialty.UpdateDetails(name, NormalizeOptional(request.Description));

        await _db.SaveChangesAsync(ct);
        return SpecialtyDto.FromEntity(specialty);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var specialty = await _db.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (specialty is null)
            return Result.Failure(Error.NotFound("Specialty.NotFound", $"Không tìm thấy chuyên khoa với Id {id}."));

        specialty.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Kiểm tra tên chuyên khoa đã tồn tại (không phân biệt hoa/thường), bỏ qua chính bản ghi đang sửa.</summary>
    private async Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var lowered = name.ToLower();
        return await _db.Specialties
            .AnyAsync(s => s.Name.ToLower() == lowered && (excludeId == null || s.Id != excludeId), ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
