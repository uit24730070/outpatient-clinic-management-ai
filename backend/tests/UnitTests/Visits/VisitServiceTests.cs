using ClinicManagement.Application.Visits;
using ClinicManagement.Application.Visits.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Visits;

public sealed class VisitServiceTests
{
    private static readonly DateTimeOffset Base =
        new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private static VisitService CreateService(
        out TestDbContext db, out Guid patientId, out Guid doctorA, out Guid doctorB, out Guid consultId)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var d1 = new Doctor("BS-000001", "BS. Nội", Guid.NewGuid(), null, null);
        var d2 = new Doctor("BS-000002", "BS. Da liễu", Guid.NewGuid(), null, null);
        var consult = new ServicePrice("DV-000001", "Khám tổng quát", 150000m, null, ServiceCategory.Consultation);
        db.Patients.Add(patient);
        db.Doctors.AddRange(d1, d2);
        db.ServicePrices.Add(consult);
        db.SaveChanges();

        patientId = patient.Id;
        doctorA = d1.Id;
        doctorB = d2.Id;
        consultId = consult.Id;
        return new VisitService(db);
    }

    private static VisitServiceLine Line(Guid doctorId, Guid? serviceId, DateTimeOffset? start = null) =>
        new(doctorId, start ?? Base, (start ?? Base).AddMinutes(30), "Khám", serviceId);

    [Fact]
    public async Task CreateAsync_ShouldCreateVisitWithMultipleConsultations()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out var doctorB, out var consultId);

        var result = await service.CreateAsync(new CreateVisitRequest(
            patientId, "Lượt sáng", new[] { Line(doctorA, consultId), Line(doctorB, consultId) }));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("LK-", result.Value.Code);
        Assert.Equal(VisitStatus.Open, result.Value.Status);
        Assert.Equal(2, result.Value.Appointments.Count);
        Assert.All(result.Value.Appointments, a => Assert.Equal(150000m, a.ServicePrice));
        Assert.Equal("Nguyễn Văn A", result.Value.PatientName);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenNoServices()
    {
        var service = CreateService(out _, out var patientId, out _, out _, out _);

        var result = await service.CreateAsync(new CreateVisitRequest(patientId, null, Array.Empty<VisitServiceLine>()));

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NoServices", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenPatientMissing()
    {
        var service = CreateService(out _, out _, out var doctorA, out _, out var consultId);

        var result = await service.CreateAsync(new CreateVisitRequest(
            Guid.NewGuid(), null, new[] { Line(doctorA, consultId) }));

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.PatientNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDoctorMissing()
    {
        var service = CreateService(out _, out var patientId, out _, out _, out var consultId);

        var result = await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(Guid.NewGuid(), consultId) }));

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.DoctorNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenServiceNotConsultation()
    {
        var service = CreateService(out var db, out var patientId, out var doctorA, out _, out _);
        var cls = new ServicePrice("DV-000009", "X-quang", 200000m, null, ServiceCategory.Paraclinical);
        db.ServicePrices.Add(cls);
        db.SaveChanges();

        var result = await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, cls.Id) }));

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.ServiceNotConsultation", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenSameDoctorOverlapsWithinVisit()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out _, out var consultId);

        // Hai dịch vụ cùng bác sĩ, cùng khung giờ trong một lượt → chống trùng.
        var result = await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId), Line(doctorA, consultId) }));

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.Overlap", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenTwoDoctorsSameTime()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out var doctorB, out var consultId);

        // Hai bác sĩ khác nhau, cùng khung giờ (song song phòng) → hợp lệ.
        var result = await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId), Line(doctorB, consultId) }));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Appointments.Count);
    }

    [Fact]
    public async Task AddServiceAsync_ShouldAppendToOpenVisit()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out var doctorB, out var consultId);
        var visit = (await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId) }))).Value;

        var result = await service.AddServiceAsync(visit.Id, new AddVisitServiceRequest(
            doctorB, Base.AddHours(1), Base.AddHours(1).AddMinutes(30), "Khám thêm", consultId));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Appointments.Count);
    }

    [Fact]
    public async Task AddServiceAsync_ShouldFail_WhenVisitClosed()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out var doctorB, out var consultId);
        var visit = (await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId) }))).Value;
        await service.CloseAsync(visit.Id);

        var result = await service.AddServiceAsync(visit.Id, new AddVisitServiceRequest(
            doctorB, Base.AddHours(1), Base.AddHours(1).AddMinutes(30), null, consultId));

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NotOpen", result.Error.Code);
    }

    [Fact]
    public async Task CloseAsync_ShouldFail_WhenAlreadyClosed()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out _, out var consultId);
        var visit = (await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId) }))).Value;
        await service.CloseAsync(visit.Id);

        var again = await service.CloseAsync(visit.Id);

        Assert.True(again.IsFailure);
        Assert.Equal("Visit.InvalidTransition", again.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldAggregateBillingAcrossVisit()
    {
        var service = CreateService(out var db, out var patientId, out var doctorA, out var doctorB, out var consultId);
        var visit = (await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId), Line(doctorB, consultId) }))).Value;

        // Một hoá đơn cho mỗi dịch vụ khám (gắn AppointmentId), một cái đã thu.
        var appt1 = visit.Appointments[0].Id;
        var appt2 = visit.Appointments[1].Id;
        var inv1 = new Invoice("HD-000001", patientId, null, null,
            new[] { new InvoiceItem(InvoiceItemType.ServiceFee, "Khám tổng quát", 150000m, 1, null) }, appt1);
        var inv2 = new Invoice("HD-000002", patientId, null, null,
            new[] { new InvoiceItem(InvoiceItemType.ServiceFee, "Khám tổng quát", 150000m, 1, null) }, appt2);
        inv1.Pay(PaymentMethod.Cash, DateTimeOffset.UtcNow);
        db.Invoices.AddRange(inv1, inv2);
        db.SaveChanges();

        var detail = (await service.GetByIdAsync(visit.Id)).Value;

        Assert.Equal(300000m, detail.TotalBilled);
        Assert.Equal(150000m, detail.TotalPaid);
        Assert.Equal(150000m, detail.TotalOutstanding);
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnServiceCount()
    {
        var service = CreateService(out _, out var patientId, out var doctorA, out var doctorB, out var consultId);
        await service.CreateAsync(new CreateVisitRequest(
            patientId, null, new[] { Line(doctorA, consultId), Line(doctorB, consultId) }));

        var list = await service.GetListAsync(1, 20, patientId, null, null);

        Assert.True(list.IsSuccess);
        Assert.Single(list.Value.Items);
        Assert.Equal(2, list.Value.Items[0].ServiceCount);
    }
}
