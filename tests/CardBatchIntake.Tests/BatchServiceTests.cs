using CardBatchIntake.Api.Models;
using CardBatchIntake.Api.Repositories;
using CardBatchIntake.Api.Services;
using Xunit;

namespace CardBatchIntake.Tests;

public sealed class BatchServiceTests
{
    private readonly InMemoryBatchRepository _repository = new();
    private readonly IBatchService _service;

    public BatchServiceTests()
    {
        _service = new BatchService(_repository, new BatchValidator());
    }

    [Fact]
    public void SubmitValidBatchStoresAcceptedBatchWithUtcTimestamp()
    {
        var result = _service.Submit(ValidRequest());

        Assert.Equal(BatchSubmissionStatus.Created, result.Status);
        Assert.NotNull(result.Batch);
        Assert.Equal(BatchStatus.Accepted, result.Batch.Status);
        Assert.Equal(TimeSpan.Zero, result.Batch.ReceivedAtUtc.Offset);
        Assert.Equal(result.Batch, _service.GetById(result.Batch.Id));
    }

    [Fact]
    public void SubmitInvalidBatchDoesNotStoreIt()
    {
        var request = ValidRequest();
        request.ClientCode = "bad";

        var result = _service.Submit(request);

        Assert.Equal(BatchSubmissionStatus.ValidationFailed, result.Status);
        Assert.NotEmpty(result.Errors!);
        Assert.Empty(_repository.List());
    }

    [Fact]
    public void SubmitDuplicateFileNameReturnsDuplicateOutcome()
    {
        var request = ValidRequest();
        Assert.Equal(BatchSubmissionStatus.Created, _service.Submit(request).Status);

        var result = _service.Submit(ValidRequest());

        Assert.Equal(BatchSubmissionStatus.DuplicateFileName, result.Status);
        Assert.Single(_repository.List());
    }

    [Fact]
    public void ListFiltersAndClampsPagination()
    {
        _service.Submit(ValidRequest());
        _service.Submit(ValidRequest("BANK", "BANK"));

        var result = _service.List("kaya", BatchStatus.Accepted, page: 0, pageSize: 200);

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("KAYA", result.Items[0].ClientCode);
    }

    [Fact]
    public void ReleaseTransitionsAcceptedBatchAndRejectsSecondRelease()
    {
        var batch = _service.Submit(ValidRequest()).Batch!;

        var released = _service.Release(batch.Id);
        var secondRelease = _service.Release(batch.Id);

        Assert.Equal(BatchReleaseStatus.Released, released.Status);
        Assert.Equal(BatchStatus.Released, released.Batch!.Status);
        Assert.Equal(BatchReleaseStatus.Conflict, secondRelease.Status);
    }

    [Fact]
    public void ReleaseMissingBatchReturnsNotFound()
    {
        Assert.Equal(BatchReleaseStatus.NotFound, _service.Release(Guid.NewGuid()).Status);
    }

    private static BatchRequest ValidRequest(string clientCode = "KAYA", string filePrefix = "KAYA") => new()
    {
        ClientCode = clientCode,
        FileName = $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd}_001.txt",
        ExpectedRecordCount = 1,
        Records = new()
        {
            new CardRecord
            {
                RecordId = "R0001",
                CardholderName = "T MOKOENA",
                MaskedPan = "************4821",
                ProductCode = "DEBIT_GOLD",
                Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy")
            }
        }
    };
}