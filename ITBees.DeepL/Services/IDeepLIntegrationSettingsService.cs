using ITBees.DeepL.Entities;
using ITBees.DeepL.Models;

namespace ITBees.DeepL.Services;

public interface IDeepLIntegrationSettingsService
{
    DeepLIntegrationSettingsVm Get();
    DeepLIntegrationSettingsVm Update(DeepLIntegrationSettingsIm im, Guid? modifiedByGuid = null);

    /// <summary>Saved settings, or null when the integration is switched off or has no API key.</summary>
    DeepLIntegrationSettings? GetEnabledSettingsOrNull();
}
