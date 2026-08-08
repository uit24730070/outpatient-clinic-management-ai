using ClinicManagement.Application.Patients.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Patients.Validators;

public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Gender).IsInEnum();

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(@"^[0-9+\-\s]+$").WithMessage("Số điện thoại không hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Address).MaximumLength(500);

        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày sinh không thể ở tương lai.")
            .When(x => x.DateOfBirth.HasValue);
    }
}
