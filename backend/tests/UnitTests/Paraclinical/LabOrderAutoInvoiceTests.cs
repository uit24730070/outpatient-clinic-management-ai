using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Paraclinical;
using ClinicManagement.Application.Paraclinical.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using UnitTests.Common;

namespace UnitTests.Paraclinical;

/// <summary>
/// Tự lập hoá đơn CLS ngay khi bác sĩ chỉ định (thay cho việc phải chờ Lễ tân/Admin bấm "Lập HĐ CLS"
/// thủ công — trước đây Lễ tân không có đường vào màn khám nên bị kẹt, chỉ Admin làm được).
/// </summary>
public sealed class LabOrderAutoInvoiceTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);

    private static LabOrderService SetupWithInvoiceService(
        string dbName, out TestDbContext db, out Encounter encounter, out Guid servicePriceId)
    {
        db = TestDbContext.CreateInMemory(dbName);
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        var service = new ServicePrice("DV-CLS001", "Siêu âm ổ bụng", 150_000m, null, ServiceCategory.Paraclinical);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.ServicePrices.Add(service);
        var appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        encounter = new Encounter(appt.Id, patient.Id, doctor.Id, null, "Đau bụng", null);
        db.Encounters.Add(encounter);
        db.SaveChanges();
        servicePriceId = service.Id;

        return new LabOrderService(db, invoices: new InvoiceService(db));
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldAutoCreateInvoice()
    {
        var service = SetupWithInvoiceService(
            nameof(CreateFromEncounter_ShouldAutoCreateInvoice), out var db, out var encounter, out var servicePriceId);

        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(servicePriceId) }));

        Assert.True(created.IsSuccess);
        Assert.NotNull(created.Value.InvoicedAt);

        var invoice = await db.Invoices.Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.LabOrderId == created.Value.Id);
        Assert.NotNull(invoice);
        var item = Assert.Single(invoice!.Items);
        Assert.Equal(InvoiceItemType.Paraclinical, item.ItemType);
        Assert.Equal(150_000m, item.LineTotal);
    }

    [Fact]
    public async Task CreateFromEncounter_ShouldNotCreateInvoice_WhenInvoiceServiceNotConfigured()
    {
        var db = TestDbContext.CreateInMemory(
            nameof(CreateFromEncounter_ShouldNotCreateInvoice_WhenInvoiceServiceNotConfigured));
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        var svc = new ServicePrice("DV-CLS001", "Siêu âm ổ bụng", 150_000m, null, ServiceCategory.Paraclinical);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.ServicePrices.Add(svc);
        var appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        var encounter = new Encounter(appt.Id, patient.Id, doctor.Id, null, "Đau bụng", null);
        db.Encounters.Add(encounter);
        db.SaveChanges();
        // Không truyền invoices — best-effort phải bỏ qua, không lỗi (giữ nút "Lập HĐ CLS" thủ công).
        var service = new LabOrderService(db);

        var created = await service.CreateFromEncounterAsync(new CreateLabOrderRequest(
            encounter.Id, null, new[] { new CreateLabOrderItemRequest(svc.Id) }));

        Assert.True(created.IsSuccess);
        Assert.Null(created.Value.InvoicedAt);
        Assert.Equal(0, await db.Invoices.CountAsync());
    }
}
