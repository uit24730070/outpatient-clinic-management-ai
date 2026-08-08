using ClinicManagement.Application.Encounters.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Encounters.Validators;

public sealed class CreateEncounterRequestValidator : AbstractValidator<CreateEncounterRequest>
{
    public CreateEncounterRequestValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Lịch khám không được để trống.");

        RuleFor(x => x.Diagnosis)
            .NotEmpty().WithMessage("Chẩn đoán không được để trống.")
            .MaximumLength(1000);

        RuleFor(x => x.Symptoms)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Symptoms));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleForEach(x => x.PrescriptionItems)
            .SetValidator(new PrescriptionItemRequestValidator());
    }
}
