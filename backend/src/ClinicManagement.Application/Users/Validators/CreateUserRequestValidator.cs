using ClinicManagement.Application.Users.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .MinimumLength(3).WithMessage("Tên đăng nhập tối thiểu 3 ký tự.")
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Tên đăng nhập chỉ gồm chữ, số và các ký tự . _ -");

        RuleFor(x => x.Password).ValidPassword();

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
