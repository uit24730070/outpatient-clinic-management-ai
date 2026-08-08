namespace ClinicManagement.Application.Ai.Dtos;

/// <summary>Câu hỏi tự do của bác sĩ để hỏi đáp có ngữ cảnh trên bệnh án của bệnh nhân.</summary>
public sealed record AskPatientRequest(string Question);
