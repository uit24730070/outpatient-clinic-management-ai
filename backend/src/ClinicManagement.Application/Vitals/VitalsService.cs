using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.Domain.Clinical;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Vitals;

public sealed class VitalsService : IVitalsService
{
    private readonly IAppDbContext _db;

    public VitalsService(IAppDbContext db) => _db = db;

    public async Task<Result<VitalsDto>> UpsertAsync(
        Guid appointmentId, UpsertVitalsRequest request, Guid measuredBy, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
        if (appointment is null)
            return Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {appointmentId}.");

        var vitals = await _db.Vitals.FirstOrDefaultAsync(v => v.AppointmentId == appointmentId, ct);
        if (vitals is null)
        {
            vitals = new Domain.Clinical.Vitals(appointmentId, appointment.PatientId, measuredBy);
            _db.Vitals.Add(vitals);
        }

        vitals.Update(
            request.HeightCm,
            request.WeightKg,
            request.TemperatureC,
            request.Pulse,
            request.BloodPressureSystolic,
            request.BloodPressureDiastolic,
            request.SpO2,
            request.RespiratoryRate,
            NormalizeOptional(request.Notes),
            measuredBy);

        await _db.SaveChangesAsync(ct);
        return await ToDtoAsync(vitals, ct);
    }

    public async Task<Result<VitalsDto?>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var vitals = await _db.Vitals.AsNoTracking()
            .FirstOrDefaultAsync(v => v.AppointmentId == appointmentId, ct);
        if (vitals is null)
            return Result.Success<VitalsDto?>(null);

        return Result.Success<VitalsDto?>(await ToDtoAsync(vitals, ct));
    }

    /// <summary>
    /// Ánh xạ trong bộ nhớ (không projection SQL) vì <see cref="Domain.Clinical.Vitals.Bmi"/> là thuộc tính
    /// tính toán không map được — EF không dịch sang SQL. Tra tên người đo bằng một truy vấn phụ.
    /// </summary>
    private async Task<VitalsDto> ToDtoAsync(Domain.Clinical.Vitals v, CancellationToken ct)
    {
        var measuredByName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == v.MeasuredBy)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return new VitalsDto(
            v.Id,
            v.AppointmentId,
            v.PatientId,
            v.HeightCm,
            v.WeightKg,
            v.Bmi,
            v.TemperatureC,
            v.Pulse,
            v.BloodPressureSystolic,
            v.BloodPressureDiastolic,
            v.SpO2,
            v.RespiratoryRate,
            v.Notes,
            v.MeasuredAt,
            v.MeasuredBy,
            measuredByName,
            v.CreatedAt,
            v.UpdatedAt);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
