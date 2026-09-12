using ClinicManagement.Application.Visits.Dtos;
using ClinicManagement.Domain.Visits;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Visits;

public interface IVisitService
{
    Task<Result<VisitDto>> CreateAsync(CreateVisitRequest request, CancellationToken ct = default);
    Task<Result<VisitDto>> AddServiceAsync(Guid visitId, AddVisitServiceRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<VisitListItemDto>>> GetListAsync(
        int page, int pageSize, Guid? patientId, VisitStatus? status, DateOnly? date, CancellationToken ct = default);
    Task<Result<VisitDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VisitDto>> CloseAsync(Guid id, CancellationToken ct = default);
    Task<Result<VisitDto>> CancelAsync(Guid id, CancellationToken ct = default);
    Task<Result<VisitDto>> ReopenAsync(Guid id, CancellationToken ct = default);
}
