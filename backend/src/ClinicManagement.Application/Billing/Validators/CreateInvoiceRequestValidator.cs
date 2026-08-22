using ClinicManagement.Application.Billing.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Billing.Validators;

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Phải chọn bệnh nhân.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Hoá đơn phải có ít nhất một dòng.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServicePriceId)
                .NotEmpty().WithMessage("Phải chọn dịch vụ.");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
