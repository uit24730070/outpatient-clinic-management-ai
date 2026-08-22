using ClinicManagement.Application.Medications.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Medications.Validators;

public sealed class CreateMedicationRequestValidator : AbstractValidator<CreateMedicationRequest>
{
    public CreateMedicationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên thuốc không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.ActiveIngredient)
            .NotEmpty().WithMessage("Hoạt chất không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Đơn vị tính không được để trống.")
            .MaximumLength(50);

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Ngưỡng tồn tối thiểu không được âm.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
