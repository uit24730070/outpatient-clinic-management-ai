using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Encounters;

/// <summary>Kiểm thử liên kết dòng đơn thuốc với danh mục (PH-06, ADR 0011).</summary>
public sealed class EncounterMedicationLinkTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);

    private static EncounterService Setup(out Appointment appt, out Guid medId)
    {
        var db = TestDbContext.CreateInMemory();
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

    [Fact]
    public async Task Create_ShouldLinkMedication_WhenMedicationIdProvided()
    {
        var service = Setup(out var appt, out var medId);

        var result = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Viêm họng", null,
            new[]
            {
                new PrescriptionItemRequest("Paracetamol", "500mg", 10, null, medId),
                new PrescriptionItemRequest("Thuốc ngoài", "1 viên", 2, null) // MedicationId = null
            }));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.PrescriptionItems.Count);
        // Đơn hỗn hợp: một dòng gắn danh mục, một dòng ngoài danh mục (thứ tự không đảm bảo).
        Assert.Contains(result.Value.PrescriptionItems, i => i.MedicationId == medId && i.DrugName == "Paracetamol");
        Assert.Contains(result.Value.PrescriptionItems, i => i.MedicationId == null && i.DrugName == "Thuốc ngoài");
    }

    [Fact]
    public async Task Create_ShouldFail_WhenMedicationIdDoesNotExist()
    {
        var service = Setup(out var appt, out _);

        var result = await service.CreateAsync(new CreateEncounterRequest(
            appt.Id, null, "Viêm họng", null,
            new[] { new PrescriptionItemRequest("Ẩn danh", "1 viên", 1, null, Guid.NewGuid()) }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Pharmacy.MedicationNotFound", result.Error.Code);
    }
}
