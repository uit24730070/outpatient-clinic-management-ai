using ClinicManagement.Application.Assistant;
using ClinicManagement.Application.Assistant.Dtos;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Trợ lý hội thoại nghiệp vụ (AI-03). Mọi vai trò đã đăng nhập đều dùng được; phạm vi dữ liệu do
/// lớp công cụ kiểm soát theo vai trò/hồ sơ bác sĩ của người gọi.
/// </summary>
[Authorize]
[Route("api/assistant")]
public sealed class AssistantController : ApiControllerBase
{
    private readonly IAssistantService _assistant;
    private readonly IAuthService _auth;

    public AssistantController(IAssistantService assistant, IAuthService auth)
    {
        _assistant = assistant;
        _auth = auth;
    }

    /// <summary>Gửi lịch sử hội thoại, nhận câu trả lời (và danh sách công cụ đã gọi để truy vết).</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken ct)
    {
        // Tra hồ sơ bác sĩ (nếu có) để giới hạn phạm vi "của tôi" khi thực thi công cụ (ADR 0009).
        Guid? doctorId = null;
        var me = await _auth.GetByIdAsync(CurrentUserId, ct);
        if (me.IsSuccess)
            doctorId = me.Value.DoctorId;

        var context = new AssistantContext(CurrentUserId, CurrentUserRole, doctorId);
        var result = await _assistant.ChatAsync(request, context, ct);
        return ToResponse(result);
    }
}
