using ClinicManagement.Application.Assistant.Dtos;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Assistant;

/// <summary>
/// Trợ lý hội thoại nghiệp vụ (AI-03): điều phối vòng lặp tool-calling — gửi hội thoại + danh sách
/// công cụ tới LLM, thực thi công cụ khi được yêu cầu (tôn trọng RBAC theo <see cref="AssistantContext"/>),
/// rồi trả câu trả lời cuối. Chỉ dùng công cụ chỉ-đọc.
/// </summary>
public interface IAssistantService
{
    Task<Result<AssistantReplyDto>> ChatAsync(
        AssistantChatRequest request, AssistantContext context, CancellationToken ct = default);
}
