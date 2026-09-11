using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Doctors;

/// <summary>Quản lý lịch làm việc (mẫu tuần) của một bác sĩ.</summary>
public interface IDoctorScheduleService
{
    Task<Result<IReadOnlyList<DoctorWorkScheduleDto>>> GetByDoctorAsync(
        Guid doctorId, CancellationToken ct = default);
    Task<Result<DoctorWorkScheduleDto>> CreateAsync(
        Guid doctorId, CreateDoctorScheduleRequest request, CancellationToken ct = default);
    Task<Result<DoctorWorkScheduleDto>> UpdateAsync(
        Guid doctorId, Guid scheduleId, UpdateDoctorScheduleRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid doctorId, Guid scheduleId, CancellationToken ct = default);
}
