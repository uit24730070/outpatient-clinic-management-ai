using FluentValidation;

namespace ClinicManagement.Application.Users.Validators;

/// <summary>Quy tắc độ mạnh mật khẩu dùng chung cho tạo tài khoản và đặt lại mật khẩu.</summary>
internal static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự.")
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ cái.")
            .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.");
}
