using CardBatchIntake.Api.Models;
using CardBatchIntake.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardBatchIntake.Api.Controllers;

// Receives batch requests and returns the appropriate result to the client.
[ApiController]
[Route("api/batches")]
public sealed class BatchesController(IBatchService batchService) : ControllerBase
{
    // Submits a batch and reports whether it was accepted or needs correction.
    [HttpPost]
    [ProducesResponseType(typeof(Batch), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Submit([FromBody] BatchRequest request)
    {
        var result = batchService.Submit(request);
        return result.Status switch
        {
            BatchSubmissionStatus.Created => CreatedAtAction(
                nameof(GetById), new { id = result.Batch!.Id }, result.Batch),
            BatchSubmissionStatus.ValidationFailed => UnprocessableEntity(new { errors = result.Errors }),
            BatchSubmissionStatus.DuplicateFileName => Conflict(new
            {
                message = "A batch with this fileName has already been submitted."
            }),
            _ => Problem()
        };
    }

    // Returns one batch, or reports that it could not be found.
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(Batch), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id) =>
        batchService.GetById(id) is { } batch ? Ok(batch) : NotFound();

    // Returns a page of batches, optionally narrowed by client and status.
    [HttpGet]
    [ProducesResponseType(typeof(BatchListResponse), StatusCodes.Status200OK)]
    public ActionResult<BatchListResponse> List(
        [FromQuery] string? clientCode,
        [FromQuery] BatchStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize) =>
        Ok(batchService.List(clientCode, status, page, pageSize));

    // Marks an accepted batch as released for production.
    [HttpPost("{id:guid}/release")]
    [ProducesResponseType(typeof(Batch), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Release(Guid id)
    {
        var result = batchService.Release(id);
        return result.Status switch
        {
            BatchReleaseStatus.Released => Ok(result.Batch),
            BatchReleaseStatus.NotFound => NotFound(),
            BatchReleaseStatus.Conflict => Conflict(new
            {
                message = "Only Accepted batches can be released."
            }),
            _ => Problem()
        };
    }
}