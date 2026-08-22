namespace ClinicManagement.Domain.Paraclinical;

/// <summary>
/// Vòng đời phiếu chỉ định cận lâm sàng. Lưu dạng chuỗi. Chuyển tự động theo tiến độ nhập kết quả
/// các mục: nhập kết quả mục đầu → <see cref="InProgress"/>; đủ mọi mục có kết quả → <see cref="Completed"/>.
/// </summary>
public enum LabOrderStatus
{
    /// <summary>Vừa chỉ định, chưa có kết quả nào.</summary>
    Ordered,

    /// <summary>Đã có ít nhất một mục kết quả nhưng chưa đủ.</summary>
    InProgress,

    /// <summary>Mọi mục đã có kết quả.</summary>
    Completed,

    /// <summary>Đã huỷ phiếu chỉ định.</summary>
    Cancelled
}
