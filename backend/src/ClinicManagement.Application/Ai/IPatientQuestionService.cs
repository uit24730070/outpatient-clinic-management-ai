using ClinicManagement.Application.Ai.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Ai;

/// <summary>Hỏi đáp có ngữ cảnh (RAG) trên lịch sử bệnh án của một bệnh nhân (AI-05).</summary>
public interface IPatientQuestionService
{
    Task<Result<PatientAnswerDto>> AnswerAsync(Guid patientId, string question, CancellationToken ct = default);
}
