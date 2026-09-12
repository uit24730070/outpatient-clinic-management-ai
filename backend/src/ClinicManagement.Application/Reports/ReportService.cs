using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Reports.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Reports;

/// <summary>
/// Hiện thực báo cáo vận hành. Tổng hợp trực tiếp từ bảng hiện có; không tạo entity/bảng mới.
/// Múi giờ phòng khám cố định UTC+7 (thống nhất Sprint 14–19) để quy đổi thời điểm UTC sang "ngày".
/// </summary>
public sealed class ReportService : IReportService
{
    /// <summary>Múi giờ phòng khám (UTC+7) — dùng để quy đổi timestamptz UTC sang ngày địa phương khi gom nhóm.</summary>
    private static readonly TimeSpan ClinicOffset = TimeSpan.FromHours(7);

    /// <summary>Số ngày mặc định của báo cáo theo khoảng khi client không truyền from/to (7 ngày gần nhất).</summary>
    private const int DefaultRangeDays = 7;

    /// <summary>Chặn khoảng ngày quá rộng (an toàn hiệu năng cho đồ án).</summary>
    private const int MaxRangeDays = 366;

    private readonly IAppDbContext _db;

    public ReportService(IAppDbContext db) => _db = db;

    public async Task<Result<OverviewDto>> GetOverviewAsync(CancellationToken ct = default)
    {
        var today = TodayLocal();
        var dayStart = DayStart(today);
        var dayEnd = dayStart.AddDays(1);

        var totalPatients = await _db.Patients.AsNoTracking().CountAsync(ct);
        var totalDoctors = await _db.Doctors.AsNoTracking().CountAsync(ct);

        var encountersToday = await _db.Encounters.AsNoTracking()
            .CountAsync(e => e.CreatedAt >= dayStart && e.CreatedAt < dayEnd, ct);

        var revenueToday = await _db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= dayStart && i.PaidAt < dayEnd)
            .SumAsync(i => i.TotalAmount, ct);

        var queueWaiting = await _db.QueueTickets.AsNoTracking()
            .CountAsync(t => t.TicketDate == today && t.Status == Domain.Queue.QueueTicketStatus.Waiting, ct);

        var statuses = await _db.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd)
            .Select(a => a.Status)
            .ToListAsync(ct);

        var breakdown = Breakdown(statuses);

        var dto = new OverviewDto(
            totalPatients,
            totalDoctors,
            encountersToday,
            revenueToday,
            queueWaiting,
            statuses.Count,
            breakdown);

        return Result.Success(dto);
    }

    public async Task<Result<AppointmentReportDto>> GetAppointmentReportAsync(
        DateOnly? from, DateOnly? to, Guid? doctorId, CancellationToken ct = default)
    {
        var range = ResolveRange(from, to);
        if (range.IsFailure)
            return Result.Failure<AppointmentReportDto>(range.Error);
        var (start, endExclusive, fromDate, toDate) = range.Value;

        var query = _db.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= start && a.StartTime < endExclusive);
        if (doctorId is { } did)
            query = query.Where(a => a.DoctorId == did);

        var rows = await query
            .Select(a => new { a.StartTime, a.Status })
            .ToListAsync(ct);

        var byDay = rows
            .GroupBy(r => LocalDate(r.StartTime))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Status).ToList());

        var days = new List<AppointmentDayReport>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var statuses = byDay.TryGetValue(d, out var list) ? list : new List<AppointmentStatus>();
            days.Add(new AppointmentDayReport(d, statuses.Count, Breakdown(statuses)));
        }

        return Result.Success(new AppointmentReportDto(fromDate, toDate, rows.Count, days));
    }

    public async Task<Result<IReadOnlyList<DoctorProductivityDto>>> GetDoctorProductivityAsync(
        DateOnly? from, DateOnly? to, Guid? doctorId, Guid? restrictUserId, CancellationToken ct = default)
    {
        var range = ResolveRange(from, to);
        if (range.IsFailure)
            return Result.Failure<IReadOnlyList<DoctorProductivityDto>>(range.Error);
        var (start, endExclusive, _, _) = range.Value;

        // Bác sĩ đăng nhập chỉ xem của mình: phân giải doctorId từ tài khoản, ghi đè tham số truyền vào.
        if (restrictUserId is { } userId)
        {
            var linkedId = await _db.Doctors.AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(ct);

            if (linkedId is null)
                return Result.Success<IReadOnlyList<DoctorProductivityDto>>(Array.Empty<DoctorProductivityDto>());

            doctorId = linkedId;
        }

        var doctorsQuery = _db.Doctors.AsNoTracking();
        if (doctorId is { } did)
            doctorsQuery = doctorsQuery.Where(d => d.Id == did);

        var doctors = await doctorsQuery
            .OrderBy(d => d.Code)
            .Select(d => new { d.Id, d.Code, d.FullName })
            .ToListAsync(ct);

        var appts = await _db.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= start && a.StartTime < endExclusive)
            .Select(a => new { a.DoctorId, a.Status })
            .ToListAsync(ct);

        var encs = await _db.Encounters.AsNoTracking()
            .Where(e => e.CreatedAt >= start && e.CreatedAt < endExclusive)
            .Select(e => e.DoctorId)
            .ToListAsync(ct);

        var apptByDoctor = appts.GroupBy(a => a.DoctorId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var encByDoctor = encs.GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = doctors.Select(d =>
        {
            var list = apptByDoctor.TryGetValue(d.Id, out var a) ? a : new();
            var encounters = encByDoctor.TryGetValue(d.Id, out var e) ? e : 0;
            return new DoctorProductivityDto(
                d.Id,
                d.Code,
                d.FullName,
                list.Count,
                list.Count(x => x.Status == AppointmentStatus.Completed),
                encounters);
        }).ToList();

        return Result.Success<IReadOnlyList<DoctorProductivityDto>>(result);
    }

    public async Task<Result<RevenueReportDto>> GetRevenueReportAsync(
        DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var range = ResolveRange(from, to);
        if (range.IsFailure)
            return Result.Failure<RevenueReportDto>(range.Error);
        var (start, endExclusive, fromDate, toDate) = range.Value;

        var rows = await _db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= start && i.PaidAt < endExclusive)
            .Select(i => new
            {
                i.PaidAt,
                Items = i.Items.Select(x => new { x.ItemType, x.LineTotal }).ToList()
            })
            .ToListAsync(ct);

        var byDay = rows
            .GroupBy(r => LocalDate(r.PaidAt!.Value))
            .ToDictionary(g => g.Key, g => g.SelectMany(x => x.Items).ToList());

        var days = new List<RevenueDayReport>();
        decimal svc = 0, med = 0, para = 0, other = 0;
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var items = byDay.TryGetValue(d, out var list) ? list : new();
            var dayServiceFee = items.Where(x => x.ItemType == InvoiceItemType.ServiceFee).Sum(x => x.LineTotal);
            var dayMedication = items.Where(x => x.ItemType == InvoiceItemType.Medication).Sum(x => x.LineTotal);
            var dayParaclinical = items.Where(x => x.ItemType == InvoiceItemType.Paraclinical).Sum(x => x.LineTotal);
            var dayOther = items.Where(x => x.ItemType == InvoiceItemType.Other).Sum(x => x.LineTotal);
            var dayTotal = dayServiceFee + dayMedication + dayParaclinical + dayOther;

            svc += dayServiceFee; med += dayMedication; para += dayParaclinical; other += dayOther;
            days.Add(new RevenueDayReport(d, dayTotal, dayServiceFee, dayMedication, dayParaclinical, dayOther));
        }

        return Result.Success(new RevenueReportDto(
            fromDate, toDate, svc + med + para + other, svc, med, para, other, days));
    }

    /// <summary>
    /// Chuẩn hoá khoảng ngày báo cáo. Trả về mốc UTC [start, endExclusive) đã quy đổi múi giờ phòng khám
    /// cùng cặp ngày địa phương (from, to). Mặc định 7 ngày gần nhất khi thiếu tham số.
    /// </summary>
    private static Result<(DateTimeOffset Start, DateTimeOffset EndExclusive, DateOnly From, DateOnly To)>
        ResolveRange(DateOnly? from, DateOnly? to)
    {
        var toDate = to ?? TodayLocal();
        var fromDate = from ?? toDate.AddDays(-(DefaultRangeDays - 1));

        if (fromDate > toDate)
            return Result.Failure<(DateTimeOffset, DateTimeOffset, DateOnly, DateOnly)>(
                Error.Validation("Report.InvalidRange", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc."));

        if (toDate.DayNumber - fromDate.DayNumber + 1 > MaxRangeDays)
            return Result.Failure<(DateTimeOffset, DateTimeOffset, DateOnly, DateOnly)>(
                Error.Validation("Report.RangeTooWide", $"Khoảng ngày tối đa {MaxRangeDays} ngày."));

        return Result.Success((DayStart(fromDate), DayStart(toDate).AddDays(1), fromDate, toDate));
    }

    /// <summary>Đếm số lịch theo từng trạng thái (tổng hợp trong bộ nhớ trên tập nhỏ đã lọc theo ngày).</summary>
    private static AppointmentStatusBreakdown Breakdown(IReadOnlyCollection<AppointmentStatus> statuses) => new(
        statuses.Count(s => s == AppointmentStatus.Scheduled),
        statuses.Count(s => s == AppointmentStatus.CheckedIn),
        statuses.Count(s => s == AppointmentStatus.InProgress),
        statuses.Count(s => s == AppointmentStatus.Completed),
        statuses.Count(s => s == AppointmentStatus.Cancelled),
        statuses.Count(s => s == AppointmentStatus.NoShow));

    private static DateOnly TodayLocal() =>
        DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(ClinicOffset).DateTime);

    private static DateOnly LocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(ClinicOffset).DateTime);

    /// <summary>
    /// Thời điểm 00:00 (giờ phòng khám) của một ngày, quy đổi về UTC (offset=0) để so với cột
    /// "timestamp with time zone" — Npgsql chỉ chấp nhận DateTimeOffset offset=0 khi bind tham số.
    /// </summary>
    private static DateTimeOffset DayStart(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), ClinicOffset).ToUniversalTime();
}
