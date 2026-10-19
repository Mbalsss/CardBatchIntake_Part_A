using CardBatchIntake.Api.Models;

namespace CardBatchIntake.Api.Services;

// Defines the actions available for submitting, finding, listing, and releasing batches.
public interface IBatchService
{
    // Checks a submission and saves it only when it is valid and not a duplicate.
    BatchSubmissionResult Submit(BatchRequest request);

    // Finds one saved batch by its identifier, or returns no result when it is missing.
    Batch? GetById(Guid id);

    // Finds batches for a client or status and returns only the requested page.
    BatchListResponse List(string? clientCode, BatchStatus? status, int? page, int? pageSize);

    // Moves an accepted batch to released, or reports why it could not be released.
    BatchReleaseResult Release(Guid id);
}

// Describes whether a submitted batch was saved or why it was rejected.
public enum BatchSubmissionStatus
{
    Created,
    ValidationFailed,
    DuplicateFileName
}

// Returns the outcome and any validation details from a batch submission.
public sealed record BatchSubmissionResult(
    BatchSubmissionStatus Status,
    Batch? Batch = null,
    IReadOnlyList<ValidationError>? Errors = null);

public enum BatchReleaseStatus
// Describes whether a batch was released or could not be released.
{
    Released,
    NotFound,
    Conflict
}

// Returns the outcome of a request to release a batch.
public sealed record BatchReleaseResult(BatchReleaseStatus Status, Batch? Batch = null);