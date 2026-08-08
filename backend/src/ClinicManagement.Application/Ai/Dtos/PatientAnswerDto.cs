namespace ClinicManagement.Application.Ai.Dtos;

/// <summary>Phiếu khám được trích làm ngữ cảnh cho câu trả lời (traceability).</summary>
public sealed record AnswerSourceDto(
    Guid EncounterId,
    DateTimeOffset CreatedAt,
    string Diagnosis,
    double Similarity);

/// <summary>
/// Kết quả hỏi đáp có ngữ cảnh (RAG) trên bệnh án của một bệnh nhân.
/// <paramref name="Sources"/> là các phiếu khám được truy hồi và dùng để trả lời.
/// </summary>
public sealed record PatientAnswerDto(
    Guid PatientId,
    string PatientName,
    string Question,
    string Answer,
    string Model,
    IReadOnlyList<AnswerSourceDto> Sources,
    DateTimeOffset GeneratedAt);
