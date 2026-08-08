using ClinicManagement.Application.Ai.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Ai;

/// <summary>Sinh bản tóm tắt lịch sử khám của một bệnh nhân từ dữ liệu bệnh án (Sprint 5).</summary>
public interface IPatientSummaryService
{
    Task<Result<PatientSummaryDto>> SummarizeAsync(Guid patientId, CancellationToken ct = default);
}
