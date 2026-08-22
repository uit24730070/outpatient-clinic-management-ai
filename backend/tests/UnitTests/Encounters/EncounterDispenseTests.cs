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
/// Kiểm thử cấp phát thuốc FEFO khi chốt phiếu khám (PH-07, ADR 0011). Dùng ngày thực (DateTime.UtcNow)
/// để phân biệt lô còn hạn / hết hạn cho khớp logic bỏ qua lô hết hạn.
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
        appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        db.SaveChanges();
        medId = med.Id;
        return new EncounterService(db);
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

    [Fact]
    public async Task Complete_ShouldDispenseFefo_NearestExpiryFirst()
    {
        var service = Setup(nameof(Complete_ShouldDispenseFefo_NearestExpiryFirst), out var db, out var appt, out var medId);
        var nearId = AddBatch(db, medId, "NEAR", Today.AddMonths(1), 10);
        var farId = AddBatch(db, medId, "FAR", Today.AddYears(1), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 15));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.NotNull(completed.Value.DispensedAt);

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
    public async Task Complete_ShouldSkipExpiredBatches()
    {
        var service = Setup(nameof(Complete_ShouldSkipExpiredBatches), out var db, out var appt, out var medId);
        // Lô hết hạn có hạn "gần" nhất nhưng phải bị bỏ qua.
        var expiredId = AddBatch(db, medId, "OLD", Today.AddDays(-1), 10);
        var validId = AddBatch(db, medId, "OK", Today.AddMonths(2), 10);
        var created = await service.CreateAsync(Req(appt.Id, medId, 5));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var expired = await db.MedicationBatches.FindAsync(expiredId);
        var valid = await db.MedicationBatches.FindAsync(validId);
        Assert.Equal(10, expired!.QuantityOnHand);  // lô hết hạn nguyên vẹn
        Assert.Equal(5, valid!.QuantityOnHand);       // chỉ trừ lô còn hạn
    }

    [Fact]
    public async Task Complete_ShouldFail_AndRollback_WhenInsufficientStock()
    {
        var dbName = nameof(Complete_ShouldFail_AndRollback_WhenInsufficientStock);
        var service = Setup(dbName, out var db, out var appt, out var medId);
        AddBatch(db, medId, "OK", Today.AddMonths(2), 5);
        var created = await service.CreateAsync(Req(appt.Id, medId, 10));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsFailure);
        Assert.Equal(ErrorType.Conflict, completed.Error.Type);
        Assert.Equal("Pharmacy.InsufficientStock", completed.Error.Code);

        // Rollback: mở context mới trên cùng CSDL — không có giao dịch cấp phát nào được ghi,
        // tồn lô giữ nguyên, phiếu vẫn Draft (SaveChanges chưa từng chạy).
        using var fresh = TestDbContext.CreateInMemory(dbName);
        Assert.Equal(0, await fresh.StockTransactions.CountAsync(t => t.Type == StockTransactionType.Dispense));
        Assert.Equal(5, (await fresh.MedicationBatches.SingleAsync()).QuantityOnHand);
        Assert.Equal(EncounterStatus.Draft, (await fresh.Encounters.SingleAsync()).Status);
    }

    [Fact]
    public async Task Complete_ShouldNotDispense_WhenNoMedicationLink()
    {
        var service = Setup(nameof(Complete_ShouldNotDispense_WhenNoMedicationLink), out var db, out var appt, out _);
        AddBatch(db, appt.Id, "X", Today.AddMonths(2), 5); // lô của thuốc khác — không liên quan
        // Đơn thuốc ngoài danh mục (MedicationId = null).
        var created = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Cảm cúm", null,
            new[] { new PrescriptionItemRequest("Thuốc ngoài danh mục", "1 viên", 3, null) }));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Null(completed.Value.DispensedAt); // không cấp phát → DispensedAt null
        Assert.Equal(0, await db.StockTransactions.CountAsync(t => t.Type == StockTransactionType.Dispense));
    }
}
