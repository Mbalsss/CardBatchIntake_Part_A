using CardBatchIntake.Api.Models;
using CardBatchIntake.Api.Repositories;

namespace CardBatchIntake.Api.Services;

// Applies the batch rules and carries out the requested batch actions.
public sealed class BatchService(IBatchRepository repository, IBatchValidator validator) : IBatchService
{
    // Validates a new batch, prevents duplicate files, and saves accepted batches.
    public BatchSubmissionResult Submit(BatchRequest request)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return new(BatchSubmissionStatus.ValidationFailed, Errors: errors);

        if (repository.FileNameExists(request.FileName))
            return new(BatchSubmissionStatus.DuplicateFileName);

        var batch = new Batch
        {
            Id = Guid.NewGuid(),
            ClientCode = request.ClientCode,
            FileName = request.FileName,
            ExpectedRecordCount = request.ExpectedRecordCount,
            Records = request.Records.ToList(),
            Status = BatchStatus.Accepted,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        };
        repository.Add(batch);
        return new(BatchSubmissionStatus.Created, batch);
    }

    // Returns a saved batch when its identifier exists.
    public Batch? GetById(Guid id) => repository.Get(id);

    // Filters and sorts batches, then returns the requested page of results.
    public BatchListResponse List(string? clientCode, BatchStatus? status, int? page, int? pageSize)
    {
        var currentPage = Math.Max(1, page ?? 1);
        var size = Math.Clamp(pageSize ?? 20, 1, 100);
        var filtered = repository.List()
            .Where(batch => string.IsNullOrWhiteSpace(clientCode) ||
                batch.ClientCode.Equals(clientCode, StringComparison.OrdinalIgnoreCase))
            .Where(batch => status is null || batch.Status == status)
            .OrderByDescending(batch => batch.ReceivedAtUtc)
            .ToList();

        return new BatchListResponse(
            currentPage,
            size,
            filtered.Count,
            filtered.Skip((currentPage - 1) * size).Take(size).ToList());
    }

    // Releases an accepted batch and reports missing or already-released batches.
    public BatchReleaseResult Release(Guid id)
    {
        var batch = repository.Get(id);
        if (batch is null)
            return new(BatchReleaseStatus.NotFound);
        if (batch.Status != BatchStatus.Accepted)
            return new(BatchReleaseStatus.Conflict, batch);

        batch.Status = BatchStatus.Released;
        repository.Update(batch);
        return new(BatchReleaseStatus.Released, batch);
    }
}