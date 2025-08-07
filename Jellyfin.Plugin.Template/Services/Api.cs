using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Template.Services;

/// <summary>
/// CollectionImportController.
/// </summary>
[ApiController]
// [Authorize]
[Route("api")]
public class CollectionImportController : ControllerBase
{
    private readonly ILogger<CollectionImportController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionImportController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CollectionImportController(ILogger<CollectionImportController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint that returns a simple greeting.
    /// </summary>
    /// <returns>A string containing "Hello World".</returns>
    [HttpGet("test")]
    // [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string> Test()
    {
        _logger.LogInformation("GET /collectionimport/test called");
        return Ok("Hello World");
    }

    // Existing Home method and other code remains unchanged...
}
