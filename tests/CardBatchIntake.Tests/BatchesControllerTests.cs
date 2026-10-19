using CardBatchIntake.Api.Controllers;
using CardBatchIntake.Api.Models;
using CardBatchIntake.Api.Repositories;
using CardBatchIntake.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CardBatchIntake.Tests;

public sealed class BatchesControllerTests
{
    private readonly InMemoryBatchRepository _repository = new();
    private readonly BatchesController _controller;

    public BatchesControllerTests()
    {
        _controller = new BatchesController(new BatchService(_repository, new BatchValidator()));
    }

    [Fact]
    public void SubmitCreatedBatchReturns201AndGetByIdLocationAction()
    {
        var result = _controller.Submit(ValidRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(nameof(BatchesController.GetById), created.ActionName);
    }

    [Fact]
    public void SubmitInvalidBatchReturns422()
    {
        var request = ValidRequest();
        request.ClientCode = "bad";

        var result = Assert.IsAssignableFrom<ObjectResult>(_controller.Submit(request));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
    }

    [Fact]
    public void SubmitDuplicateBatchReturns409()
    {
        var request = ValidRequest();
        _controller.Submit(request);

        var result = Assert.IsAssignableFrom<ObjectResult>(_controller.Submit(request));

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public void GetMissingBatchReturns404()
    {
        Assert.IsType<NotFoundResult>(_controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void ReleaseMissingBatchReturns404()
    {
        Assert.IsType<NotFoundResult>(_controller.Release(Guid.NewGuid()));
    }

    private static BatchRequest ValidRequest() => new()
    {
        ClientCode = "KAYA",
        FileName = $"KAYA_{DateTime.UtcNow:yyyyMMdd}_001.txt",
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