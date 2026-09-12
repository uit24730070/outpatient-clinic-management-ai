using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;
using UnitTests.Common;

namespace UnitTests.Encounters;

/// <summary>
/// Kiểm thử vòng đời cấp phát thuốc Reserved→Paid→Dispensed (ADR 0021, PAY-02). Chốt phiếu chỉ <b>giữ tồn</b>
/// (kiểm tồn khả dụng, không trừ kho vật lý); Dược sĩ cấp phát thực khi đã thu tiền mới trừ tồn FEFO.
/// Dùng ngày thực (DateTime.UtcNow) để phân biệt lô còn hạn / hết hạn.
/// </summary>
public sealed class EncounterDispenseTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static EncounterService Setup(string dbName, out TestDbContext db, out Appointment appt, out Guid medId)
    {
        db = TestDbContext.CreateInMemory(dbName);
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        var med = new Medication("TH-000001", "Paracetamol 500mg", "Paracetamol", "viên", 10, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.Medications.Add(med);
        appt = AddAppointment(db, patient.Id, doctor.Id);
        db.SaveChanges();
        medId = med.Id;
        return new EncounterService(db);
    }

    private static Appointment AddAppointment(TestDbContext db, Guid patientId, Guid doctorId)
    {
        var appt = new Appointment(patientId, doctorId, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        return appt;
    }

    private static Guid AddBatch(TestDbContext db, Guid medId, string batchNo, DateOnly expiry, int qty)
    {
        var batch = new MedicationBatch(medId, batchNo, expiry, qty);
        db.MedicationBatches.Add(batch);
        db.SaveChanges();
        return batch.Id;
    }

    private static CreateEncounterRequest Req(Guid apptId, Guid medId, int qty) =>
        new(apptId, null, "Viêm họng", null,
            new[] { new PrescriptionItemRequest("Paracetamol", "500mg", qty, null, medId) });

    /// <summary>Mô phỏng thu hoá đơn thuốc: chuyển phiếu Reserved → Paid (mở cổng cấp phát).</summary>
    private static async Task MarkPaidAsync(TestDbContext db, Guid encounterId)
    {
        var e = await db.Encounters.FindAsync(encounterId);
        e!.MarkMedicationPaid(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Complete_ShouldReserve_WithoutDeductingStock()
    {
        var service = Setup(nameof(Complete_ShouldReserve_WithoutDeductingStock), out var db, out var appt, out var medId);
        var batchId = AddBatch(db, medId, "OK", Today.AddMonths(6), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 4));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Equal(DispenseStatus.Reserved, completed.Value.DispenseStatus);
        Assert.NotNull(completed.Value.ReservedAt);
        Assert.Null(completed.Value.DispensedAt);
        // Tồn vật lý chưa đổi + chưa có giao dịch xuất kho.
        var batch = await db.MedicationBatches.FindAsync(batchId);
        Assert.Equal(10, batch!.QuantityOnHand);
        Assert.Equal(0, await db.StockTransactions.CountAsync(t => t.Type == StockTransactionType.Dispense));
    }

    [Fact]
    public async Task Complete_ShouldFail_WhenInsufficientAvailableStock()
    {
        var service = Setup(nameof(Complete_ShouldFail_WhenInsufficientAvailableStock), out var db, out var appt, out var medId);
        AddBatch(db, medId, "OK", Today.AddMonths(2), 5);
        var created = await service.CreateAsync(Req(appt.Id, medId, 10));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsFailure);
        Assert.Equal(ErrorType.Conflict, completed.Error.Type);
        Assert.Equal("Pharmacy.InsufficientStock", completed.Error.Code);

        // Rollback: phiếu vẫn Draft (SaveChanges chưa từng chạy).
        using var fresh = TestDbContext.CreateInMemory(nameof(Complete_ShouldFail_WhenInsufficientAvailableStock));
        Assert.Equal(EncounterStatus.Draft, (await fresh.Encounters.SingleAsync()).Status);
        Assert.Equal(DispenseStatus.None, (await fresh.Encounters.SingleAsync()).DispenseStatus);
    }

    [Fact]
    public async Task Complete_ShouldFail_WhenAvailabilityReducedByOtherReservation()
    {
        var service = Setup(nameof(Complete_ShouldFail_WhenAvailabilityReducedByOtherReservation),
            out var db, out var appt1, out var medId);
        AddBatch(db, medId, "OK", Today.AddMonths(6), 10);

        // Phiếu 1 giữ chỗ 6 (Reserved) — dù tồn vật lý còn 10.
        var enc1 = await service.CreateAsync(Req(appt1.Id, medId, 6));
        var completed1 = await service.CompleteAsync(enc1.Value.Id);
        Assert.True(completed1.IsSuccess);
        Assert.Equal(DispenseStatus.Reserved, completed1.Value.DispenseStatus);

        // Phiếu 2 (lịch khác) cần 6 nhưng tồn khả dụng chỉ còn 10 − 6 = 4 → chặn.
        var appt2 = AddAppointment(db, appt1.PatientId, appt1.DoctorId);
        await db.SaveChangesAsync();
        var enc2 = await service.CreateAsync(Req(appt2.Id, medId, 6));
        var completed2 = await service.CompleteAsync(enc2.Value.Id);

        Assert.True(completed2.IsFailure);
        Assert.Equal("Pharmacy.InsufficientStock", completed2.Error.Code);
    }

    [Fact]
    public async Task Dispense_WhenNotPaid_ShouldReturnConflict()
    {
        var service = Setup(nameof(Dispense_WhenNotPaid_ShouldReturnConflict), out var db, out var appt, out var medId);
        AddBatch(db, medId, "OK", Today.AddMonths(6), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 4));
        await service.CompleteAsync(created.Value.Id); // Reserved (chưa thu)

        var dispensed = await service.DispenseAsync(created.Value.Id);

        Assert.True(dispensed.IsFailure);
        Assert.Equal(ErrorType.Conflict, dispensed.Error.Type);
        Assert.Equal("Pharmacy.NotPaid", dispensed.Error.Code);
        Assert.Equal(0, await db.StockTransactions.CountAsync(t => t.Type == StockTransactionType.Dispense));
    }

    [Fact]
    public async Task Dispense_AfterPaid_ShouldDispenseFefo_NearestExpiryFirst()
    {
        var service = Setup(nameof(Dispense_AfterPaid_ShouldDispenseFefo_NearestExpiryFirst),
            out var db, out var appt, out var medId);
        var nearId = AddBatch(db, medId, "NEAR", Today.AddMonths(1), 10);
        var farId = AddBatch(db, medId, "FAR", Today.AddYears(1), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 15));
        await service.CompleteAsync(created.Value.Id);
        await MarkPaidAsync(db, created.Value.Id);

        var dispensed = await service.DispenseAsync(created.Value.Id);

        Assert.True(dispensed.IsSuccess);
        Assert.Equal(DispenseStatus.Dispensed, dispensed.Value.DispenseStatus);
        Assert.NotNull(dispensed.Value.DispensedAt);

        var near = await db.MedicationBatches.FindAsync(nearId);
        var far = await db.MedicationBatches.FindAsync(farId);
        Assert.Equal(0, near!.QuantityOnHand);   // trừ hết lô hạn gần trước
        Assert.Equal(5, far!.QuantityOnHand);     // rồi mới đụng lô hạn xa

        var txs = await db.StockTransactions.Where(t => t.Type == StockTransactionType.Dispense).ToListAsync();
        Assert.Equal(2, txs.Count);
        Assert.All(txs, t => Assert.Equal(nameof(Encounter), t.ReferenceType));
        Assert.All(txs, t => Assert.Equal(created.Value.Id, t.ReferenceId));
        Assert.Contains(txs, t => t.QuantityDelta == -10);
        Assert.Contains(txs, t => t.QuantityDelta == -5);
    }

    [Fact]
    public async Task Dispense_ShouldSkipExpiredBatches()
    {
        var service = Setup(nameof(Dispense_ShouldSkipExpiredBatches), out var db, out var appt, out var medId);
        // Lô hết hạn có hạn "gần" nhất nhưng phải bị bỏ qua.
        var expiredId = AddBatch(db, medId, "OLD", Today.AddDays(-1), 10);
        var validId = AddBatch(db, medId, "OK", Today.AddMonths(2), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 5));
        await service.CompleteAsync(created.Value.Id);
        await MarkPaidAsync(db, created.Value.Id);

        var dispensed = await service.DispenseAsync(created.Value.Id);

        Assert.True(dispensed.IsSuccess);
        var expired = await db.MedicationBatches.FindAsync(expiredId);
        var valid = await db.MedicationBatches.FindAsync(validId);
        Assert.Equal(10, expired!.QuantityOnHand);  // lô hết hạn nguyên vẹn
        Assert.Equal(5, valid!.QuantityOnHand);       // chỉ trừ lô còn hạn
    }

    [Fact]
    public async Task Dispense_WhenNoMedicationLink_ShouldReturnNothingToDispense()
    {
        var service = Setup(nameof(Dispense_WhenNoMedicationLink_ShouldReturnNothingToDispense),
            out var db, out var appt, out _);
        // Đơn thuốc ngoài danh mục (MedicationId = null) → không đi qua vòng đời kho.
        var created = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Cảm cúm", null,
            new[] { new PrescriptionItemRequest("Thuốc ngoài danh mục", "1 viên", 3, null) }));
        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Equal(DispenseStatus.None, completed.Value.DispenseStatus);

        var dispensed = await service.DispenseAsync(created.Value.Id);
        Assert.True(dispensed.IsFailure);
        Assert.Equal(ErrorType.Validation, dispensed.Error.Type);
        Assert.Equal("Pharmacy.NothingToDispense", dispensed.Error.Code);
    }

    // ── REF-02: Hoàn kho đơn đã cấp phát ────────────────────────────────────

    [Fact]
    public async Task ReturnStock_ShouldRestoreBatchQuantity_AndMarkReturned()
    {
        var service = Setup(nameof(ReturnStock_ShouldRestoreBatchQuantity_AndMarkReturned),
            out var db, out var appt, out var medId);
        var batchId = AddBatch(db, medId, "LOT-A", Today.AddMonths(6), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 4));
        await service.CompleteAsync(created.Value.Id);
        await MarkPaidAsync(db, created.Value.Id);
        await service.DispenseAsync(created.Value.Id);

        // Trước hoàn: tồn = 6 (đã cấp 4)
        var before = await db.MedicationBatches.FindAsync(batchId);
        Assert.Equal(6, before!.QuantityOnHand);

        var result = await service.ReturnStockAsync(created.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(DispenseStatus.Returned, result.Value.DispenseStatus);

        // Sau hoàn: tồn = 10 (khôi phục đúng lô)
        await db.Entry(before).ReloadAsync();
        Assert.Equal(10, before.QuantityOnHand);

        // Sổ cái: có giao dịch Return dương
        var returnTx = await db.StockTransactions
            .FirstOrDefaultAsync(t => t.Type == StockTransactionType.Return && t.MedicationBatchId == batchId);
        Assert.NotNull(returnTx);
        Assert.Equal(4, returnTx!.QuantityDelta);
    }

    [Fact]
    public async Task ReturnStock_ShouldBeIdempotentGuard_ReturnConflictOnSecondCall()
    {
        var service = Setup(nameof(ReturnStock_ShouldBeIdempotentGuard_ReturnConflictOnSecondCall),
            out var db, out var appt, out var medId);
        AddBatch(db, medId, "LOT-B", Today.AddMonths(6), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 2));
        await service.CompleteAsync(created.Value.Id);
        await MarkPaidAsync(db, created.Value.Id);
        await service.DispenseAsync(created.Value.Id);
        await service.ReturnStockAsync(created.Value.Id);

        // Gọi lần 2 → 409
        var second = await service.ReturnStockAsync(created.Value.Id);

        Assert.True(second.IsFailure);
        Assert.Equal(ErrorType.Conflict, second.Error.Type);
        Assert.Equal("Pharmacy.InvalidDispenseTransition", second.Error.Code);
    }

    [Fact]
    public async Task ReturnStock_WhenNotDispensed_ShouldReturnConflict()
    {
        var service = Setup(nameof(ReturnStock_WhenNotDispensed_ShouldReturnConflict),
            out var db, out var appt, out var medId);
        AddBatch(db, medId, "LOT-C", Today.AddMonths(6), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 3));
        await service.CompleteAsync(created.Value.Id);
        // Chưa thu tiền, chưa cấp phát — gọi ReturnStock ngay → 409

        var result = await service.ReturnStockAsync(created.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
