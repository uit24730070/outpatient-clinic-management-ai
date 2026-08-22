namespace ClinicManagement.Domain.Paraclinical;

/// <summary>Trạng thái của một mục chỉ định (một dịch vụ cận lâm sàng). Lưu dạng chuỗi.</summary>
public enum LabOrderItemStatus
{
    /// <summary>Chờ thực hiện/nhập kết quả.</summary>
    Pending,

    /// <summary>Đã có kết quả.</summary>
    Completed
}
