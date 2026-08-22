using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Medications.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Medications;

public sealed class MedicationService : IMedicationService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public MedicationService(IAppDbContext db) => _db = db;

    public async Task<Result<MedicationDto>> CreateAsync(
        CreateMedicationRequest request, CancellationToken ct = default)
    {
        var code = await GenerateCodeAsync(ct);
        var medication = new Medication(
            code,
            request.Name.Trim(),
            request.ActiveIngredient.Trim(),
            request.Unit.Trim(),
            request.ReorderLevel,
            NormalizeOptional(request.Description));

        _db.Medications.Add(medication);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(medication.Id, ct))!;
    }

    public async Task<Result<PagedResult<MedicationDto>>> GetListAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Medications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                m.Code.ToLower().Contains(term) ||
                m.ActiveIngredient.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(m => m.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MedicationDto>(items, page, pageSize, total);
    }

    public async Task<Result<MedicationDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Medication.NotFound", $"Không tìm thấy thuốc với Id {id}.")
            : dto;
    }

    public async Task<Result<MedicationDto>> UpdateAsync(
        Guid id, UpdateMedicationRequest request, CancellationToken ct = default)
    {
        var medication = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medication is null)
            return Error.NotFound("Medication.NotFound", $"Không tìm thấy thuốc với Id {id}.");

        medication.UpdateDetails(
            request.Name.Trim(),
            request.ActiveIngredient.Trim(),
            request.Unit.Trim(),
            request.ReorderLevel,
            NormalizeOptional(request.Description));

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(medication.Id, ct))!;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var medication = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medication is null)
            return Result.Failure(Error.NotFound("Medication.NotFound", $"Không tìm thấy thuốc với Id {id}."));

        medication.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MedicationBatchDto>>> GetBatchesAsync(
        Guid medicationId, CancellationToken ct = default)
    {
        if (!await _db.Medications.AnyAsync(m => m.Id == medicationId, ct))
            return Error.NotFound("Medication.NotFound", $"Không tìm thấy thuốc với Id {medicationId}.");

        var batches = await _db.MedicationBatches.AsNoTracking()
            .Where(b => b.MedicationId == medicationId)
            // Hạn gần nhất lên đầu (định hướng FEFO cho P2).
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new MedicationBatchDto(
                b.Id, b.MedicationId, b.BatchNumber, b.ExpiryDate, b.QuantityOnHand, b.CreatedAt, b.UpdatedAt))
            .ToListAsync(ct);

        return batches;
    }

    /// <summary>Ánh xạ truy vấn Thuốc sang DTO kèm tồn tổng (subquery Sum các lô chưa xoá, phía server).</summary>
    private IQueryable<MedicationDto> Project(IQueryable<Medication> query) =>
        query.Select(m => new MedicationDto(
            m.Id,
            m.Code,
            m.Name,
            m.ActiveIngredient,
            m.Unit,
            m.ReorderLevel,
            m.Description,
            _db.MedicationBatches.Where(b => b.MedicationId == m.Id).Sum(b => (int?)b.QuantityOnHand) ?? 0,
            m.CreatedAt,
            m.UpdatedAt));

    private async Task<MedicationDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Medications.AsNoTracking().Where(m => m.Id == id)).FirstOrDefaultAsync(ct);

    /// <summary>Sinh mã thuốc dạng TH-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Medications.IgnoreQueryFilters().CountAsync(ct);
        return $"TH-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
