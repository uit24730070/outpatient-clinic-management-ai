using System.Text;
using ClinicManagement.Application.Ai.Dtos;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Ai;

/// <summary>
/// Hỏi đáp có ngữ cảnh (RAG): embed câu hỏi → truy hồi top-K phiếu khám gần nhất theo cosine →
/// dựng prompt kèm nguồn → gọi LLM. Thay cho việc nhồi cứng N phiếu ở Sprint 6.
/// </summary>
public sealed class PatientQuestionService : IPatientQuestionService
{
    /// <summary>Số phiếu khám gần nhất truy hồi làm ngữ cảnh.</summary>
    private const int TopK = 5;

    private const string SystemPrompt =
        "Bạn là trợ lý y khoa hỗ trợ bác sĩ tại phòng khám ngoại trú. " +
        "Trả lời câu hỏi của bác sĩ dựa CHỈ trên các phiếu khám được cung cấp bên dưới. " +
        "Viết bằng tiếng Việt, ngắn gọn, dẫn nguồn theo số 'Nguồn N' khi cần. " +
        "Nếu ngữ cảnh không đủ để trả lời, hãy nói rõ là chưa có đủ thông tin. " +
        "Không bịa thông tin ngoài dữ liệu, không đưa chẩn đoán mới hay chỉ định điều trị. " +
        "Đây là thông tin tham khảo, không thay thế đánh giá của bác sĩ.";

    private readonly IAppDbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly IEncounterEmbeddingStore _store;
    private readonly IChatCompletionService _chat;

    public PatientQuestionService(
        IAppDbContext db,
        IEmbeddingService embeddings,
        IEncounterEmbeddingStore store,
        IChatCompletionService chat)
    {
        _db = db;
        _embeddings = embeddings;
        _store = store;
        _chat = chat;
    }

    public async Task<Result<PatientAnswerDto>> AnswerAsync(
        Guid patientId, string question, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.Id, p.FullName })
            .FirstOrDefaultAsync(ct);
        if (patient is null)
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {patientId}.");

        var q = question?.Trim() ?? string.Empty;
        if (q.Length == 0)
            return Error.Validation("Ai.QuestionRequired", "Vui lòng nhập câu hỏi.");

        // 1) Embed câu hỏi (input_type = query).
        var embed = await _embeddings.EmbedAsync(
            new EmbeddingRequest(new[] { q }, EmbeddingInputType.Query), ct);
        if (embed.IsFailure)
            return Result.Failure<PatientAnswerDto>(embed.Error);

        // 2) Truy hồi top-K phiếu gần nhất.
        var matches = await _store.SearchAsync(patientId, embed.Value.Vectors[0], TopK, ct);
        if (matches.Count == 0)
            return NoContext(patient.Id, patient.FullName, q);

        // 3) Nạp nội dung các phiếu được truy hồi (query filter tự loại phiếu đã xoá mềm),
        //    giữ đúng thứ tự & độ tương đồng của kết quả truy hồi.
        var ids = matches.Select(m => m.EncounterId).ToList();
        var contents = await EncounterNarrative
            .Project(_db, _db.Encounters.AsNoTracking().Where(e => ids.Contains(e.Id)))
            .ToListAsync(ct);
        var byId = contents.ToDictionary(c => c.Id);

        var ordered = matches
            .Where(m => byId.ContainsKey(m.EncounterId))
            .Select(m => (Match: m, Content: byId[m.EncounterId]))
            .ToList();
        if (ordered.Count == 0)
            return NoContext(patient.Id, patient.FullName, q);

        // 4) Dựng prompt kèm nguồn và gọi LLM.
        var prompt = BuildPrompt(patient.FullName, q, ordered);
        var completion = await _chat.CompleteAsync(
            new ChatCompletionRequest(new[] { ChatMessage.User(prompt) }, System: SystemPrompt), ct);
        if (completion.IsFailure)
            return Result.Failure<PatientAnswerDto>(completion.Error);

        var sources = ordered
            .Select(o => new AnswerSourceDto(
                o.Content.Id, o.Content.CreatedAt, o.Content.Diagnosis, Math.Round(o.Match.Similarity, 4)))
            .ToList();

        return new PatientAnswerDto(
            patient.Id, patient.FullName, q,
            completion.Value.Text.Trim(), completion.Value.Model, sources, DateTimeOffset.UtcNow);
    }

    private static Result<PatientAnswerDto> NoContext(Guid patientId, string name, string question) =>
        new PatientAnswerDto(
            patientId, name, question,
            "Chưa có dữ liệu bệnh án đã lập chỉ mục để trả lời câu hỏi này.",
            "-", Array.Empty<AnswerSourceDto>(), DateTimeOffset.UtcNow);

    private static string BuildPrompt(
        string patientName, string question,
        IReadOnlyList<(EncounterMatch Match, EncounterContent Content)> ordered)
    {
        var sb = new StringBuilder();
        sb.Append("Bệnh nhân: \"").Append(patientName).AppendLine("\".");
        sb.Append("Câu hỏi của bác sĩ: ").AppendLine(question).AppendLine();
        sb.AppendLine("Dưới đây là các phiếu khám liên quan nhất được truy hồi (dùng để trả lời):").AppendLine();

        for (var i = 0; i < ordered.Count; i++)
            EncounterNarrative.AppendPromptBlock(sb, i + 1, ordered[i].Content);

        sb.AppendLine("Hãy trả lời câu hỏi dựa trên các nguồn trên.");
        return sb.ToString();
    }
}
