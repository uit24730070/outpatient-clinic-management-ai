using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Domain.Appointments;

namespace ClinicManagement.Application.Assistant.Tools;

/// <summary>
/// Công cụ liệt kê lịch khám theo ngày/bác sĩ/trạng thái (đọc). <b>Kiểm soát quyền:</b> nếu người
/// gọi là bác sĩ, chỉ trả lịch của chính họ (ép <c>doctorId</c> = hồ sơ đang đăng nhập).
/// </summary>
public sealed class ListAppointmentsTool : IAssistantTool
{
    private readonly IAppointmentService _appointments;
    public ListAppointmentsTool(IAppointmentService appointments) => _appointments = appointments;

    public AiTool Definition { get; } = new(
        "list_appointments",
        "Liệt kê lịch khám, lọc theo ngày (YYYY-MM-DD), bác sĩ, hoặc trạng thái. " +
        "Nếu người dùng là bác sĩ thì chỉ trả lịch của chính họ.",
        new[]
        {
            new AiToolParameter("date", "string", "Ngày cần xem theo định dạng YYYY-MM-DD. Bỏ trống để không lọc theo ngày."),
            new AiToolParameter("doctorId", "string", "Id bác sĩ (GUID) để lọc. Bỏ qua nếu người dùng là bác sĩ."),
            new AiToolParameter("patientId", "string", "Id bệnh nhân (GUID) để lọc lịch của một bệnh nhân."),
            new AiToolParameter("status", "string", "Trạng thái lịch cần lọc.", Required: false,
                Enum: new[] { "Scheduled", "CheckedIn", "InProgress", "Completed", "Cancelled", "NoShow" }),
            new AiToolParameter("limit", "integer", "Số kết quả tối đa (mặc định 50, tối đa 100).")
        });

    public async Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default)
    {
        var args = ToolJson.Parse(argumentsJson);
        var limit = Math.Clamp(args.GetInt("limit") ?? 50, 1, 100);

        DateOnly? date = DateOnly.TryParse(args.GetString("date"), out var d) ? d : null;

        AppointmentStatus? status =
            Enum.TryParse<AppointmentStatus>(args.GetString("status"), ignoreCase: true, out var s) ? s : null;

        // Kiểm soát quyền: bác sĩ chỉ được xem lịch của chính mình.
        Guid? doctorId = args.GetGuid("doctorId");
        if (context.IsDoctor)
        {
            if (context.DoctorId is null)
                return ToolJson.Error("Tài khoản bác sĩ chưa gắn hồ sơ nên không có lịch khám nào.");
            doctorId = context.DoctorId;
        }

        var filter = new AppointmentFilter(
            Page: 1, PageSize: limit, Date: date, DoctorId: doctorId,
            PatientId: args.GetGuid("patientId"), Status: status);

        var result = await _appointments.GetListAsync(filter, ct);
        if (result.IsFailure)
            return ToolJson.Error(result.Error.Message);

        var items = result.Value.Items.Select(a => new
        {
            id = a.Id,
            patientId = a.PatientId,
            patientName = a.PatientName,
            doctorId = a.DoctorId,
            doctorName = a.DoctorName,
            startTime = a.StartTime,
            endTime = a.EndTime,
            status = a.Status.ToString(),
            reason = a.Reason
        });

        return ToolJson.Serialize(new { total = result.Value.TotalCount, items });
    }
}
