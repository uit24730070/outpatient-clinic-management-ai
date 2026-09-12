using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Application.Assistant.Tools;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Shared.Results;

namespace UnitTests.Assistant;

public sealed class AssistantToolTests
{
    /// <summary>Fake service lịch khám: ghi lại filter nhận được, trả trang rỗng.</summary>
    private sealed class CapturingAppointmentService : IAppointmentService
    {
        public AppointmentFilter? LastFilter { get; private set; }

        public Task<Result<PagedResult<AppointmentDto>>> GetListAsync(AppointmentFilter filter, CancellationToken ct = default)
        {
            LastFilter = filter;
            Result<PagedResult<AppointmentDto>> ok =
                new PagedResult<AppointmentDto>(Array.Empty<AppointmentDto>(), 1, filter.PageSize, 0);
            return Task.FromResult(ok);
        }

        public Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto?>> GetLastForPatientAsync(Guid patientId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> UpdateAsync(Guid id, UpdateAppointmentRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> CheckInAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> StartAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> CompleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> CancelAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> MarkNoShowAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    /// <summary>Fake service phiếu khám: ghi lại filter nhận được, trả trang rỗng.</summary>
    private sealed class CapturingEncounterService : IEncounterService
    {
        public EncounterFilter? LastFilter { get; private set; }

        public Task<Result<PagedResult<EncounterDto>>> GetListAsync(EncounterFilter filter, CancellationToken ct = default)
        {
            LastFilter = filter;
            Result<PagedResult<EncounterDto>> ok =
                new PagedResult<EncounterDto>(Array.Empty<EncounterDto>(), 1, filter.PageSize, 0);
            return Task.FromResult(ok);
        }

        public Task<Result<EncounterDto>> CreateAsync(CreateEncounterRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<EncounterDto>> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<EncounterDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<EncounterDto>> UpdateAsync(Guid id, UpdateEncounterRequest r, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<EncounterDto>> CompleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<EncounterDto>> DispenseAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static readonly Guid DoctorProfileId = Guid.NewGuid();
    private static AssistantContext DoctorContext() => new(Guid.NewGuid(), "Doctor", DoctorProfileId);
    private static AssistantContext ReceptionistContext() => new(Guid.NewGuid(), "Receptionist", null);

    [Fact]
    public async Task ListAppointments_ShouldForceDoctorId_WhenCallerIsDoctor()
    {
        var svc = new CapturingAppointmentService();
        var tool = new ListAppointmentsTool(svc);

        // Bác sĩ cố truyền doctorId khác → phải bị ép về hồ sơ của chính mình.
        await tool.ExecuteAsync($"{{\"doctorId\":\"{Guid.NewGuid()}\"}}", DoctorContext());

        Assert.Equal(DoctorProfileId, svc.LastFilter!.DoctorId);
    }

    [Fact]
    public async Task ListAppointments_ShouldReturnError_WhenDoctorNotLinked()
    {
        var svc = new CapturingAppointmentService();
        var tool = new ListAppointmentsTool(svc);
        var unlinkedDoctor = new AssistantContext(Guid.NewGuid(), "Doctor", null);

        var output = await tool.ExecuteAsync("{}", unlinkedDoctor);

        Assert.Contains("chưa gắn hồ sơ", output);
        Assert.Null(svc.LastFilter); // không gọi service khi thiếu hồ sơ
    }

    [Fact]
    public async Task ListAppointments_ShouldHonorFilters_ForReceptionist()
    {
        var svc = new CapturingAppointmentService();
        var tool = new ListAppointmentsTool(svc);
        var doctorId = Guid.NewGuid();

        await tool.ExecuteAsync(
            $"{{\"date\":\"2026-08-10\",\"doctorId\":\"{doctorId}\",\"status\":\"Scheduled\"}}",
            ReceptionistContext());

        Assert.Equal(new DateOnly(2026, 8, 10), svc.LastFilter!.Date);
        Assert.Equal(doctorId, svc.LastFilter.DoctorId);
        Assert.Equal(ClinicManagement.Domain.Appointments.AppointmentStatus.Scheduled, svc.LastFilter.Status);
    }

    [Fact]
    public async Task GetPatientEncounters_ShouldForceDoctorId_WhenCallerIsDoctor()
    {
        var svc = new CapturingEncounterService();
        var tool = new GetPatientEncountersTool(svc);
        var patientId = Guid.NewGuid();

        await tool.ExecuteAsync($"{{\"patientId\":\"{patientId}\"}}", DoctorContext());

        Assert.Equal(patientId, svc.LastFilter!.PatientId);
        Assert.Equal(DoctorProfileId, svc.LastFilter.DoctorId);
    }

    [Fact]
    public async Task GetPatientEncounters_ShouldNotFilterDoctor_ForReceptionist()
    {
        var svc = new CapturingEncounterService();
        var tool = new GetPatientEncountersTool(svc);
        var patientId = Guid.NewGuid();

        await tool.ExecuteAsync($"{{\"patientId\":\"{patientId}\"}}", ReceptionistContext());

        Assert.Equal(patientId, svc.LastFilter!.PatientId);
        Assert.Null(svc.LastFilter.DoctorId);
    }

    [Fact]
    public async Task GetPatientEncounters_ShouldReturnError_WhenPatientIdMissing()
    {
        var svc = new CapturingEncounterService();
        var tool = new GetPatientEncountersTool(svc);

        var output = await tool.ExecuteAsync("{}", ReceptionistContext());

        Assert.Contains("patientId", output);
        Assert.Null(svc.LastFilter);
    }
}
