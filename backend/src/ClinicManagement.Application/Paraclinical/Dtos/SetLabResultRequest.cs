namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>Dữ liệu nhập kết quả cho một mục chỉ định cận lâm sàng.</summary>
public sealed record SetLabResultRequest(
    string? ResultText,
    string? Conclusion);
