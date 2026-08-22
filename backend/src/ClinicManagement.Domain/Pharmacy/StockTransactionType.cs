namespace ClinicManagement.Domain.Pharmacy;

/// <summary>Loại giao dịch tồn kho trong sổ cái (<see cref="StockTransaction"/>).</summary>
public enum StockTransactionType
{
    /// <summary>Nhập kho (tăng tồn) — P1.</summary>
    Import,

    /// <summary>Cấp phát theo đơn (giảm tồn) — dành cho P2.</summary>
    Dispense,

    /// <summary>Điều chỉnh thủ công/kiểm kê — dành cho P2.</summary>
    Adjust
}
