using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Application.Queue;
using ClinicManagement.Application.Visits;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Encounters;

public sealed class EncounterService : IEncounterService
{
    private const int MaxPageSize = 100;

    // Lịch đã kết thúc phần khám — không còn việc gì để làm ở phòng khám (ADR 0017 mở rộng: tự đóng lượt).
    private static readonly AppointmentStatus[] TerminalAppointmentStatuses =
    [
        AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow,
    ];

    private readonly IAppDbContext _db;
    // Best-effort: sinh/cập nhật embedding sau khi ghi phiếu (null trong unit test → bỏ qua).
    private readonly IEncounterEmbeddingIndexer? _embeddingIndexer;
    // Best-effort: tự lập hoá đơn thuốc khi chốt phiếu (null trong unit test → bỏ qua, dùng lại
    // nút "Lập HĐ thuốc" thủ công ở VisitDetailPage làm phương án dự phòng).
    private readonly IInvoiceService? _invoices;
    // Best-effort: tự đóng lượt tiếp nhận khi dịch vụ khám cuối cùng của lượt hoàn tất (null trong
    // unit test → bỏ qua, dùng lại nút "Đóng lượt"/"Mở lại lượt" thủ công làm phương án dự phòng).
    private readonly IVisitService? _visits;
    // Best-effort: tự xử lý vé hàng đợi khi chốt phiếu (null trong unit test → bỏ qua) — tránh số ảo
    // treo mãi trên bảng hàng đợi khi bác sĩ tự "Bắt đầu khám" bỏ qua bước gọi số.
    private readonly IQueueService? _queue;

    public EncounterService(
        IAppDbContext db,
        IEncounterEmbeddingIndexer? embeddingIndexer = null,
        IInvoiceService? invoices = null,
        IVisitService? visits = null,
        IQueueService? queue = null)
    {
        _db = db;
        _embeddingIndexer = embeddingIndexer;
        _invoices = invoices;
        _visits = visits;
        _queue = queue;
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
        await AutoInvoiceMedicationAsync(encounter, ct);
        await AutoResolveQueueTicketAsync(appointment.Id, ct);
        await AutoCloseVisitAsync(appointment.VisitId, ct);
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

    public async Task<Result<EncounterDto>> ReturnStockAsync(
        Guid id, ReturnStockRequest request, Guid returnedByUserId, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {id}.");

        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            return Error.Validation("Pharmacy.ReturnReasonRequired", "Lý do hoàn kho không được để trống.");

        var occurredAt = DateTimeOffset.UtcNow;

        // Chỉ được hoàn khi đã cấp phát thực và còn trong cửa sổ thời gian (Dispensed → Returned).
        var mark = encounter.MarkReturned(reason, returnedByUserId, occurredAt);
        if (mark.IsFailure)
            return Result.Failure<EncounterDto>(mark.Error);

        // Truy sổ cái Dispense theo encounterId để nhập lại đúng lô đã trừ (sổ cái bất biến — chỉ thêm).
        // Giữ thứ tự tạo (FEFO lúc cấp) để hoàn một phần cũng ưu tiên đúng lô đã lấy trước.
        var dispenses = await _db.StockTransactions
            .Where(t => t.Type == StockTransactionType.Dispense
                        && t.ReferenceType == nameof(Encounter)
                        && t.ReferenceId == encounter.Id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
        if (dispenses.Count == 0)
            return Result.Failure<EncounterDto>(Error.Validation(
                "Pharmacy.NothingToReturn", "Đơn này không có giao dịch xuất kho để hoàn."));

        var batchIds = dispenses.Select(t => t.MedicationBatchId).Distinct().ToList();
        var batches = await _db.MedicationBatches
            .Where(b => batchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);

        var dispensedByMedication = dispenses
            .GroupBy(t => batches[t.MedicationBatchId].MedicationId)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.QuantityDelta)));

        // Không chọn dòng nào → hoàn toàn bộ (tương thích ngược, hành vi cũ trước khi có hoàn một phần).
        List<ReturnStockItemRequest> itemsToReturn;
        if (request.Items is null || request.Items.Count == 0)
        {
            itemsToReturn = dispensedByMedication
                .Select(kv => new ReturnStockItemRequest(kv.Key, kv.Value))
                .ToList();
        }
        else
        {
            var seen = new HashSet<Guid>();
            foreach (var item in request.Items)
            {
                if (!seen.Add(item.MedicationId))
                    return Error.Validation("Pharmacy.ReturnDuplicateMedication",
                        "Danh sách hoàn kho có thuốc bị lặp.");

                if (item.Quantity <= 0)
                    return Error.Validation("Pharmacy.ReturnQuantityInvalid",
                        "Số lượng hoàn phải lớn hơn 0.");

                if (!dispensedByMedication.TryGetValue(item.MedicationId, out var dispensedQty))
                    return Error.Validation("Pharmacy.ReturnMedicationNotDispensed",
                        $"Thuốc {item.MedicationId} không thuộc đơn đã cấp phát này.");

                if (item.Quantity > dispensedQty)
                    return Error.Validation("Pharmacy.ReturnQuantityExceedsDispensed",
                        $"Chỉ hoàn tối đa {dispensedQty} (đã cấp) cho thuốc {item.MedicationId}.");
            }

            itemsToReturn = request.Items.ToList();
        }

        foreach (var item in itemsToReturn)
        {
            var remaining = item.Quantity;
            var medicationTxs = dispenses.Where(t => batches[t.MedicationBatchId].MedicationId == item.MedicationId);

            foreach (var tx in medicationTxs)
            {
                if (remaining <= 0)
                    break;

                var batch = batches[tx.MedicationBatchId];
                var take = Math.Min(remaining, Math.Abs(tx.QuantityDelta));
                if (take <= 0)
                    continue;

                batch.Increase(take);
                _db.StockTransactions.Add(new StockTransaction(
                    batch.Id,
                    StockTransactionType.Return,
                    take,
                    referenceType: nameof(Encounter),
                    referenceId: encounter.Id,
                    occurredAt: occurredAt));

                remaining -= take;
            }
        }

        await _db.SaveChangesAsync(ct);
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

    /// <summary>
    /// Tự lập hoá đơn thuốc ngay khi chốt phiếu (nếu có dòng thuốc gắn danh mục) — thay cho việc bắt
    /// Lễ tân phải nhớ bấm "Lập HĐ thuốc" thủ công ở VisitDetailPage (dễ bị bỏ sót). Best-effort: lỗi
    /// (vd đã lập trước đó) không làm hỏng thao tác chốt phiếu; nút thủ công vẫn còn làm phương án
    /// dự phòng nếu vì lý do nào đó bước này không chạy được.
    /// </summary>
    private async Task AutoInvoiceMedicationAsync(Encounter encounter, CancellationToken ct)
    {
        if (_invoices is null) return;
        if (!encounter.PrescriptionItems.Any(p => p.MedicationId is not null)) return;

        await _invoices.CreateFromEncounterAsync(encounter.Id, ct);
    }

    /// <summary>
    /// Tự xử lý vé hàng đợi (nếu có) gắn với dịch vụ khám vừa chốt — tránh số ảo treo mãi trên bảng
    /// hàng đợi khi bác sĩ tự "Bắt đầu khám" (bỏ qua bước gọi số của Lễ tân/Điều dưỡng). Vé đang
    /// <see cref="QueueTicketStatus.InProgress"/> → <c>Done</c> (đã phục vụ xong); vé còn
    /// <see cref="QueueTicketStatus.Waiting"/>/<see cref="QueueTicketStatus.Called"/> (chưa từng qua
    /// quầy) → <c>Skip</c>. Vé đã Done/Skip hoặc không có vé thì bỏ qua.
    /// </summary>
    private async Task AutoResolveQueueTicketAsync(Guid appointmentId, CancellationToken ct)
    {
        if (_queue is null) return;

        var ticket = await _db.QueueTickets
            .Where(t => t.AppointmentId == appointmentId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (ticket is null) return;

        switch (ticket.Status)
        {
            case QueueTicketStatus.InProgress:
                await _queue.DoneAsync(ticket.Id, ct);
                break;
            case QueueTicketStatus.Waiting or QueueTicketStatus.Called:
                await _queue.SkipAsync(ticket.Id, ct);
                break;
        }
    }

    /// <summary>
    /// Tự đóng lượt tiếp nhận khi dịch vụ khám vừa chốt là dịch vụ <b>cuối cùng</b> của lượt còn dang dở
    /// (mọi Appointment khác cùng lượt đã Completed/Cancelled/NoShow) — coi như bác sĩ đã xong việc,
    /// chuyển bệnh nhân xuống quầy thuốc/thu ngân. Không đụng tới việc thanh toán/cấp phát thuốc (độc
    /// lập với VisitStatus). Best-effort: lượt lẻ (không gắn Visit) hoặc lỗi transition (vd đã đóng/huỷ
    /// trước đó) đều bỏ qua; nút "Đóng lượt"/"Mở lại lượt" thủ công vẫn còn làm phương án dự phòng.
    /// </summary>
    private async Task AutoCloseVisitAsync(Guid? visitId, CancellationToken ct)
    {
        if (_visits is null || visitId is null) return;

        var hasUnfinished = await _db.Appointments
            .AnyAsync(a => a.VisitId == visitId && !TerminalAppointmentStatuses.Contains(a.Status), ct);
        if (hasUnfinished) return;

        await _visits.CloseAsync(visitId.Value, ct);
    }

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
            e.UpdatedAt,
            e.MedicationInvoicedAt,
            e.ReturnReason,
            e.ReturnedAt,
            e.ReturnedByUserId,
            _db.Users.Where(u => u.Id == e.ReturnedByUserId).Select(u => u.FullName).FirstOrDefault()));

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
