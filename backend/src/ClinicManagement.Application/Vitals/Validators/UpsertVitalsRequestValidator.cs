using ClinicManagement.Application.Vitals.Dtos;
using FluentValidation;

namespace ClinicManagement.Application.Vitals.Validators;

/// <summary>
/// Kiểm tra biên hợp lý các chỉ số sinh hiệu (chỉ khi có giá trị — mọi trường tuỳ chọn). Biên rộng,
/// mục đích chặn nhập sai rõ ràng (âm/quá lớn), không thay chẩn đoán lâm sàng.
/// </summary>
public sealed class UpsertVitalsRequestValidator : AbstractValidator<UpsertVitalsRequest>
{
    public UpsertVitalsRequestValidator()
    {
        RuleFor(x => x.HeightCm)
            .InclusiveBetween(20m, 300m).WithMessage("Chiều cao (cm) không hợp lệ.")
            .When(x => x.HeightCm.HasValue);

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(0.5m, 500m).WithMessage("Cân nặng (kg) không hợp lệ.")
            .When(x => x.WeightKg.HasValue);

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(25m, 45m).WithMessage("Nhiệt độ (°C) không hợp lệ.")
            .When(x => x.TemperatureC.HasValue);

        RuleFor(x => x.Pulse)
            .InclusiveBetween(20, 300).WithMessage("Mạch (lần/phút) không hợp lệ.")
            .When(x => x.Pulse.HasValue);

        RuleFor(x => x.BloodPressureSystolic)
            .InclusiveBetween(40, 300).WithMessage("Huyết áp tâm thu (mmHg) không hợp lệ.")
            .When(x => x.BloodPressureSystolic.HasValue);

        RuleFor(x => x.BloodPressureDiastolic)
            .InclusiveBetween(20, 200).WithMessage("Huyết áp tâm trương (mmHg) không hợp lệ.")
            .When(x => x.BloodPressureDiastolic.HasValue);

        RuleFor(x => x.SpO2)
            .InclusiveBetween(50, 100).WithMessage("SpO2 (%) không hợp lệ.")
            .When(x => x.SpO2.HasValue);

        RuleFor(x => x.RespiratoryRate)
            .InclusiveBetween(4, 80).WithMessage("Nhịp thở (lần/phút) không hợp lệ.")
            .When(x => x.RespiratoryRate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
