using System.Globalization;
using System.Text;
using ClinicManagement.Application.Ai.Dtos;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Ai;

public sealed class PatientSummaryService : IPatientSummaryService
{
    /// <summary>Số phiếu khám gần nhất tối đa nhồi vào ngữ cảnh (context stuffing — chưa RAG).</summary>
    private const int MaxEncounters = 10;

    private const string SystemPrompt =
        "Bạn là trợ lý y khoa hỗ trợ bác sĩ tại phòng khám ngoại trú. " +
        "Nhiệm vụ: tóm tắt ngắn gọn, chính xác lịch sử khám của một bệnh nhân dựa CHỈ trên dữ liệu bệnh án được cung cấp. " +
        "Viết bằng tiếng Việt, có cấu trúc rõ ràng: các vấn đề/chẩn đoán nổi bật, diễn tiến theo thời gian, thuốc đã kê đáng chú ý, và điểm cần theo dõi. " +
        "Không bịa thông tin ngoài dữ liệu. Không đưa chẩn đoán mới hay chỉ định điều trị. " +
        "Đây là thông tin tham khảo, không thay thế đánh giá của bác sĩ.";

    private readonly IAppDbContext _db;
    private readonly IChatCompletionService _chat;

    public PatientSummaryService(IAppDbContext db, IChatCompletionService chat)
    {
        _db = db;
        _chat = chat;
    }

    public async Task<Result<PatientSummaryDto>> SummarizeAsync(Guid patientId, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.Id, p.FullName })
            .FirstOrDefaultAsync(ct);

        if (patient is null)
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {patientId}.");

        // Nạp N phiếu khám gần nhất (kèm đơn thuốc) làm ngữ cảnh.
        var encounters = await _db.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(MaxEncounters)
            .Select(e => new EncounterContext(
                e.CreatedAt,
                _db.Doctors.Where(d => d.Id == e.DoctorId).Select(d => d.FullName).FirstOrDefault(),
                e.Symptoms,
                e.Diagnosis,
                e.Notes,
                e.Status.ToString(),
                e.PrescriptionItems
                    .Select(i => new PrescriptionContext(i.DrugName, i.Dosage, i.Quantity, i.Instruction))
                    .ToList()))
            .ToListAsync(ct);

        // Chưa có bệnh án → không gọi LLM (tiết kiệm chi phí), trả thông điệp phù hợp.
        if (encounters.Count == 0)
            return new PatientSummaryDto(
                patient.Id, patient.FullName,
                "Bệnh nhân chưa có phiếu khám nào để tóm tắt.",
                0, "-", DateTimeOffset.UtcNow);

        var prompt = BuildPrompt(patient.FullName, encounters);

        var request = new ChatCompletionRequest(
            Messages: new[] { ChatMessage.User(prompt) },
            System: SystemPrompt);

        var completion = await _chat.CompleteAsync(request, ct);
        if (completion.IsFailure)
            return Result.Failure<PatientSummaryDto>(completion.Error);

        return new PatientSummaryDto(
            patient.Id, patient.FullName,
            completion.Value.Text.Trim(),
            encounters.Count,
            completion.Value.Model,
            DateTimeOffset.UtcNow);
    }

    /// <summary>Dựng prompt tiếng Việt liệt kê các phiếu khám (cũ → mới) để LLM tóm tắt.</summary>
    private static string BuildPrompt(string patientName, IReadOnlyList<EncounterContext> encounters)
    {
        var sb = new StringBuilder();
        sb.Append("Tóm tắt lịch sử khám của bệnh nhân \"").Append(patientName).Append("\". ")
          .Append("Dưới đây là ").Append(encounters.Count)
          .AppendLine(" phiếu khám gần nhất (sắp theo thời gian tăng dần):").AppendLine();

        // Đảo lại cho đúng trình tự thời gian (dữ liệu nạp theo mới → cũ).
        var ordered = encounters.OrderBy(e => e.CreatedAt).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var e = ordered[i];
            sb.Append("### Phiếu ").Append(i + 1).Append(" — ")
              .Append(e.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))
              .Append(" (").Append(e.Status).AppendLine(")");
            sb.Append("- Bác sĩ: ").AppendLine(e.DoctorName ?? "(không rõ)");
            if (!string.IsNullOrWhiteSpace(e.Symptoms))
                sb.Append("- Triệu chứng: ").AppendLine(e.Symptoms);
            sb.Append("- Chẩn đoán: ").AppendLine(e.Diagnosis);
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.Append("- Chỉ định/Ghi chú: ").AppendLine(e.Notes);

            if (e.PrescriptionItems.Count == 0)
            {
                sb.AppendLine("- Đơn thuốc: không kê");
            }
            else
            {
                sb.AppendLine("- Đơn thuốc:");
                foreach (var it in e.PrescriptionItems)
                {
                    sb.Append("  + ").Append(it.DrugName).Append(" — ").Append(it.Dosage)
                      .Append(" × ").Append(it.Quantity.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(it.Instruction))
                        sb.Append(" (").Append(it.Instruction).Append(')');
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private sealed record EncounterContext(
        DateTimeOffset CreatedAt,
        string? DoctorName,
        string? Symptoms,
        string Diagnosis,
        string? Notes,
        string Status,
        IReadOnlyList<PrescriptionContext> PrescriptionItems);

    private sealed record PrescriptionContext(string DrugName, string Dosage, int Quantity, string? Instruction);
}
