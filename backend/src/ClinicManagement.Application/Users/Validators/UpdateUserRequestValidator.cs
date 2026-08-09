using ClinicManagement.Application.Users.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Users.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Role).IsInEnum().WithMessage("Vai trò không hợp lệ.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
