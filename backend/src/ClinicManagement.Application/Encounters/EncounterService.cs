using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Pharmacy;
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

        var medicationsCheck = await EnsureMedicationsExistAsync(request.PrescriptionItems, ct);
        if (medicationsCheck.IsFailure)
            return Result.Failure<EncounterDto>(medicationsCheck.Error);

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

        if (filter.DispenseStatus is { } dispenseStatus)
            query = query.Where(e => e.DispenseStatus == dispenseStatus);

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

        var medicationsCheck = await EnsureMedicationsExistAsync(request.PrescriptionItems, ct);
        if (medicationsCheck.IsFailure)
            return Result.Failure<EncounterDto>(medicationsCheck.Error);

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

        // Giữ tồn (Reserved) thay vì cấp phát ngay (ADR 0021, PAY-02): kiểm tồn khả dụng đủ nhưng
        // chưa trừ tồn vật lý — chờ thu tiền rồi Dược sĩ mới cấp phát thực.
        var reserve = await ReserveAsync(encounter, ct);
        if (reserve.IsFailure)
            return Result.Failure<EncounterDto>(reserve.Error);

        await _db.SaveChangesAsync(ct);

        await IndexEmbeddingAsync(encounter.Id, ct);
        return (await ProjectByIdAsync(encounter.Id, ct))!;
    }

    public async Task<Result<EncounterDto>> DispenseAsync(Guid id, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {id}.");

        if (encounter.DispenseStatus == DispenseStatus.None)
            return Error.Validation("Pharmacy.NothingToDispense",
                "Phiếu khám không có thuốc gắn danh mục để cấp phát.");

        // Chuyển trạng thái Domain trước (Paid → Dispensed); chưa thu (Reserved) → Pharmacy.NotPaid.
        var occurredAt = DateTimeOffset.UtcNow;
        var mark = encounter.MarkDispensed(occurredAt);
        if (mark.IsFailure)
            return Result.Failure<EncounterDto>(mark.Error);

        // Xuất kho thực theo FEFO (trừ tồn + ghi sổ cái). Thiếu tồn → rollback (chưa SaveChanges).
        var dispense = await DispenseStockAsync(encounter, occurredAt, ct);
        if (dispense.IsFailure)
            return Result.Failure<EncounterDto>(dispense.Error);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Lô vừa bị thay đổi bởi thao tác khác (concurrency token xmin) — client thử lại.
            return Error.Conflict("Pharmacy.ConcurrencyConflict",
                "Tồn kho vừa thay đổi bởi thao tác khác, vui lòng thử lại.");
        }

        return (await ProjectByIdAsync(encounter.Id, ct))!;
    }

    /// <summary>
    /// Giữ tồn khi chốt phiếu (ADR 0021, PAY-02): nếu có dòng thuốc gắn danh mục, kiểm <b>tồn khả dụng</b>
    /// (tồn lô còn hạn − số lượng đang Reserved/Paid chưa cấp phát) đủ cho từng thuốc rồi đặt trạng thái
    /// Reserved. Thiếu → <c>Pharmacy.InsufficientStock</c>. Không đụng <see cref="MedicationBatch"/>.
    /// </summary>
    private async Task<Result> ReserveAsync(Encounter encounter, CancellationToken ct)
    {
        var needed = encounter.PrescriptionItems
            .Where(i => i.MedicationId is not null)
            .GroupBy(i => i.MedicationId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (needed.Count == 0)
            return Result.Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var (medicationId, quantity) in needed)
        {
            // Tồn còn hạn của thuốc (chưa trừ đơn giữ chỗ).
            var onHand = await _db.MedicationBatches
                .Where(b => b.MedicationId == medicationId && b.ExpiryDate >= today)
                .SumAsync(b => (int?)b.QuantityOnHand, ct) ?? 0;

            // Số lượng đang giữ chỗ (Reserved/Paid, chưa Dispensed) của thuốc — trừ khỏi tồn khả dụng.
            var reserved = await _db.Encounters
                .Where(e => e.DispenseStatus == DispenseStatus.Reserved || e.DispenseStatus == DispenseStatus.Paid)
                .SelectMany(e => e.PrescriptionItems)
                .Where(i => i.MedicationId == medicationId)
                .SumAsync(i => (int?)i.Quantity, ct) ?? 0;

            var available = onHand - reserved;
            if (available < quantity)
                return Result.Failure(Error.Conflict("Pharmacy.InsufficientStock",
                    $"Không đủ tồn khả dụng để giữ thuốc (Id {medicationId}): khả dụng {available}, cần {quantity}."));
        }

        return encounter.MarkReserved(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Xuất kho thực các dòng đơn có <c>MedicationId</c>: trừ tồn các lô còn hạn theo FEFO (hạn tăng dần),
    /// ghi <see cref="StockTransaction"/> <see cref="StockTransactionType.Dispense"/> (âm). Bỏ qua lô đã
    /// hết hạn. Thiếu tồn còn hạn → <c>Pharmacy.InsufficientStock</c> (không cấp phát một phần). Chưa ghi
    /// DB ở đây — <see cref="DispenseAsync"/> gọi <c>SaveChanges</c> một lần (cùng transaction).
    /// </summary>
    private async Task<Result> DispenseStockAsync(Encounter encounter, DateTimeOffset occurredAt, CancellationToken ct)
    {
        // Gom số lượng cần cấp phát theo từng thuốc (chỉ dòng gắn danh mục).
        var needed = encounter.PrescriptionItems
            .Where(i => i.MedicationId is not null)
            .GroupBy(i => i.MedicationId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (needed.Count == 0)
            return Result.Success();

        var today = DateOnly.FromDateTime(occurredAt.UtcDateTime);

        foreach (var (medicationId, quantity) in needed)
        {
            var remaining = quantity;
            // FEFO: chỉ lô còn hạn (ExpiryDate ≥ hôm nay), còn tồn, hạn gần nhất trước.
            var batches = await _db.MedicationBatches
                .Where(b => b.MedicationId == medicationId && b.QuantityOnHand > 0 && b.ExpiryDate >= today)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync(ct);

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                    break;

                var take = Math.Min(remaining, batch.QuantityOnHand);
                var decrease = batch.Decrease(take);
                if (decrease.IsFailure)
                    return decrease;

                _db.StockTransactions.Add(new StockTransaction(
                    batch.Id,
                    StockTransactionType.Dispense,
                    -take,
                    referenceType: nameof(Encounter),
                    referenceId: encounter.Id,
                    occurredAt: occurredAt));

                remaining -= take;
            }

            if (remaining > 0)
                return Result.Failure(Error.Conflict("Pharmacy.InsufficientStock",
                    $"Không đủ tồn còn hạn để cấp phát thuốc (Id {medicationId}): còn thiếu {remaining}."));
        }

        return Result.Success();
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
                .Select(i => new PrescriptionItemDto(i.MedicationId, i.DrugName, i.Dosage, i.Quantity, i.Instruction))
                .ToList(),
            e.DispenseStatus,
            e.ReservedAt,
            e.MedicationPaidAt,
            e.DispensedAt,
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
                NormalizeOptional(i.Instruction),
                i.MedicationId));

    /// <summary>Kiểm mọi dòng đơn có <c>MedicationId</c> đều trỏ tới thuốc tồn tại (chưa xoá).</summary>
    private async Task<Result> EnsureMedicationsExistAsync(
        IReadOnlyList<PrescriptionItemRequest>? items, CancellationToken ct)
    {
        var medicationIds = (items ?? Array.Empty<PrescriptionItemRequest>())
            .Where(i => i.MedicationId is not null)
            .Select(i => i.MedicationId!.Value)
            .Distinct()
            .ToList();
        if (medicationIds.Count == 0)
            return Result.Success();

        var existingIds = await _db.Medications
            .Where(m => medicationIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);
        var missing = medicationIds.Except(existingIds).ToList();
        return missing.Count > 0
            ? Result.Failure(Error.NotFound("Pharmacy.MedicationNotFound",
                $"Thuốc không tồn tại hoặc đã bị xoá: {string.Join(", ", missing)}."))
            : Result.Success();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
