using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Billing;

public sealed class ServicePriceService : IServicePriceService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public ServicePriceService(IAppDbContext db) => _db = db;

    public async Task<Result<ServicePriceDto>> CreateAsync(
        CreateServicePriceRequest request, CancellationToken ct = default)
    {
        var code = await GenerateCodeAsync(ct);
        var service = new ServicePrice(
            code,
            request.Name.Trim(),
            request.UnitPrice,
            NormalizeOptional(request.Description),
            request.Category);

        _db.ServicePrices.Add(service);
        await _db.SaveChangesAsync(ct);

        return ServicePriceDto.FromEntity(service);
    }

    public async Task<Result<PagedResult<ServicePriceDto>>> GetListAsync(
        int page, int pageSize, string? search, ServiceCategory? category,
        string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.ServicePrices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.Code.ToLower().Contains(term));
        }

        if (category is not null)
            query = query.Where(s => s.Category == category);

        var total = await query.CountAsync(ct);
        var items = await ApplySort(query, sortBy, sortDesc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ServicePriceDto.FromEntity(s))
            .ToListAsync(ct);

        return new PagedResult<ServicePriceDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định CreatedAt desc khi không chỉ định.</summary>
    private static IOrderedQueryable<ServicePrice> ApplySort(IQueryable<ServicePrice> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "code" => desc ? query.OrderByDescending(s => s.Code) : query.OrderBy(s => s.Code),
            "name" => desc ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "unitPrice" => desc ? query.OrderByDescending(s => s.UnitPrice) : query.OrderBy(s => s.UnitPrice),
            "category" => desc ? query.OrderByDescending(s => s.Category) : query.OrderBy(s => s.Category),
            _ => query.OrderByDescending(s => s.CreatedAt),
        };

    public async Task<Result<ServicePriceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _db.ServicePrices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return service is null
            ? Error.NotFound("ServicePrice.NotFound", $"Không tìm thấy dịch vụ với Id {id}.")
            : ServicePriceDto.FromEntity(service);
    }

    public async Task<Result<ServicePriceDto>> UpdateAsync(
        Guid id, UpdateServicePriceRequest request, CancellationToken ct = default)
    {
        var service = await _db.ServicePrices.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
            return Error.NotFound("ServicePrice.NotFound", $"Không tìm thấy dịch vụ với Id {id}.");

        service.UpdateDetails(
            request.Name.Trim(),
            request.UnitPrice,
            NormalizeOptional(request.Description),
            request.Category);

        await _db.SaveChangesAsync(ct);
        return ServicePriceDto.FromEntity(service);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _db.ServicePrices.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
            return Result.Failure(Error.NotFound("ServicePrice.NotFound", $"Không tìm thấy dịch vụ với Id {id}."));

        service.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Sinh mã dịch vụ dạng DV-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.ServicePrices.IgnoreQueryFilters().CountAsync(ct);
        return $"DV-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
