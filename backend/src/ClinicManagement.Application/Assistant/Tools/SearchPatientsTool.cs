using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Patients;

namespace ClinicManagement.Application.Assistant.Tools;

/// <summary>Công cụ tìm bệnh nhân theo tên/mã/số điện thoại (đọc — mọi vai trò).</summary>
public sealed class SearchPatientsTool : IAssistantTool
{
    private readonly IPatientService _patients;
    public SearchPatientsTool(IPatientService patients) => _patients = patients;

    public AiTool Definition { get; } = new(
        "search_patients",
        "Tìm bệnh nhân theo tên, mã bệnh nhân hoặc số điện thoại. Trả danh sách gọn để tra cứu Id.",
        new[]
        {
            new AiToolParameter("query", "string", "Từ khoá tìm kiếm: tên, mã BN hoặc số điện thoại.", Required: true),
            new AiToolParameter("limit", "integer", "Số kết quả tối đa (mặc định 10, tối đa 20).")
        });

    public async Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default)
    {
        var args = ToolJson.Parse(argumentsJson);
        var query = args.GetString("query");
        var limit = Math.Clamp(args.GetInt("limit") ?? 10, 1, 20);

        var result = await _patients.GetListAsync(1, limit, query, ct);
        if (result.IsFailure)
            return ToolJson.Error(result.Error.Message);

        var items = result.Value.Items.Select(p => new
        {
            id = p.Id,
            code = p.Code,
            fullName = p.FullName,
            gender = p.Gender.ToString(),
            dateOfBirth = p.DateOfBirth?.ToString("yyyy-MM-dd"),
            phoneNumber = p.PhoneNumber
        });

        return ToolJson.Serialize(new { total = result.Value.TotalCount, items });
    }
}
