using ClinicManagement.Application.Doctors.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Doctors.Validators;

public sealed class CreateDoctorScheduleRequestValidator : AbstractValidator<CreateDoctorScheduleRequest>
{
    public CreateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum().WithMessage("Thứ trong tuần không hợp lệ.");
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
    }
}

public sealed class UpdateDoctorScheduleRequestValidator : AbstractValidator<UpdateDoctorScheduleRequest>
{
    public UpdateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum().WithMessage("Thứ trong tuần không hợp lệ.");
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
    }
}
