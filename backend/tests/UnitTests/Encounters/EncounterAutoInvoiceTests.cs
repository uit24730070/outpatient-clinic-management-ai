using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using UnitTests.Common;

namespace UnitTests.Encounters;

/// <summary>
/// Tự lập hoá đơn thuốc khi chốt phiếu (thay cho việc phải bấm "Lập HĐ thuốc" thủ công ở
/// VisitDetailPage — dễ bị bỏ sót, xem trao đổi thực tế dẫn tới thay đổi này).
/// </summary>
public sealed class EncounterAutoInvoiceTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static EncounterService SetupWithInvoiceService(
        string dbName, out TestDbContext db, out Appointment appt, out Guid medId)
    {
        db = TestDbContext.CreateInMemory(dbName);
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        var med = new Medication("TH-000001", "Paracetamol 500mg", "Paracetamol", "viên", 10, null, 2000m);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.Medications.Add(med);
        appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        db.MedicationBatches.Add(new MedicationBatch(med.Id, "OK", Today.AddMonths(6), 10));
        db.SaveChanges();
        medId = med.Id;
        return new EncounterService(db, invoices: new InvoiceService(db));
    }

    [Fact]
    public async Task Complete_ShouldAutoCreateMedicationInvoice_WhenPrescriptionHasCatalogMedication()
    {
        var service = SetupWithInvoiceService(
            nameof(Complete_ShouldAutoCreateMedicationInvoice_WhenPrescriptionHasCatalogMedication),
            out var db, out var appt, out var medId);
        var created = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Viêm họng", null,
            new[] { new PrescriptionItemRequest("Paracetamol", "500mg", 4, null, medId) }));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.NotNull(completed.Value.MedicationInvoicedAt);

        var invoice = await db.Invoices.Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.EncounterId == created.Value.Id);
        Assert.NotNull(invoice);
        var item = Assert.Single(invoice!.Items);
        Assert.Equal(InvoiceItemType.Medication, item.ItemType);
        Assert.Equal(8000m, item.LineTotal); // 4 × 2.000đ
    }

    [Fact]
    public async Task Complete_ShouldNotCreateInvoice_WhenNoMedicationLinked()
    {
        var service = SetupWithInvoiceService(
            nameof(Complete_ShouldNotCreateInvoice_WhenNoMedicationLinked),
            out var db, out var appt, out _);
        var created = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Cảm cúm", null,
            new[] { new PrescriptionItemRequest("Thuốc ngoài danh mục", "1 viên", 3, null) }));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Null(completed.Value.MedicationInvoicedAt);
        Assert.Equal(0, await db.Invoices.CountAsync());
    }

    [Fact]
    public async Task Complete_ShouldNotCreateInvoice_WhenInvoiceServiceNotConfigured()
    {
        var db = TestDbContext.CreateInMemory(nameof(Complete_ShouldNotCreateInvoice_WhenInvoiceServiceNotConfigured));
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        var med = new Medication("TH-000001", "Paracetamol 500mg", "Paracetamol", "viên", 10, null, 2000m);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.Medications.Add(med);
        var appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        db.MedicationBatches.Add(new MedicationBatch(med.Id, "OK", Today.AddMonths(6), 10));
        db.SaveChanges();
        // Không truyền invoices (như test khác trong sprint) — best-effort phải bỏ qua, không lỗi.
        var service = new EncounterService(db);
        var created = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Viêm họng", null,
            new[] { new PrescriptionItemRequest("Paracetamol", "500mg", 4, null, med.Id) }));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Null(completed.Value.MedicationInvoicedAt);
        Assert.Equal(0, await db.Invoices.CountAsync());
    }
}
