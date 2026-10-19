using Microsoft.AspNetCore.Mvc;

namespace CardBatchIntake.Api.Controllers;

// Provides a simple response to confirm that the API is running.
[ApiController]
[Route("")]
public sealed class StatusController : ControllerBase
{
    // Returns the service name and running status.
    [HttpGet]
    public IActionResult Get() => Ok(new { name = "Card Batch Intake API", status = "running" });
}