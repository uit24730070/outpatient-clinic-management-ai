using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Doctors;

public sealed class DoctorService : IDoctorService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public DoctorService(IAppDbContext db) => _db = db;

    public async Task<Result<DoctorDto>> CreateAsync(CreateDoctorRequest request, CancellationToken ct = default)
    {
        if (!await SpecialtyExistsAsync(request.SpecialtyId, ct))
            return Error.Validation("Doctor.SpecialtyNotFound",
                $"Chuyên khoa với Id {request.SpecialtyId} không tồn tại.");

        var code = await GenerateCodeAsync(ct);
        var doctor = new Doctor(
            code,
            request.FullName.Trim(),
            request.SpecialtyId,
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Email));

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(doctor.Id, ct))!;
    }

    public async Task<Result<PagedResult<DoctorDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Doctors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d =>
                d.FullName.ToLower().Contains(term) ||
                d.Code.ToLower().Contains(term) ||
                (d.PhoneNumber != null && d.PhoneNumber.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await Project(ApplySort(query, sortBy, sortDesc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DoctorDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định CreatedAt desc khi không chỉ định.</summary>
    private static IOrderedQueryable<Doctor> ApplySort(IQueryable<Doctor> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "code" => desc ? query.OrderByDescending(d => d.Code) : query.OrderBy(d => d.Code),
            "fullName" => desc ? query.OrderByDescending(d => d.FullName) : query.OrderBy(d => d.FullName),
            "phoneNumber" => desc ? query.OrderByDescending(d => d.PhoneNumber) : query.OrderBy(d => d.PhoneNumber),
            _ => query.OrderByDescending(d => d.CreatedAt),
        };

    public async Task<Result<DoctorDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {id}.")
            : dto;
    }

    public async Task<Result<DoctorDto>> UpdateAsync(
        Guid id, UpdateDoctorRequest request, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doctor is null)
            return Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {id}.");

        if (!await SpecialtyExistsAsync(request.SpecialtyId, ct))
            return Error.Validation("Doctor.SpecialtyNotFound",
                $"Chuyên khoa với Id {request.SpecialtyId} không tồn tại.");

        doctor.UpdateDetails(
            request.FullName.Trim(),
            request.SpecialtyId,
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Email));

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(doctor.Id, ct))!;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doctor is null)
            return Result.Failure(Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {id}."));

        doctor.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<DoctorDto>> LinkUserAsync(
        Guid doctorId, LinkUserRequest request, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, ct);
        if (doctor is null)
            return Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {doctorId}.");

        if (doctor.UserId is not null)
            return Error.Conflict("Doctor.AlreadyLinked",
                "Hồ sơ bác sĩ đã gắn một tài khoản. Hãy gỡ liên kết hiện tại trước.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
            return Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {request.UserId}.");

        if (user.Role != Domain.Users.UserRole.Doctor)
            return Error.Validation("Doctor.UserNotDoctor",
                "Chỉ có thể gắn tài khoản có vai trò Bác sĩ vào hồ sơ bác sĩ.");

        // Một tài khoản chỉ gắn tối đa một hồ sơ (unique index là chốt DB; kiểm ở đây cho thông báo thân thiện).
        var alreadyLinked = await _db.Doctors.AnyAsync(d => d.UserId == request.UserId, ct);
        if (alreadyLinked)
            return Error.Conflict("Doctor.UserAlreadyLinked",
                "Tài khoản này đã được gắn với một hồ sơ bác sĩ khác.");

        doctor.AssignUser(request.UserId);
        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(doctor.Id, ct))!;
    }

    public async Task<Result<DoctorDto>> UnlinkUserAsync(Guid doctorId, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, ct);
        if (doctor is null)
            return Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {doctorId}.");

        doctor.UnassignUser();
        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(doctor.Id, ct))!;
    }

    /// <summary>Ánh xạ truy vấn Bác sĩ sang DTO kèm tên chuyên khoa (subquery, null nếu khoa đã bị xoá).</summary>
    private IQueryable<DoctorDto> Project(IQueryable<Doctor> query) =>
        query.Select(d => new DoctorDto(
            d.Id,
            d.Code,
            d.FullName,
            d.SpecialtyId,
            _db.Specialties.Where(s => s.Id == d.SpecialtyId).Select(s => s.Name).FirstOrDefault(),
            d.PhoneNumber,
            d.Email,
            d.UserId,
            d.CreatedAt,
            d.UpdatedAt));

    private async Task<DoctorDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Doctors.AsNoTracking().Where(d => d.Id == id)).FirstOrDefaultAsync(ct);

    private async Task<bool> SpecialtyExistsAsync(Guid specialtyId, CancellationToken ct) =>
        await _db.Specialties.AnyAsync(s => s.Id == specialtyId, ct);

    /// <summary>Sinh mã bác sĩ dạng BS-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Doctors.IgnoreQueryFilters().CountAsync(ct);
        return $"BS-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
