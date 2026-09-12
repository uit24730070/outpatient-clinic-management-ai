namespace ClinicManagement.Domain.Encounters;

/// <summary>
/// Trạng thái cấp phát thuốc của phiếu khám (ADR 0021, PAY-02) — lưu dạng chuỗi. Tách "giữ tồn" khỏi
/// "xuất kho thực": chốt phiếu chỉ giữ tồn khả dụng, thu tiền rồi mới xuất kho FEFO.
/// Vòng đời: <see cref="None"/> hoặc <see cref="Reserved"/> → <see cref="Paid"/> → <see cref="Dispensed"/>.
/// </summary>
public enum DispenseStatus
{
    /// <summary>Không có dòng thuốc gắn danh mục để cấp phát (không đi qua vòng đời kho).</summary>
    None,

    /// <summary>Đã chốt phiếu, giữ tồn khả dụng — chưa trừ tồn vật lý, chờ thu tiền.</summary>
    Reserved,

    /// <summary>Đã thu tiền hoá đơn thuốc — chờ Dược sĩ cấp phát thực.</summary>
    Paid,

    /// <summary>Đã cấp phát thực: trừ tồn kho theo FEFO + ghi sổ cái xuất.</summary>
    Dispensed
}
