namespace CardBatchIntake.Api.Models;

// Shows whether a batch is ready for production or has been released.
public enum BatchStatus { Accepted, Released }

// Contains the client, file, and card records submitted as one batch.
public sealed class BatchRequest
{
    public string ClientCode { get; set; } = "";
    public string FileName { get; set; } = "";
    public int ExpectedRecordCount { get; set; }
    public List<CardRecord> Records { get; set; } = new();
}

// Contains the information for one card in a submitted batch.
public sealed class CardRecord
{
    public string RecordId { get; set; } = "";
    public string CardholderName { get; set; } = "";
    public string MaskedPan { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string Expiry { get; set; } = "";
}

// Represents a valid batch saved by the API.
public sealed class Batch
{
    public Guid Id { get; set; }
    public string ClientCode { get; set; } = "";
    public string FileName { get; set; } = "";
    public int ExpectedRecordCount { get; set; }
    public List<CardRecord> Records { get; set; } = new();
    public BatchStatus Status { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}

// Explains a problem found in a submitted request.
public sealed record ValidationError(string Field, string Message, string Code = "VALIDATION_ERROR");

// Contains one page of matching batches and the total number of matches.
public sealed record BatchListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<Batch> Items);