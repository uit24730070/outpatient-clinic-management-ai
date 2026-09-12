using ClinicManagement.Application.Paraclinical;
using ClinicManagement.Application.Paraclinical.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Paraclinical;

public sealed class LabOrderServiceTests
{
    private static LabOrderService CreateService(TestDbContext db) => new(db);

    /// <summary>Tạo một phiếu khám nháp (điều kiện để chỉ định CLS).</summary>
    private static Encounter SeedDraftEncounter(TestDbContext db, Guid? patientId = null, Guid? doctorId = null)
    {
        var encounter = new Encounter(
            Guid.NewGuid(), patientId ?? Guid.NewGuid(), doctorId ?? Guid.NewGuid(),
            null, "Theo dõi", null);
        db.Encounters.Add(encounter);
        return encounter;
    }

    private static ServicePrice SeedParaclinical(TestDbContext db, string code, string name, decimal price)
    {
        var svc = new ServicePrice(code, name, price, null, ServiceCategory.Paraclinical);
        db.ServicePrices.Add(svc);
        return svc;
    }

    private static Patient SeedPatient(TestDbContext db, string code = "BN-000001")
    {
        var p = new Patient(code, "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(p);
        return p;
    }

    /// <summary>Đánh dấu phiếu chỉ định đã thu phí CLS (mô phỏng thu hoá đơn) để mở cổng nhập kết quả (PAY-01).</summary>
    private static async Task MarkPaidAsync(TestDbContext db, Guid orderId)
    {
        var order = await db.LabOrders.FindAsync(orderId);
        order!.MarkPaid(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateWalkIn_WithVisit_ShouldAttachVisitId()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        var visit = new Visit("LK-000001", patient.Id, null);
        db.Visits.Add(visit);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, null, null, new[] { new CreateLabOrderItemRequest(xn.Id) }, visit.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(visit.Id, result.Value.VisitId);
    }

    [Fact]
    public async Task CreateWalkIn_ShouldFail_WhenVisitMissing()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, null, null, new[] { new CreateLabOrderItemRequest(xn.Id) }, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateWalkIn_WithoutEncounterOrDoctor_ShouldSnapshotAndGenerateCode()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, null, "BN yêu cầu", new[] { new CreateLabOrderItemRequest(xn.Id) }));

        Assert.True(result.IsSuccess);
        Assert.Equal("CLS-000001", result.Value.Code);
        Assert.Equal(patient.Id, result.Value.PatientId);
        Assert.Null(result.Value.EncounterId);
        Assert.Null(result.Value.DoctorId);
        Assert.Equal(LabOrderStatus.Ordered, result.Value.Status);
        Assert.Equal(80000m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task CreateWalkIn_WithNonParaclinicalService_ShouldReturnValidation()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var consult = new ServicePrice("DV-000001", "Khám tổng quát", 150000m, null, ServiceCategory.Consultation);
        db.ServicePrices.Add(consult);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, null, null, new[] { new CreateLabOrderItemRequest(consult.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal("Paraclinical.ServiceNotParaclinical", result.Error.Code);
    }

    [Fact]
    public async Task CreateWalkIn_WhenPatientMissing_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            Guid.NewGuid(), null, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Patient.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateWalkIn_WhenAppointmentMissing_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, Guid.NewGuid(), null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Appointment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateWalkIn_WithAppointment_ShouldAttachAppointmentId()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var appt = new Appointment(patient.Id, Guid.NewGuid(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);
        db.Appointments.Add(appt);
        var xn = SeedParaclinical(db, "DV-CLS001", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateWalkInAsync(new CreateWalkInLabOrderRequest(
            patient.Id, appt.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        Assert.True(result.IsSuccess);
        Assert.Equal(appt.Id, result.Value.AppointmentId);
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldSnapshotServices_AndGenerateCode()
    {
        var db = TestDbContext.CreateInMemory();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var encounter = SeedDraftEncounter(db, patientId, doctorId);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        var xq = SeedParaclinical(db, "DV-000005", "X-quang ngực", 120000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, "Chỉ định thường quy",
            new[] { new CreateLabOrderItemRequest(xn.Id), new CreateLabOrderItemRequest(xq.Id) }));

        Assert.True(result.IsSuccess);
        Assert.Equal("CLS-000001", result.Value.Code);
        Assert.Equal(patientId, result.Value.PatientId);
        Assert.Equal(doctorId, result.Value.DoctorId);
        Assert.Equal(LabOrderStatus.Ordered, result.Value.Status);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(200000m, result.Value.TotalAmount);
        Assert.All(result.Value.Items, i => Assert.Equal(LabOrderItemStatus.Pending, i.Status));
        Assert.Contains(result.Value.Items, i => i.ServiceName == "Công thức máu" && i.UnitPrice == 80000m);
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldSnapshotPrice_NotAffectedByLaterChange()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var svc = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(svc.Id) }));

        svc.UpdateDetails("Công thức máu", 999000m, null, ServiceCategory.Paraclinical);
        await db.SaveChangesAsync();

        var reread = await service.GetByIdAsync(created.Value.Id);
        Assert.Equal(80000m, Assert.Single(reread.Value.Items).UnitPrice);
    }

    [Fact]
    public async Task CreateFromEncounter_WithNonParaclinicalService_ShouldReturnValidation()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var consult = new ServicePrice("DV-000001", "Khám tổng quát", 150000m, null, ServiceCategory.Consultation);
        db.ServicePrices.Add(consult);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(consult.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Paraclinical.ServiceNotParaclinical", result.Error.Code);
    }

    [Fact]
    public async Task CreateFromEncounter_WhenServiceMissing_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(Guid.NewGuid()) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ServicePrice.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateFromEncounter_WhenEncounterCompleted_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        encounter.Complete();
        var svc = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(svc.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Paraclinical.EncounterNotDraft", result.Error.Code);
    }

    [Fact]
    public async Task SetItemResult_PartialThenAll_ShouldMoveInProgressThenCompleted()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        var xq = SeedParaclinical(db, "DV-000005", "X-quang ngực", 120000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null,
            new[] { new CreateLabOrderItemRequest(xn.Id), new CreateLabOrderItemRequest(xq.Id) }));
        var items = created.Value.Items;
        await MarkPaidAsync(db, created.Value.Id);

        // Nhập kết quả mục đầu → InProgress.
        var first = await service.SetItemResultAsync(created.Value.Id, items[0].Id,
            new SetLabResultRequest("WBC 7.5", "Bình thường"));
        Assert.True(first.IsSuccess);
        Assert.Equal(LabOrderStatus.InProgress, first.Value.Status);
        Assert.Equal(LabOrderItemStatus.Completed, first.Value.Items.Single(i => i.Id == items[0].Id).Status);
        Assert.NotNull(first.Value.Items.Single(i => i.Id == items[0].Id).ResultedAt);

        // Nhập nốt mục còn lại → Completed.
        var second = await service.SetItemResultAsync(created.Value.Id, items[1].Id,
            new SetLabResultRequest("Không tổn thương", null));
        Assert.True(second.IsSuccess);
        Assert.Equal(LabOrderStatus.Completed, second.Value.Status);
    }

    [Fact]
    public async Task SetItemResult_AfterCompleted_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));
        var itemId = created.Value.Items[0].Id;
        await MarkPaidAsync(db, created.Value.Id);
        await service.SetItemResultAsync(created.Value.Id, itemId, new SetLabResultRequest("OK", null)); // → Completed

        var again = await service.SetItemResultAsync(created.Value.Id, itemId, new SetLabResultRequest("Sửa", null));

        Assert.True(again.IsFailure);
        Assert.Equal(ErrorType.Conflict, again.Error.Type);
        Assert.Equal("Paraclinical.InvalidTransition", again.Error.Code);
    }

    [Fact]
    public async Task SetItemResult_UnknownItem_ShouldReturnNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));
        await MarkPaidAsync(db, created.Value.Id);

        var result = await service.SetItemResultAsync(created.Value.Id, Guid.NewGuid(),
            new SetLabResultRequest("OK", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Paraclinical.ItemNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Cancel_ShouldMoveCancelled_AndBlockFurtherResult()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));
        await MarkPaidAsync(db, created.Value.Id);

        var cancel = await service.CancelAsync(created.Value.Id);
        Assert.True(cancel.IsSuccess);
        Assert.Equal(LabOrderStatus.Cancelled, cancel.Value.Status);

        var afterCancel = await service.SetItemResultAsync(created.Value.Id, created.Value.Items[0].Id,
            new SetLabResultRequest("OK", null));
        Assert.True(afterCancel.IsFailure);
        Assert.Equal(ErrorType.Conflict, afterCancel.Error.Type);
        Assert.Equal("Paraclinical.InvalidTransition", afterCancel.Error.Code);
    }

    [Fact]
    public async Task SetItemResult_WhenNotPaid_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        // Chưa thu phí CLS → chặn nhập kết quả (gating PAY-01).
        var result = await service.SetItemResultAsync(created.Value.Id, created.Value.Items[0].Id,
            new SetLabResultRequest("OK", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Paraclinical.NotPaid", result.Error.Code);
    }

    [Fact]
    public async Task SetItemResult_AfterPaid_ShouldSucceed()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));
        await MarkPaidAsync(db, created.Value.Id);

        var result = await service.SetItemResultAsync(created.Value.Id, created.Value.Items[0].Id,
            new SetLabResultRequest("OK", null));

        Assert.True(result.IsSuccess);
        Assert.Equal(LabOrderStatus.Completed, result.Value.Status);
        Assert.NotNull(result.Value.PaidAt);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByEncounterAndStatus()
    {
        var db = TestDbContext.CreateInMemory();
        var enc1 = SeedDraftEncounter(db);
        var enc2 = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-000004", "Công thức máu", 80000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            enc1.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));
        await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            enc2.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        var byEncounter = await service.GetListAsync(1, 20, enc1.Id, null, null);
        Assert.Equal(1, byEncounter.Value.TotalCount);

        var ordered = await service.GetListAsync(1, 20, null, null, LabOrderStatus.Ordered);
        Assert.Equal(2, ordered.Value.TotalCount);

        var completed = await service.GetListAsync(1, 20, null, null, LabOrderStatus.Completed);
        Assert.Equal(0, completed.Value.TotalCount);
    }

    // ── REF-03: Huỷ phiếu CLS đã thu + hoàn tiền HĐ CLS ────────────────────

    [Fact]
    public async Task CancelAsync_PaidOrderWithNoResults_ShouldCancelAndRefundInvoice()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-CLS001", "Điện giải đồ", 120000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        // Mô phỏng lập hoá đơn + thu tiền CLS
        var invoice = new Invoice("HD-000001", encounter.PatientId, null, null,
            new[] { new InvoiceItem(InvoiceItemType.Paraclinical, "Điện giải đồ", 120000m, 1, xn.Id) },
            labOrderId: created.Value.Id);
        invoice.Pay(PaymentMethod.Cash, DateTimeOffset.UtcNow);
        db.Invoices.Add(invoice);
        await MarkPaidAsync(db, created.Value.Id);

        var result = await service.CancelAsync(created.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(LabOrderStatus.Cancelled, result.Value.Status);

        // Hoá đơn CLS phải được hoàn tiền
        var reloadedInvoice = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Refunded, reloadedInvoice!.Status);
        Assert.NotNull(reloadedInvoice.RefundedAt);
    }

    [Fact]
    public async Task CancelAsync_PaidOrderWithPartialResults_ShouldReturnConflict()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        // Cần 2 dịch vụ để nhập kết quả 1 item → InProgress (không phải Completed)
        var xn1 = SeedParaclinical(db, "DV-CLS002", "X-quang ngực", 200000m);
        var xn2 = SeedParaclinical(db, "DV-CLS009", "Siêu âm bụng", 150000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[]
            {
                new CreateLabOrderItemRequest(xn1.Id),
                new CreateLabOrderItemRequest(xn2.Id)
            }));
        await MarkPaidAsync(db, created.Value.Id);

        // Nhập kết quả một mục trong hai → chuyển sang InProgress
        await service.SetItemResultAsync(created.Value.Id, created.Value.Items[0].Id,
            new SetLabResultRequest("Bình thường", null));

        var result = await service.CancelAsync(created.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Paraclinical.HasPartialResults", result.Error.Code);
    }

    [Fact]
    public async Task CancelAsync_UnpaidOrder_ShouldCancelWithoutRefund()
    {
        var db = TestDbContext.CreateInMemory();
        var encounter = SeedDraftEncounter(db);
        var xn = SeedParaclinical(db, "DV-CLS003", "Siêu âm bụng", 150000m);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(xn.Id) }));

        var result = await service.CancelAsync(created.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(LabOrderStatus.Cancelled, result.Value.Status);
    }
}
