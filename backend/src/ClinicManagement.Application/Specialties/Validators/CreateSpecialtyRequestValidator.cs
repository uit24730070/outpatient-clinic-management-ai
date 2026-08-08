using ClinicManagement.Application.Specialties.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Specialties.Validators;

public sealed class CreateSpecialtyRequestValidator : AbstractValidator<CreateSpecialtyRequest>
{
    public CreateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chuyên khoa không được để trống.")
            .MaximumLength(150);

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
