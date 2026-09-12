using ClinicManagement.Application.Reports;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Domain.Users;
using UnitTests.Common;

namespace UnitTests.Reports;

public sealed class ReportServiceTests
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    /// <summary>Thời điểm 12:00 (giờ phòng khám) của một ngày — rơi chắc chắn vào ngày địa phương đó.</summary>
    private static DateTimeOffset At(DateOnly date) => new(date.ToDateTime(new TimeOnly(12, 0)), Offset);

    private static DateOnly TodayLocal() =>
        DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(Offset).DateTime);

    private static Patient SeedPatient(TestDbContext db, string code = "BN-000001")
    {
        var p = new Patient(code, "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(p);
        return p;
    }

    private static Doctor SeedDoctor(TestDbContext db, string code, Guid? userId = null)
    {
        var d = new Doctor(code, $"BS {code}", Guid.NewGuid(), null, null);
        if (userId is { } uid) d.AssignUser(uid);
        db.Doctors.Add(d);
        return d;
    }

    /// <summary>Phiếu khám với CreatedAt được đặt tường minh (InMemory không có interceptor audit như Infrastructure).</summary>
    private static Encounter SeedEncounter(TestDbContext db, Guid patientId, Guid doctorId, DateTimeOffset createdAt)
    {
        var e = new Encounter(Guid.NewGuid(), patientId, doctorId, null, "Cảm cúm", null) { CreatedAt = createdAt };
        db.Encounters.Add(e);
        return e;
    }

    private static Appointment CompletedAppt(Guid patientId, Guid doctorId, DateTimeOffset start)
    {
        var a = new Appointment(patientId, doctorId, start, start.AddMinutes(30), "Khám");
        a.CheckIn(); a.Start(); a.Complete();
        return a;
    }

    private static Invoice PaidInvoice(string code, Guid patientId, DateTimeOffset paidAt, params InvoiceItem[] items)
    {
        var inv = new Invoice(code, patientId, null, null, items);
        inv.Pay(PaymentMethod.Cash, paidAt);
        return inv;
    }

    [Fact]
    public async Task Overview_ShouldAggregateTodaySnapshot()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var doctor = SeedDoctor(db, "BS-000001");
        var now = At(TodayLocal());

        db.Appointments.Add(new Appointment(patient.Id, doctor.Id, now, now.AddMinutes(30), "Khám"));
        SeedEncounter(db, patient.Id, doctor.Id, now);
        db.Invoices.Add(PaidInvoice("HD-000001", patient.Id, now,
            new InvoiceItem(InvoiceItemType.ServiceFee, "Công khám", 120_000m, 1)));
        db.QueueTickets.Add(new QueueTicket(TodayLocal(), 1, patient.Id, null, null, null));
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetOverviewAsync();

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(1, dto.TotalActivePatients);
        Assert.Equal(1, dto.TotalDoctors);
        Assert.Equal(1, dto.EncountersToday);
        Assert.Equal(120_000m, dto.RevenueToday);
        Assert.Equal(1, dto.QueueWaiting);
        Assert.Equal(1, dto.AppointmentsToday);
        Assert.Equal(1, dto.AppointmentsByStatus.Scheduled);
    }

    [Fact]
    public async Task Overview_RevenueToday_ShouldIgnoreDraftInvoices()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var now = At(TodayLocal());

        db.Invoices.Add(PaidInvoice("HD-000001", patient.Id, now,
            new InvoiceItem(InvoiceItemType.ServiceFee, "Công khám", 100_000m, 1)));
        // Hoá đơn nháp: không tính vào doanh thu.
        db.Invoices.Add(new Invoice("HD-000002", patient.Id, null, null,
            new[] { new InvoiceItem(InvoiceItemType.Medication, "Thuốc", 50_000m, 1) }));
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetOverviewAsync();

        Assert.Equal(100_000m, result.Value.RevenueToday);
    }

    [Fact]
    public async Task AppointmentReport_ShouldGroupByDay_AndFillEmptyDays()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var doctor = SeedDoctor(db, "BS-000001");
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 3);

        db.Appointments.Add(new Appointment(patient.Id, doctor.Id, At(from), At(from).AddMinutes(30), "Khám"));
        db.Appointments.Add(CompletedAppt(patient.Id, doctor.Id, At(from)));
        db.Appointments.Add(new Appointment(patient.Id, doctor.Id, At(to), At(to).AddMinutes(30), "Khám"));
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetAppointmentReportAsync(from, to, null);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(3, dto.Days.Count); // đủ 3 ngày kể cả ngày giữa trống
        Assert.Equal(3, dto.Total);
        Assert.Equal(2, dto.Days[0].Total);
        Assert.Equal(1, dto.Days[0].ByStatus.Completed);
        Assert.Equal(0, dto.Days[1].Total); // 2026-09-02 trống
        Assert.Equal(1, dto.Days[2].Total);
    }

    [Fact]
    public async Task AppointmentReport_ShouldRejectInvalidRange()
    {
        var db = TestDbContext.CreateInMemory();
        var result = await new ReportService(db)
            .GetAppointmentReportAsync(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 1), null);

        Assert.True(result.IsFailure);
        Assert.Equal("Report.InvalidRange", result.Error.Code);
    }

    [Fact]
    public async Task DoctorProductivity_ShouldCountAppointmentsCompletedAndEncounters()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var d1 = SeedDoctor(db, "BS-000001");
        var d2 = SeedDoctor(db, "BS-000002");
        var today = TodayLocal();
        var at = At(today);

        db.Appointments.Add(new Appointment(patient.Id, d1.Id, at, at.AddMinutes(30), "Khám"));
        db.Appointments.Add(CompletedAppt(patient.Id, d1.Id, at));
        db.Appointments.Add(new Appointment(patient.Id, d2.Id, at, at.AddMinutes(30), "Khám"));
        SeedEncounter(db, patient.Id, d1.Id, at);
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetDoctorProductivityAsync(today, today, null, null);

        Assert.True(result.IsSuccess);
        var bs1 = result.Value.Single(x => x.DoctorId == d1.Id);
        var bs2 = result.Value.Single(x => x.DoctorId == d2.Id);
        Assert.Equal(2, bs1.TotalAppointments);
        Assert.Equal(1, bs1.CompletedAppointments);
        Assert.Equal(1, bs1.Encounters);
        Assert.Equal(1, bs2.TotalAppointments);
        Assert.Equal(0, bs2.Encounters);
    }

    [Fact]
    public async Task DoctorProductivity_ShouldRestrictToLinkedDoctor_WhenUserIsDoctor()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var userId = Guid.NewGuid();
        var mine = SeedDoctor(db, "BS-000001", userId);
        var other = SeedDoctor(db, "BS-000002");
        var today = TodayLocal();
        var at = At(today);
        db.Appointments.Add(new Appointment(patient.Id, mine.Id, at, at.AddMinutes(30), "Khám"));
        db.Appointments.Add(new Appointment(patient.Id, other.Id, at, at.AddMinutes(30), "Khám"));
        await db.SaveChangesAsync();

        var result = await new ReportService(db)
            .GetDoctorProductivityAsync(today, today, null, userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(mine.Id, result.Value[0].DoctorId);
    }

    [Fact]
    public async Task DoctorProductivity_ShouldReturnEmpty_WhenDoctorUserNotLinked()
    {
        var db = TestDbContext.CreateInMemory();
        SeedDoctor(db, "BS-000001");
        await db.SaveChangesAsync();

        var result = await new ReportService(db)
            .GetDoctorProductivityAsync(null, null, null, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task RevenueReport_ShouldSplitByItemType_AndOnlyPaid()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = SeedPatient(db);
        var day = new DateOnly(2026, 9, 5);

        db.Invoices.Add(PaidInvoice("HD-000001", patient.Id, At(day),
            new InvoiceItem(InvoiceItemType.ServiceFee, "Công khám", 100_000m, 1),
            new InvoiceItem(InvoiceItemType.Medication, "Thuốc", 30_000m, 2),
            new InvoiceItem(InvoiceItemType.Paraclinical, "Xét nghiệm", 80_000m, 1)));
        // Nháp: bỏ qua.
        db.Invoices.Add(new Invoice("HD-000002", patient.Id, null, null,
            new[] { new InvoiceItem(InvoiceItemType.Other, "Khác", 999_000m, 1) }));
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetRevenueReportAsync(day, day);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(100_000m, dto.ServiceFeeTotal);
        Assert.Equal(60_000m, dto.MedicationTotal);
        Assert.Equal(80_000m, dto.ParaclinicalTotal);
        Assert.Equal(0m, dto.OtherTotal);
        Assert.Equal(240_000m, dto.GrandTotal);
        Assert.Single(dto.Days);
        Assert.Equal(240_000m, dto.Days[0].Total);
    }
}
