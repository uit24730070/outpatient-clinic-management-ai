using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;
using UnitTests.Common;

namespace UnitTests.Billing;

public sealed class InvoiceServiceTests
{
    private static InvoiceService CreateService(TestDbContext db) =>
        new(db, new BillingOptions());

    /// <summary>Seed dịch vụ công khám mặc định (DV-000001) mà lập-từ-phiếu cần.</summary>
    private static ServicePrice SeedConsultation(TestDbContext db, decimal price = 150000m)
    {
        var svc = new ServicePrice("DV-000001", "Khám tổng quát", price, null);
        db.ServicePrices.Add(svc);
        return svc;
    }

    private static Medication SeedMedication(TestDbContext db, string code, decimal salePrice)
    {
        var med = new Medication(code, $"Thuốc {code}", "Hoạt chất", "viên", 0, null, salePrice);
        db.Medications.Add(med);
        return med;
    }

    /// <summary>Tạo một phiếu khám đã hoàn tất với các dòng đơn (đã kèm/không kèm MedicationId).</summary>
    private static Encounter SeedCompletedEncounter(TestDbContext db, Guid patientId, IEnumerable<PrescriptionItem> items)
    {
        var encounter = new Encounter(Guid.NewGuid(), patientId, Guid.NewGuid(), null, "Viêm họng cấp", null);
        encounter.ReplaceItems(items);
        encounter.Complete();
        db.Encounters.Add(encounter);
        return encounter;
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldBuildConsultationAndMedicationLines_WithSnapshotPrices()
    {
        var db = TestDbContext.CreateInMemory();
        SeedConsultation(db, 150000m);
        var medA = SeedMedication(db, "TH-000001", 5000m);
        var medB = SeedMedication(db, "TH-000002", 8000m);
        var patientId = Guid.NewGuid();
        var encounter = SeedCompletedEncounter(db, patientId, new[]
        {
            new PrescriptionItem("Thuốc A", "500mg", 10, null, medA.Id),
            new PrescriptionItem("Thuốc B", "250mg", 2, null, medB.Id),
            new PrescriptionItem("Thuốc ngoài danh mục", "1 gói", 5, null) // MedicationId = null → bỏ qua
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(encounter.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("HD-000001", result.Value.Code);
        Assert.Equal(patientId, result.Value.PatientId);
        Assert.Equal(encounter.Id, result.Value.EncounterId);
        Assert.Equal(InvoiceStatus.Draft, result.Value.Status);

        // 1 công khám + 2 dòng thuốc (dòng ngoài danh mục bị bỏ qua).
        Assert.Equal(3, result.Value.Items.Count);

        var consultation = Assert.Single(result.Value.Items, i => i.ItemType == InvoiceItemType.ServiceFee);
        Assert.Equal(150000m, consultation.UnitPrice);
        Assert.Equal(150000m, consultation.LineTotal);

        var medLines = result.Value.Items.Where(i => i.ItemType == InvoiceItemType.Medication).ToList();
        Assert.Equal(2, medLines.Count);
        Assert.Equal(50000m, medLines.Single(i => i.ReferenceId == medA.Id).LineTotal); // 5000 × 10
        Assert.Equal(16000m, medLines.Single(i => i.ReferenceId == medB.Id).LineTotal); // 8000 × 2

        // TotalAmount = Σ LineTotal.
        Assert.Equal(216000m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldSnapshotPrice_NotAffectedByLaterPriceChange()
    {
        var db = TestDbContext.CreateInMemory();
        SeedConsultation(db, 150000m);
        var med = SeedMedication(db, "TH-000001", 5000m);
        var encounter = SeedCompletedEncounter(db, Guid.NewGuid(), new[]
        {
            new PrescriptionItem("Thuốc A", "500mg", 4, null, med.Id)
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(encounter.Id);

        // Đổi giá bán sau khi lập hoá đơn → hoá đơn cũ không đổi.
        med.UpdateDetails(med.Name, "Hoạt chất", "viên", 0, null, 9999m);
        await db.SaveChangesAsync();

        var reread = await service.GetByIdAsync(created.Value.Id);
        var medLine = Assert.Single(reread.Value.Items, i => i.ItemType == InvoiceItemType.Medication);
        Assert.Equal(5000m, medLine.UnitPrice);
        Assert.Equal(20000m, medLine.LineTotal);
    }

    [Fact]
    public async Task CreateFromEncounter_Duplicate_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        SeedConsultation(db);
        var encounter = SeedCompletedEncounter(db, Guid.NewGuid(), Array.Empty<PrescriptionItem>());
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var first = await service.CreateFromEncounterAsync(encounter.Id);
        Assert.True(first.IsSuccess);

        var second = await service.CreateFromEncounterAsync(encounter.Id);
        Assert.True(second.IsFailure);
        Assert.Equal(ErrorType.Conflict, second.Error.Type);
        Assert.Equal("Billing.InvoiceAlreadyExists", second.Error.Code);
    }

    [Fact]
    public async Task CreateFromEncounter_WhenNotCompleted_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        SeedConsultation(db);
        var draft = new Encounter(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Cúm mùa", null);
        db.Encounters.Add(draft);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(draft.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Billing.EncounterNotCompleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateFromEncounter_WhenEncounterMissing_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        SeedConsultation(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_ManualInvoice_ShouldSnapshotServicePrices_AndSumTotal()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(patient);
        var svc1 = new ServicePrice("DV-000001", "Khám tổng quát", 150000m, null);
        var svc2 = new ServicePrice("DV-000002", "Thủ thuật", 200000m, null);
        db.ServicePrices.AddRange(svc1, svc2);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAsync(new CreateInvoiceRequest(
            patient.Id, "Hoá đơn dịch vụ lẻ", new[]
            {
                new CreateInvoiceItemRequest(svc1.Id, 1),
                new CreateInvoiceItemRequest(svc2.Id, 2)
            }));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EncounterId);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(150000m + 200000m * 2, result.Value.TotalAmount); // 550000
    }

    [Fact]
    public async Task CreateAsync_WhenServiceMissing_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAsync(new CreateInvoiceRequest(
            patient.Id, null, new[] { new CreateInvoiceItemRequest(Guid.NewGuid(), 1) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByPatientAndStatus()
    {
        var db = TestDbContext.CreateInMemory();
        var p1 = new Patient("BN-000001", "A", null, Gender.Male, null, null);
        var p2 = new Patient("BN-000002", "B", null, Gender.Male, null, null);
        db.Patients.AddRange(p1, p2);
        var svc = new ServicePrice("DV-000001", "Khám", 100000m, null);
        db.ServicePrices.Add(svc);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.CreateAsync(new CreateInvoiceRequest(p1.Id, null, new[] { new CreateInvoiceItemRequest(svc.Id, 1) }));
        await service.CreateAsync(new CreateInvoiceRequest(p2.Id, null, new[] { new CreateInvoiceItemRequest(svc.Id, 1) }));

        var byPatient = await service.GetListAsync(1, 20, p1.Id, null, null, null);
        Assert.Equal(1, byPatient.Value.TotalCount);

        var draftOnly = await service.GetListAsync(1, 20, null, InvoiceStatus.Draft, null, null);
        Assert.Equal(2, draftOnly.Value.TotalCount);

        var paidOnly = await service.GetListAsync(1, 20, null, InvoiceStatus.Paid, null, null);
        Assert.Equal(0, paidOnly.Value.TotalCount);
    }

    // ── BILL-04: thu tiền + vòng đời ──────────────────────────────────────────

    /// <summary>Tạo nhanh một hoá đơn lẻ Draft cho các test vòng đời.</summary>
    private static async Task<(InvoiceService service, Guid invoiceId)> SeedDraftInvoiceAsync(
        TestDbContext db, decimal price = 100000m)
    {
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(patient);
        var svc = new ServicePrice("DV-000001", "Khám", price, null);
        db.ServicePrices.Add(svc);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateAsync(new CreateInvoiceRequest(
            patient.Id, null, new[] { new CreateInvoiceItemRequest(svc.Id, 1) }));
        return (service, created.Value.Id);
    }

    [Fact]
    public async Task PayAsync_Draft_ShouldMovePaid_AndSetPaidAtAndMethod()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);

        var result = await service.PayAsync(id, new PayInvoiceRequest(PaymentMethod.Card));

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Paid, result.Value.Status);
        Assert.Equal(PaymentMethod.Card, result.Value.PaymentMethod);
        Assert.NotNull(result.Value.PaidAt);
    }

    [Fact]
    public async Task PayAsync_AlreadyPaid_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);
        await service.PayAsync(id, new PayInvoiceRequest(PaymentMethod.Cash));

        var again = await service.PayAsync(id, new PayInvoiceRequest(PaymentMethod.Transfer));

        Assert.True(again.IsFailure);
        Assert.Equal(ErrorType.Conflict, again.Error.Type);
        Assert.Equal("Billing.InvalidTransition", again.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_AfterPaid_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);
        var svcId = (await db.ServicePrices.FirstAsync()).Id;
        await service.PayAsync(id, new PayInvoiceRequest(PaymentMethod.Cash));

        var update = await service.UpdateAsync(id, new UpdateInvoiceRequest(
            "sửa sau khi thu", new[] { new CreateInvoiceItemRequest(svcId, 3) }));

        Assert.True(update.IsFailure);
        Assert.Equal(ErrorType.Conflict, update.Error.Type);
        Assert.Equal("Billing.InvalidTransition", update.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_Draft_ShouldReplaceItems_AndRecalcTotal()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db, 100000m);
        var svcId = (await db.ServicePrices.FirstAsync()).Id;

        var update = await service.UpdateAsync(id, new UpdateInvoiceRequest(
            "ghi chú mới", new[] { new CreateInvoiceItemRequest(svcId, 3) }));

        Assert.True(update.IsSuccess);
        Assert.Equal(300000m, update.Value.TotalAmount);
        Assert.Equal("ghi chú mới", update.Value.Note);
        Assert.Single(update.Value.Items);
    }

    [Fact]
    public async Task CancelAsync_Draft_ShouldMoveCancelled()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);

        var result = await service.CancelAsync(id);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Cancelled, result.Value.Status);
    }

    [Fact]
    public async Task DeleteAsync_AfterPaid_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);
        await service.PayAsync(id, new PayInvoiceRequest(PaymentMethod.Cash));

        var del = await service.DeleteAsync(id);

        Assert.True(del.IsFailure);
        Assert.Equal(ErrorType.Conflict, del.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_Draft_ShouldSoftDelete_AndHideFromList()
    {
        var db = TestDbContext.CreateInMemory();
        var (service, id) = await SeedDraftInvoiceAsync(db);

        var del = await service.DeleteAsync(id);
        Assert.True(del.IsSuccess);

        var list = await service.GetListAsync(1, 20, null, null, null, null);
        Assert.Equal(0, list.Value.TotalCount);
    }
}
