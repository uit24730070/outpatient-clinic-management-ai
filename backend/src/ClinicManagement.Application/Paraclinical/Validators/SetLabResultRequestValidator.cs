using ClinicManagement.Application.Paraclinical.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Paraclinical.Validators;

public sealed class SetLabResultRequestValidator : AbstractValidator<SetLabResultRequest>
{
    public SetLabResultRequestValidator()
    {
        RuleFor(x => x.ResultText).MaximumLength(4000);
        RuleFor(x => x.Conclusion).MaximumLength(1000);

        RuleForEach(x => x.Parameters).ChildRules(p =>
        {
            p.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên thông số không được để trống.")
                .MaximumLength(100);
            p.RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Giá trị không được để trống.")
                .MaximumLength(200);
            p.RuleFor(x => x.Unit).MaximumLength(50);
            p.RuleFor(x => x.ReferenceRange).MaximumLength(100);
        });
    }
}
