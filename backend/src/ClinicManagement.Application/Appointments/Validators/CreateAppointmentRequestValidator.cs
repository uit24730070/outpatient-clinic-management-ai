using ClinicManagement.Application.Appointments.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Appointments.Validators;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Bệnh nhân không được để trống.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Bác sĩ không được để trống.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Thời điểm bắt đầu không được để trống.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
