using ClinicManagement.Application.Users.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Users.Validators;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
