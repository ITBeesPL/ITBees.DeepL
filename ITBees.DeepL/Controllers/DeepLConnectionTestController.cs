using ITBees.DeepL.Models;
using ITBees.DeepL.Services;
using ITBees.RestfulApiControllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.DeepL.Controllers;

/// <summary>
/// "Testuj połączenie" in the DeepL tab: checks the key (from the form or, when empty, the saved one) with
/// GET /v2/usage. Nothing is saved.
/// </summary>
[Authorize(Roles = "PlatformOperator")]
public class DeepLConnectionTestController : RestfulControllerBase<DeepLConnectionTestController>
{
    private readonly IDeepLConnectionTestService _deepLConnectionTestService;

    public DeepLConnectionTestController(ILogger<DeepLConnectionTestController> logger,
        IDeepLConnectionTestService deepLConnectionTestService) : base(logger)
    {
        _deepLConnectionTestService = deepLConnectionTestService;
    }

    [HttpPost]
    [Produces<DeepLConnectionTestVm>]
    public Task<IActionResult> Post([FromBody] DeepLConnectionTestIm deepLConnectionTestIm)
    {
        return ReturnOkResultAsync(async () =>
            (object)await _deepLConnectionTestService.TestAsync(deepLConnectionTestIm, HttpContext.RequestAborted));
    }
}
