using ClinicManagement.Application.Encounters.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Encounters.Validators;

public sealed class PrescriptionItemRequestValidator : AbstractValidator<PrescriptionItemRequest>
{
    public PrescriptionItemRequestValidator()
    {
        RuleFor(x => x.DrugName)
            .NotEmpty().WithMessage("Tên thuốc không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Liều dùng không được để trống.")
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

        RuleFor(x => x.Instruction)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Instruction));
    }
}
