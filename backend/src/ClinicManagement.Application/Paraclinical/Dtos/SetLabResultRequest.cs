namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>
/// Dữ liệu nhập kết quả cho một mục chỉ định cận lâm sàng. <see cref="Parameters"/> (ADR 0025) dùng cho
/// mục nhóm Xét nghiệm — thông số có cấu trúc thay cho <see cref="ResultText"/> văn bản tự do; hai cách
/// không loại trừ nhau, để trống/null thì giữ như trước (không thông số).
/// </summary>
public sealed record SetLabResultRequest(
    string? ResultText,
    string? Conclusion,
    IReadOnlyList<ResultParameterInput>? Parameters = null);
