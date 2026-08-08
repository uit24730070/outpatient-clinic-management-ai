using ClinicManagement.Application.Ai;
using ClinicManagement.Shared.Results;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>Kết quả backfill embedding: số phiếu khám đã lập chỉ mục.</summary>
public sealed record ReindexResult(int Indexed);

[Authorize]
[Route("api/ai")]
public sealed class AiController : ApiControllerBase
{
    private readonly IEncounterEmbeddingIndexer _indexer;

    public AiController(IEncounterEmbeddingIndexer indexer) => _indexer = indexer;

    /// <summary>
    /// Backfill embedding cho toàn bộ phiếu khám hiện có (bảo trì — chỉ Admin).
    /// Dùng khi bật khoá embedding lần đầu hoặc sau khi đổi model.
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken ct)
    {
        var count = await _indexer.ReindexAllAsync(ct);
        Result<ReindexResult> result = new ReindexResult(count);
        return ToResponse(result);
    }
}
