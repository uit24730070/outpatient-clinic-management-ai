namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Định nghĩa một công cụ (tool/function) trung lập provider mà LLM có thể yêu cầu gọi:
/// tên, mô tả, và danh sách tham số. Hiện thực ở Infrastructure dịch sang định dạng
/// tool-use của provider (vd <c>tools[].function.parameters</c> JSON Schema của OpenAI).
/// </summary>
public sealed record AiTool(
    string Name,
    string Description,
    IReadOnlyList<AiToolParameter> Parameters);

/// <summary>
/// Một tham số của công cụ. <paramref name="Type"/> theo kiểu JSON Schema
/// (<c>string</c>/<c>integer</c>/<c>number</c>/<c>boolean</c>). <paramref name="Enum"/>
/// (nếu có) giới hạn các giá trị hợp lệ.
/// </summary>
public sealed record AiToolParameter(
    string Name,
    string Type,
    string Description,
    bool Required = false,
    IReadOnlyList<string>? Enum = null);
