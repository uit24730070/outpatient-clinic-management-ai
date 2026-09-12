using ClinicManagement.Application.Billing.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Billing.Validators;

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Phải chọn bệnh nhân.");

        // Cho phép Items rỗng khi gộp phí CLS từ LabOrderId (dòng CLS tự nạp ở service) — vẫn cần
        // ít nhất một dòng khi không gộp.
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Hoá đơn phải có ít nhất một dòng.")
            .When(x => x.LabOrderId is null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServicePriceId)
                .NotEmpty().WithMessage("Phải chọn dịch vụ.");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });

        RuleForEach(x => x.AppointmentIds)
            .NotEmpty().WithMessage("Id dịch vụ khám không hợp lệ.");

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
