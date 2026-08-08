using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Encounters;

public sealed class EncounterServiceTests
{
    private static readonly DateTimeOffset Base =
        new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Tạo service + một lịch khám ở trạng thái InProgress (sẵn sàng lập phiếu).</summary>
    private static EncounterService CreateServiceWithInProgressAppointment(
        out TestDbContext db, out Appointment appointment)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);

        appointment = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), "Khám tổng quát");
        appointment.CheckIn();
        appointment.Start(); // → InProgress
        db.Appointments.Add(appointment);
        db.SaveChanges();

        return new EncounterService(db);
    }

    private static CreateEncounterRequest ValidRequest(Guid appointmentId) =>
        new(appointmentId, "Sốt, ho", "Viêm họng cấp", "Nghỉ ngơi",
            new[] { new PrescriptionItemRequest("Paracetamol", "500mg", 10, "Ngày 2 lần") });

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WithSnapshotAndItems()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);

        var result = await service.CreateAsync(ValidRequest(appt.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(EncounterStatus.Draft, result.Value.Status);
        Assert.Equal(appt.PatientId, result.Value.PatientId);
        Assert.Equal("Nguyễn Văn A", result.Value.PatientName);
        Assert.Equal("BS. Trần B", result.Value.DoctorName);
        Assert.Equal("Viêm họng cấp", result.Value.Diagnosis);
        Assert.Single(result.Value.PrescriptionItems);
        Assert.Equal("Paracetamol", result.Value.PrescriptionItems[0].DrugName);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAppointmentMissing()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out _);

        var result = await service.CreateAsync(ValidRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Encounter.AppointmentNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAppointmentNotInProgress()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        // Lịch mới đặt (Scheduled), chưa khám.
        var appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();
        var service = new EncounterService(db);

        var result = await service.CreateAsync(ValidRequest(appt.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Encounter.AppointmentNotInProgress", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenEncounterAlreadyExists()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);
        await service.CreateAsync(ValidRequest(appt.Id));

        var duplicate = await service.CreateAsync(ValidRequest(appt.Id));

        Assert.True(duplicate.IsFailure);
        Assert.Equal(ErrorType.Conflict, duplicate.Error.Type);
        Assert.Equal("Encounter.AlreadyExists", duplicate.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceItems_WhenDraft()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);
        var created = await service.CreateAsync(ValidRequest(appt.Id));

        var updated = await service.UpdateAsync(created.Value.Id, new UpdateEncounterRequest(
            "Sốt cao", "Cúm mùa", null,
            new[]
            {
                new PrescriptionItemRequest("Oseltamivir", "75mg", 10, "Ngày 2 lần"),
                new PrescriptionItemRequest("Vitamin C", "1000mg", 5, null)
            }));

        Assert.True(updated.IsSuccess);
        Assert.Equal("Cúm mùa", updated.Value.Diagnosis);
        Assert.Equal(2, updated.Value.PrescriptionItems.Count);
        Assert.DoesNotContain(updated.Value.PrescriptionItems, i => i.DrugName == "Paracetamol");
    }

    [Fact]
    public async Task CompleteAsync_ShouldCompleteEncounterAndAppointment()
    {
        var service = CreateServiceWithInProgressAppointment(out var db, out var appt);
        var created = await service.CreateAsync(ValidRequest(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        Assert.Equal(EncounterStatus.Completed, completed.Value.Status);

        var reloadedAppt = await db.Appointments.FindAsync(appt.Id);
        Assert.Equal(AppointmentStatus.Completed, reloadedAppt!.Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenCompleted()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);
        var created = await service.CreateAsync(ValidRequest(appt.Id));
        await service.CompleteAsync(created.Value.Id);

        var update = await service.UpdateAsync(created.Value.Id, new UpdateEncounterRequest(
            null, "Đổi chẩn đoán", null, null));

        Assert.True(update.IsFailure);
        Assert.Equal(ErrorType.Conflict, update.Error.Type);
        Assert.Equal("Encounter.InvalidTransition", update.Error.Code);
    }

    [Fact]
    public async Task CompleteAsync_ShouldFail_WhenAlreadyCompleted()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);
        var created = await service.CreateAsync(ValidRequest(appt.Id));
        await service.CompleteAsync(created.Value.Id);

        var again = await service.CompleteAsync(created.Value.Id);

        Assert.True(again.IsFailure);
        Assert.Equal("Encounter.InvalidTransition", again.Error.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByPatient_OrderedNewestFirst()
    {
        var service = CreateServiceWithInProgressAppointment(out var db, out var appt1);
        var patientId = appt1.PatientId;
        var doctorId = appt1.DoctorId;
        await service.CreateAsync(ValidRequest(appt1.Id));

        // Lịch thứ hai của cùng bệnh nhân → phiếu thứ hai.
        var appt2 = new Appointment(patientId, doctorId, Base.AddHours(2), Base.AddHours(2).AddMinutes(30), null);
        appt2.CheckIn();
        appt2.Start();
        db.Appointments.Add(appt2);
        await db.SaveChangesAsync();
        await service.CreateAsync(ValidRequest(appt2.Id));

        var list = await service.GetListAsync(new EncounterFilter(PatientId: patientId));

        Assert.True(list.IsSuccess);
        Assert.Equal(2, list.Value.TotalCount);

        // Bệnh nhân khác không có phiếu.
        var other = await service.GetListAsync(new EncounterFilter(PatientId: Guid.NewGuid()));
        Assert.Equal(0, other.Value.TotalCount);
    }

    [Fact]
    public async Task GetByAppointmentAsync_ShouldReturnEncounter_OrNotFound()
    {
        var service = CreateServiceWithInProgressAppointment(out _, out var appt);
        await service.CreateAsync(ValidRequest(appt.Id));

        var found = await service.GetByAppointmentAsync(appt.Id);
        Assert.True(found.IsSuccess);
        Assert.Equal(appt.Id, found.Value.AppointmentId);

        var missing = await service.GetByAppointmentAsync(Guid.NewGuid());
        Assert.True(missing.IsFailure);
        Assert.Equal(ErrorType.NotFound, missing.Error.Type);
    }
}
