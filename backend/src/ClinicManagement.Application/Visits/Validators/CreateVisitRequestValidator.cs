using ClinicManagement.Application.Visits.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Visits.Validators;

public sealed class VisitServiceLineValidator : AbstractValidator<VisitServiceLine>
{
    public VisitServiceLineValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty()
            .GreaterThan(x => x.StartTime).WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class CreateVisitRequestValidator : AbstractValidator<CreateVisitRequest>
{
    public CreateVisitRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.Services)
            .NotEmpty().WithMessage("Lượt tiếp đón phải có ít nhất một dịch vụ khám.");
        RuleForEach(x => x.Services).SetValidator(new VisitServiceLineValidator());
    }
}

public sealed class AddVisitServiceRequestValidator : AbstractValidator<AddVisitServiceRequest>
{
    public AddVisitServiceRequestValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty()
            .GreaterThan(x => x.StartTime).WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
