using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Encounters;

public sealed class EncounterService : IEncounterService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    // Best-effort: sinh/cập nhật embedding sau khi ghi phiếu (null trong unit test → bỏ qua).
    private readonly IEncounterEmbeddingIndexer? _embeddingIndexer;

    public EncounterService(IAppDbContext db, IEncounterEmbeddingIndexer? embeddingIndexer = null)
    {
        _db = db;
        _embeddingIndexer = embeddingIndexer;
    }

    public async Task<Result<EncounterDto>> CreateAsync(
        CreateEncounterRequest request, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return Error.Validation("Encounter.AppointmentNotFound",
                $"Lịch khám với Id {request.AppointmentId} không tồn tại.");

        // Chỉ tạo phiếu cho lịch đang khám (đã check-in và bắt đầu) — ADR 0006.
        if (appointment.Status != AppointmentStatus.InProgress)
            return Error.Conflict("Encounter.AppointmentNotInProgress",
                $"Chỉ tạo phiếu khám cho lịch đang khám (InProgress); trạng thái hiện tại là {appointment.Status}.");

        // Ràng buộc 1–1: một lịch khám chỉ có một phiếu (unique index chặn ở DB).
        if (await _db.Encounters.AnyAsync(e => e.AppointmentId == request.AppointmentId, ct))
            return Error.Conflict("Encounter.AlreadyExists",
                "Lịch khám này đã có phiếu khám.");

        var encounter = new Encounter(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            NormalizeOptional(request.Symptoms),
            request.Diagnosis.Trim(),
            NormalizeOptional(request.Notes));

        encounter.ReplaceItems(MapItems(request.PrescriptionItems));

        _db.Encounters.Add(encounter);
        await _db.SaveChangesAsync(ct);
        await IndexEmbeddingAsync(encounter.Id, ct);

        return (await ProjectByIdAsync(encounter.Id, ct))!;
    }

    public async Task<Result<PagedResult<EncounterDto>>> GetListAsync(
        EncounterFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > MaxPageSize ? 20 : filter.PageSize;

        var query = _db.Encounters.AsNoTracking();

        if (filter.PatientId is { } patientId)
            query = query.Where(e => e.PatientId == patientId);

        if (filter.DoctorId is { } doctorId)
            query = query.Where(e => e.DoctorId == doctorId);

        if (filter.Status is { } status)
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync(ct);
        // Lịch sử khám: mới nhất lên đầu.
        var items = await Project(query.OrderByDescending(e => e.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<EncounterDto>(items, page, pageSize, total);
    }

    public async Task<Result<EncounterDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {id}.")
            : dto;
    }

    public async Task<Result<EncounterDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var dto = await Project(_db.Encounters.AsNoTracking().Where(e => e.AppointmentId == appointmentId))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? Error.NotFound("Encounter.NotFound", $"Lịch khám với Id {appointmentId} chưa có phiếu khám.")
            : dto;
    }

    public async Task<Result<EncounterDto>> UpdateAsync(
        Guid id, UpdateEncounterRequest request, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {id}.");

        var update = encounter.UpdateDetails(
            NormalizeOptional(request.Symptoms),
            request.Diagnosis.Trim(),
            NormalizeOptional(request.Notes));
        if (update.IsFailure)
            return Result.Failure<EncounterDto>(update.Error);

        var replace = encounter.ReplaceItems(MapItems(request.PrescriptionItems));
        if (replace.IsFailure)
            return Result.Failure<EncounterDto>(replace.Error);

        await _db.SaveChangesAsync(ct);
        await IndexEmbeddingAsync(encounter.Id, ct);
        return (await ProjectByIdAsync(encounter.Id, ct))!;
    }

    public async Task<Result<EncounterDto>> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {id}.");

        var complete = encounter.Complete();
        if (complete.IsFailure)
            return Result.Failure<EncounterDto>(complete.Error);

        // Khép lịch khám bằng method Domain (InProgress → Completed); cả hai cùng thành/bại.
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == encounter.AppointmentId, ct);
        if (appointment is null)
            return Error.NotFound("Encounter.AppointmentNotFound",
                $"Lịch khám gắn với phiếu không tồn tại (Id {encounter.AppointmentId}).");

        var closeAppointment = appointment.Complete();
        if (closeAppointment.IsFailure)
            return Result.Failure<EncounterDto>(closeAppointment.Error);

        await _db.SaveChangesAsync(ct);
        await IndexEmbeddingAsync(encounter.Id, ct);
        return (await ProjectByIdAsync(encounter.Id, ct))!;
    }

    /// <summary>Lập chỉ mục embedding cho phiếu (best-effort; bỏ qua khi chưa cấu hình indexer).</summary>
    private Task IndexEmbeddingAsync(Guid encounterId, CancellationToken ct) =>
        _embeddingIndexer?.IndexAsync(encounterId, ct) ?? Task.CompletedTask;

    /// <summary>Ánh xạ truy vấn Phiếu khám sang DTO kèm tên bệnh nhân/bác sĩ (subquery) và cụm đơn thuốc.</summary>
    private IQueryable<EncounterDto> Project(IQueryable<Encounter> query) =>
        query.Select(e => new EncounterDto(
            e.Id,
            e.AppointmentId,
            e.PatientId,
            _db.Patients.Where(p => p.Id == e.PatientId).Select(p => p.FullName).FirstOrDefault(),
            e.DoctorId,
            _db.Doctors.Where(d => d.Id == e.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            e.Symptoms,
            e.Diagnosis,
            e.Notes,
            e.Status,
            e.PrescriptionItems
                .Select(i => new PrescriptionItemDto(i.DrugName, i.Dosage, i.Quantity, i.Instruction))
                .ToList(),
            e.CreatedAt,
            e.UpdatedAt));

    private async Task<EncounterDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Encounters.AsNoTracking().Where(e => e.Id == id)).FirstOrDefaultAsync(ct);

    private static IEnumerable<PrescriptionItem> MapItems(IReadOnlyList<PrescriptionItemRequest>? items) =>
        (items ?? Array.Empty<PrescriptionItemRequest>())
            .Select(i => new PrescriptionItem(
                i.DrugName.Trim(),
                i.Dosage.Trim(),
                i.Quantity,
                NormalizeOptional(i.Instruction)));

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
