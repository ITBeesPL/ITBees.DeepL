using System.Security.Claims;
using ITBees.DeepL.Models;
using ITBees.DeepL.Services;
using ITBees.RestfulApiControllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.DeepL.Controllers;

/// <summary>
/// DeepL settings endpoint for the ITBees admin panels ("Integracje" tab). Discovered automatically by ASP.NET
/// once the library is referenced; required registration: DeepLIntegrationSetup.Register(services) +
/// DbModelBuilder.Register(modelBuilder).
/// </summary>
[Authorize(Roles = "PlatformOperator")]
public class DeepLIntegrationSettingsController : RestfulControllerBase<DeepLIntegrationSettingsController>
{
    private readonly IDeepLIntegrationSettingsService _deepLIntegrationSettingsService;

    public DeepLIntegrationSettingsController(ILogger<DeepLIntegrationSettingsController> logger,
        IDeepLIntegrationSettingsService deepLIntegrationSettingsService) : base(logger)
    {
        _deepLIntegrationSettingsService = deepLIntegrationSettingsService;
    }

    [HttpGet]
    [Produces<DeepLIntegrationSettingsVm>]
    public IActionResult Get()
    {
        return ReturnOkResult(() => _deepLIntegrationSettingsService.Get());
    }

    [HttpPut]
    [Produces<DeepLIntegrationSettingsVm>]
    public IActionResult Put([FromBody] DeepLIntegrationSettingsIm deepLIntegrationSettingsIm)
    {
        return ReturnOkResult(() =>
            _deepLIntegrationSettingsService.Update(deepLIntegrationSettingsIm, GetCurrentUserGuidOrNull()));
    }

    private Guid? GetCurrentUserGuidOrNull()
    {
        var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
