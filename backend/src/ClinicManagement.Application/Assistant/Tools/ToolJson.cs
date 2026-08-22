using System.Text.Json;

namespace ClinicManagement.Application.Assistant.Tools;

/// <summary>
/// Tiện ích cho công cụ trợ lý: phân giải tham số JSON đầu vào và tuần tự hoá kết quả gọn cho LLM.
/// </summary>
public static class ToolJson
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Tuần tự hoá kết quả công cụ thành JSON gọn (không escape tiếng Việt).</summary>
    public static string Serialize(object value) => JsonSerializer.Serialize(value, SerializeOptions);

    /// <summary>Kết quả lỗi chuẩn hoá để LLM hiểu công cụ không trả được dữ liệu.</summary>
    public static string Error(string message) => Serialize(new { error = message });

    /// <summary>Phân giải tham số JSON; trả về đối tượng rỗng nếu chuỗi trống/không hợp lệ.</summary>
    public static ToolArguments Parse(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
            return new ToolArguments(default);
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            return new ToolArguments(doc.RootElement.Clone());
        }
        catch (JsonException)
        {
            return new ToolArguments(default);
        }
    }
}

/// <summary>Bao gói tham số JSON của công cụ với các bộ đọc an toàn kiểu.</summary>
public readonly struct ToolArguments
{
    private readonly JsonElement _root;
    public ToolArguments(JsonElement root) => _root = root;

    public string? GetString(string name)
    {
        if (_root.ValueKind != JsonValueKind.Object || !_root.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            _ => null
        };
    }

    public int? GetInt(string name)
    {
        if (_root.ValueKind != JsonValueKind.Object || !_root.TryGetProperty(name, out var el))
            return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
        return null;
    }

    public Guid? GetGuid(string name)
    {
        var s = GetString(name);
        return Guid.TryParse(s, out var g) ? g : null;
    }
}
