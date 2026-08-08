using System.Globalization;
using System.Text;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Encounters;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Ai;

/// <summary>Ảnh chụp nội dung một phiếu khám để sinh embedding và dựng ngữ cảnh prompt.</summary>
internal sealed record EncounterContent(
    Guid Id,
    Guid PatientId,
    DateTimeOffset CreatedAt,
    string? DoctorName,
    string? Symptoms,
    string Diagnosis,
    string? Notes,
    string Status,
    IReadOnlyList<PrescriptionLine> Items);

internal sealed record PrescriptionLine(string DrugName, string Dosage, int Quantity, string? Instruction);

/// <summary>
/// Chuyển phiếu khám sang văn bản: một dạng gọn để sinh embedding (AI-04) và một dạng khối
/// có cấu trúc để nhồi vào prompt truy hồi (AI-05). Tái dùng giữa indexer và service hỏi đáp.
/// </summary>
internal static class EncounterNarrative
{
    /// <summary>Projection EF chung: phiếu khám → <see cref="EncounterContent"/> kèm tên bác sĩ và đơn thuốc.</summary>
    public static IQueryable<EncounterContent> Project(IAppDbContext db, IQueryable<Encounter> query) =>
        query.Select(e => new EncounterContent(
            e.Id,
            e.PatientId,
            e.CreatedAt,
            db.Doctors.Where(d => d.Id == e.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            e.Symptoms,
            e.Diagnosis,
            e.Notes,
            e.Status.ToString(),
            e.PrescriptionItems
                .Select(i => new PrescriptionLine(i.DrugName, i.Dosage, i.Quantity, i.Instruction))
                .ToList()));

    /// <summary>Văn bản gọn của một phiếu khám để sinh embedding.</summary>
    public static string ToEmbeddingText(EncounterContent e)
    {
        var sb = new StringBuilder();
        sb.Append("Ngày khám ")
          .Append(e.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append(". ");
        if (!string.IsNullOrWhiteSpace(e.DoctorName))
            sb.Append("Bác sĩ ").Append(e.DoctorName).Append(". ");
        if (!string.IsNullOrWhiteSpace(e.Symptoms))
            sb.Append("Triệu chứng: ").Append(e.Symptoms).Append(". ");
        sb.Append("Chẩn đoán: ").Append(e.Diagnosis).Append(". ");
        if (!string.IsNullOrWhiteSpace(e.Notes))
            sb.Append("Chỉ định/Ghi chú: ").Append(e.Notes).Append(". ");
        if (e.Items.Count > 0)
        {
            sb.Append("Đơn thuốc: ");
            sb.Append(string.Join("; ", e.Items.Select(FormatItem)));
            sb.Append('.');
        }
        return sb.ToString();
    }

    /// <summary>Một khối phiếu khám có cấu trúc để nhồi vào prompt (kèm số thứ tự nguồn).</summary>
    public static void AppendPromptBlock(StringBuilder sb, int sourceNo, EncounterContent e)
    {
        sb.Append("### Nguồn ").Append(sourceNo).Append(" — phiếu ngày ")
          .Append(e.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))
          .Append(" (").Append(e.Status).AppendLine(")");
        sb.Append("- Bác sĩ: ").AppendLine(e.DoctorName ?? "(không rõ)");
        if (!string.IsNullOrWhiteSpace(e.Symptoms))
            sb.Append("- Triệu chứng: ").AppendLine(e.Symptoms);
        sb.Append("- Chẩn đoán: ").AppendLine(e.Diagnosis);
        if (!string.IsNullOrWhiteSpace(e.Notes))
            sb.Append("- Chỉ định/Ghi chú: ").AppendLine(e.Notes);
        if (e.Items.Count == 0)
            sb.AppendLine("- Đơn thuốc: không kê");
        else
        {
            sb.AppendLine("- Đơn thuốc:");
            foreach (var it in e.Items)
                sb.Append("  + ").AppendLine(FormatItem(it));
        }
        sb.AppendLine();
    }

    private static string FormatItem(PrescriptionLine it)
    {
        var text = $"{it.DrugName} — {it.Dosage} × {it.Quantity.ToString(CultureInfo.InvariantCulture)}";
        return string.IsNullOrWhiteSpace(it.Instruction) ? text : $"{text} ({it.Instruction})";
    }
}
