namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Loại đầu vào cần sinh embedding — nhiều provider (vd Voyage AI) tối ưu vector khác nhau
/// cho <c>document</c> (nội dung lập chỉ mục) và <c>query</c> (câu truy vấn). Trung lập provider.
/// </summary>
public enum EmbeddingInputType
{
    Document,
    Query
}
