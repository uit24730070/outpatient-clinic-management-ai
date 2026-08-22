using ClinicManagement.Application.Paraclinical.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Paraclinical.Validators;

public sealed class CreateLabOrderRequestValidator : AbstractValidator<CreateLabOrderRequest>
{
    public CreateLabOrderRequestValidator()
    {
        RuleFor(x => x.EncounterId)
            .NotEmpty().WithMessage("Phải gắn phiếu khám.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Phiếu chỉ định phải có ít nhất một dịch vụ.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServicePriceId)
                .NotEmpty().WithMessage("Phải chọn dịch vụ cận lâm sàng.");
        });

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
