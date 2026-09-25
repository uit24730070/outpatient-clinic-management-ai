using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Doctors;

namespace ClinicManagement.Application.Assistant.Tools;

/// <summary>Công cụ liệt kê/tra cứu bác sĩ theo tên/mã/chuyên khoa (đọc — mọi vai trò).</summary>
public sealed class ListDoctorsTool : IAssistantTool
{
    private readonly IDoctorService _doctors;
    public ListDoctorsTool(IDoctorService doctors) => _doctors = doctors;

    public AiTool Definition { get; } = new(
        "list_doctors",
        "Liệt kê hoặc tìm bác sĩ theo tên, mã, hoặc tên chuyên khoa. Dùng để tra cứu Id bác sĩ.",
        new[]
        {
            new AiToolParameter("query", "string", "Từ khoá tìm kiếm (tên bác sĩ, mã, hoặc chuyên khoa). Bỏ trống để liệt kê."),
            new AiToolParameter("limit", "integer", "Số kết quả tối đa (mặc định 20, tối đa 50).")
        });

    public async Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default)
    {
        var args = ToolJson.Parse(argumentsJson);
        var query = args.GetString("query");
        var limit = Math.Clamp(args.GetInt("limit") ?? 20, 1, 50);

        var result = await _doctors.GetListAsync(1, limit, query, ct: ct);
        if (result.IsFailure)
            return ToolJson.Error(result.Error.Message);

        var items = result.Value.Items.Select(d => new
        {
            id = d.Id,
            code = d.Code,
            fullName = d.FullName,
            specialty = d.SpecialtyName,
            phoneNumber = d.PhoneNumber
        });

        return ToolJson.Serialize(new { total = result.Value.TotalCount, items });
    }
}
