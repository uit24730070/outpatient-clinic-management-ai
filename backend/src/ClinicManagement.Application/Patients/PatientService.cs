using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Dtos;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Patients;

public sealed class PatientService : IPatientService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public PatientService(IAppDbContext db) => _db = db;

    public async Task<Result<PatientDto>> CreateAsync(CreatePatientRequest request, CancellationToken ct = default)
    {
        var code = await GenerateCodeAsync(ct);
        var patient = new Patient(
            code,
            request.FullName.Trim(),
            request.DateOfBirth,
            request.Gender,
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Address));

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);

        return PatientDto.FromEntity(patient);
    }

    public async Task<Result<PagedResult<PatientDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(term) ||
                p.Code.ToLower().Contains(term) ||
                (p.PhoneNumber != null && p.PhoneNumber.ToLower().Contains(term)));
        }

        var ordered = ApplySort(query, sortBy, sortDesc);

        var total = await query.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PatientDto.FromEntity(p))
            .ToListAsync(ct);

        return new PagedResult<PatientDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định CreatedAt desc khi không chỉ định.</summary>
    private static IOrderedQueryable<Patient> ApplySort(IQueryable<Patient> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "code" => desc ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
            "fullName" => desc ? query.OrderByDescending(p => p.FullName) : query.OrderBy(p => p.FullName),
            "dateOfBirth" => desc ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            "phoneNumber" => desc ? query.OrderByDescending(p => p.PhoneNumber) : query.OrderBy(p => p.PhoneNumber),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };

    public async Task<Result<PatientDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return patient is null
            ? Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {id}.")
            : PatientDto.FromEntity(patient);
    }

    public async Task<Result<PatientDto>> UpdateAsync(
        Guid id, UpdatePatientRequest request, CancellationToken ct = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (patient is null)
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {id}.");

        patient.UpdateDetails(
            request.FullName.Trim(),
            request.DateOfBirth,
            request.Gender,
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Address));

        await _db.SaveChangesAsync(ct);
        return PatientDto.FromEntity(patient);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (patient is null)
            return Result.Failure(Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {id}."));

        patient.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Sinh mã bệnh nhân dạng BN-000001 theo số thứ tự lớn nhất hiện có.
    /// Đếm cả bản ghi đã xoá mềm (<see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters"/>)
    /// để tránh trùng mã với bản ghi đang chiếm giá trị unique.
    /// </summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Patients.IgnoreQueryFilters().CountAsync(ct);
        return $"BN-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
