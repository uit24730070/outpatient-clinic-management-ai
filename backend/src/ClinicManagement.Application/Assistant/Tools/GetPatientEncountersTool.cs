using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Encounters;

namespace ClinicManagement.Application.Assistant.Tools;

/// <summary>
/// Công cụ lấy lịch sử phiếu khám của một bệnh nhân (đọc). <b>Kiểm soát quyền:</b> nếu người gọi là
/// bác sĩ, chỉ trả các phiếu do chính họ lập (ép <c>doctorId</c> = hồ sơ đang đăng nhập).
/// </summary>
public sealed class GetPatientEncountersTool : IAssistantTool
{
    private readonly IEncounterService _encounters;
    public GetPatientEncountersTool(IEncounterService encounters) => _encounters = encounters;

    public AiTool Definition { get; } = new(
        "get_patient_encounters",
        "Lấy lịch sử phiếu khám (triệu chứng, chẩn đoán, đơn thuốc) của một bệnh nhân theo Id. " +
        "Nếu người dùng là bác sĩ thì chỉ trả các phiếu do chính họ lập.",
        new[]
        {
            new AiToolParameter("patientId", "string", "Id bệnh nhân (GUID) cần xem lịch sử khám.", Required: true),
            new AiToolParameter("limit", "integer", "Số phiếu gần nhất tối đa (mặc định 10, tối đa 20).")
        });

    public async Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default)
    {
        var args = ToolJson.Parse(argumentsJson);
        var patientId = args.GetGuid("patientId");
        if (patientId is null)
            return ToolJson.Error("Thiếu patientId hợp lệ (GUID).");

        var limit = Math.Clamp(args.GetInt("limit") ?? 10, 1, 20);

        // Kiểm soát quyền: bác sĩ chỉ xem phiếu của chính mình.
        Guid? doctorId = null;
        if (context.IsDoctor)
        {
            if (context.DoctorId is null)
                return ToolJson.Error("Tài khoản bác sĩ chưa gắn hồ sơ nên không có phiếu khám nào.");
            doctorId = context.DoctorId;
        }

        var filter = new EncounterFilter(Page: 1, PageSize: limit, PatientId: patientId, DoctorId: doctorId);
        var result = await _encounters.GetListAsync(filter, ct);
        if (result.IsFailure)
            return ToolJson.Error(result.Error.Message);

        var items = result.Value.Items.Select(e => new
        {
            id = e.Id,
            createdAt = e.CreatedAt,
            doctorName = e.DoctorName,
            symptoms = e.Symptoms,
            diagnosis = e.Diagnosis,
            notes = e.Notes,
            status = e.Status.ToString(),
            prescriptions = e.PrescriptionItems.Select(i => new
            {
                drug = i.DrugName,
                dosage = i.Dosage,
                quantity = i.Quantity,
                instruction = i.Instruction
            })
        });

        return ToolJson.Serialize(new { total = result.Value.TotalCount, items });
    }
}
