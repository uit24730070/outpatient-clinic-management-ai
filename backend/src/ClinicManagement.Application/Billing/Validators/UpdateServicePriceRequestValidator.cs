using ClinicManagement.Application.Billing.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Billing.Validators;

public sealed class UpdateServicePriceRequestValidator : AbstractValidator<UpdateServicePriceRequest>
{
    public UpdateServicePriceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên dịch vụ không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
