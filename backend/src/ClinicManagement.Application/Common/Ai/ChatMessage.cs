namespace ClinicManagement.Application.Common.Ai;

/// <summary>Một lượt hội thoại (vai trò + nội dung văn bản) gửi tới LLM.</summary>
public sealed record ChatMessage(ChatRole Role, string Content)
{
    public static ChatMessage User(string content) => new(ChatRole.User, content);
    public static ChatMessage Assistant(string content) => new(ChatRole.Assistant, content);
}
