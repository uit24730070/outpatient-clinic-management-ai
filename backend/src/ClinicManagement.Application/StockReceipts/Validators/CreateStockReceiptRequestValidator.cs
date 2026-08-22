using ClinicManagement.Application.StockReceipts.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.StockReceipts.Validators;

public sealed class CreateStockReceiptRequestValidator : AbstractValidator<CreateStockReceiptRequest>
{
    public CreateStockReceiptRequestValidator()
    {
        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Nhà cung cấp không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Phiếu nhập phải có ít nhất một dòng.");

        RuleForEach(x => x.Items).SetValidator(new StockReceiptItemRequestValidator());
    }
}

public sealed class StockReceiptItemRequestValidator : AbstractValidator<StockReceiptItemRequest>
{
    public StockReceiptItemRequestValidator()
    {
        RuleFor(x => x.MedicationId)
            .NotEmpty().WithMessage("Thuốc không được để trống.");

        RuleFor(x => x.BatchNumber)
            .NotEmpty().WithMessage("Số lô không được để trống.")
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng nhập phải lớn hơn 0.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.")
            .When(x => x.UnitCost.HasValue);
    }
}
