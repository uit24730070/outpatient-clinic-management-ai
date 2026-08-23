using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using UnitTests.Common;

namespace UnitTests.Billing;

/// <summary>Gom & thu viện phí theo lượt tiếp đón (ADR 0017): Invoice.VisitId suy từ lịch gắn hoá đơn.</summary>
public sealed class VisitBillingTests
{
    private static readonly DateTimeOffset Base = new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);

    private static InvoiceService Setup(
        out TestDbContext db, out Guid patientId, out Guid visitId, out Guid apptId1, out Guid apptId2, out Guid svcId)
    {
        db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var visit = new Visit("LK-000001", patient.Id, null);
        var svc = new ServicePrice("DV-000001", "Khám tổng quát", 150000m, null, ServiceCategory.Consultation);
        var a1 = new Appointment(patient.Id, Guid.NewGuid(), Base, Base.AddMinutes(30), null, svc.Id, svc.Name, svc.UnitPrice, visit.Id);
        var a2 = new Appointment(patient.Id, Guid.NewGuid(), Base.AddHours(1), Base.AddHours(1).AddMinutes(30), null, svc.Id, svc.Name, svc.UnitPrice, visit.Id);
        db.Patients.Add(patient);
        db.Visits.Add(visit);
        db.ServicePrices.Add(svc);
        db.Appointments.AddRange(a1, a2);
        db.SaveChanges();

        patientId = patient.Id; visitId = visit.Id; apptId1 = a1.Id; apptId2 = a2.Id; svcId = svc.Id;
        return new InvoiceService(db);
    }

    private static CreateInvoiceRequest Req(Guid patientId, Guid apptId, Guid svcId) =>
        new(patientId, null, new[] { new CreateInvoiceItemRequest(svcId, 1) }, apptId);

    [Fact]
    public async Task CreateAsync_ShouldDeriveVisitId_FromAppointment()
    {
        var svc = Setup(out _, out var patientId, out var visitId, out var apptId1, out _, out var svcId);

        var result = await svc.CreateAsync(Req(patientId, apptId1, svcId));

        Assert.True(result.IsSuccess);
        Assert.Equal(visitId, result.Value.VisitId);
    }

    [Fact]
    public async Task GetByVisitAsync_ShouldAggregateAllInvoicesOfVisit()
    {
        var svc = Setup(out _, out var patientId, out var visitId, out var apptId1, out var apptId2, out var svcId);
        await svc.CreateAsync(Req(patientId, apptId1, svcId));
        await svc.CreateAsync(Req(patientId, apptId2, svcId));

        var result = await svc.GetByVisitAsync(visitId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Invoices.Count);
        Assert.Equal(300000m, result.Value.TotalBilled);
        Assert.Equal(0m, result.Value.TotalPaid);
        Assert.Equal(300000m, result.Value.TotalOutstanding);
    }

    [Fact]
    public async Task PayVisitAsync_ShouldPayAllDraftInvoices()
    {
        var svc = Setup(out _, out var patientId, out var visitId, out var apptId1, out var apptId2, out var svcId);
        await svc.CreateAsync(Req(patientId, apptId1, svcId));
        await svc.CreateAsync(Req(patientId, apptId2, svcId));

        var result = await svc.PayVisitAsync(visitId, new PayInvoiceRequest(PaymentMethod.Cash));

        Assert.True(result.IsSuccess);
        Assert.Equal(300000m, result.Value.TotalPaid);
        Assert.Equal(0m, result.Value.TotalOutstanding);
        Assert.All(result.Value.Invoices, i => Assert.Equal(InvoiceStatus.Paid, i.Status));
    }

    [Fact]
    public async Task PayVisitAsync_ShouldSkipNonDraft_AndNotFailWhenNothingToPay()
    {
        var svc = Setup(out _, out var patientId, out var visitId, out var apptId1, out _, out var svcId);
        var inv = await svc.CreateAsync(Req(patientId, apptId1, svcId));
        await svc.CancelAsync(inv.Value.Id); // Cancelled → không thu, không tính TotalBilled

        var result = await svc.PayVisitAsync(visitId, new PayInvoiceRequest(PaymentMethod.Cash));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.TotalBilled);
        Assert.Equal(0m, result.Value.TotalPaid);
    }
}
