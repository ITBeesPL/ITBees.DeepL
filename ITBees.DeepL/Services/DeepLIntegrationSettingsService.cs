using ITBees.DeepL.Entities;
using ITBees.DeepL.Models;
using ITBees.Interfaces.Repository;

namespace ITBees.DeepL.Services;

/// <summary>
/// DeepL settings kept in the host database (one row). The host registers the entity with
/// <see cref="Setup.DbModelBuilder.Register"/> and provides the generic ITBees repositories
/// (IReadOnlyRepository / IWriteOnlyRepository).
/// </summary>
public class DeepLIntegrationSettingsService : IDeepLIntegrationSettingsService
{
    private readonly IReadOnlyRepository<DeepLIntegrationSettings> _settingsRoRepo;
    private readonly IWriteOnlyRepository<DeepLIntegrationSettings> _settingsWoRepo;

    public DeepLIntegrationSettingsService(
        IReadOnlyRepository<DeepLIntegrationSettings> settingsRoRepo,
        IWriteOnlyRepository<DeepLIntegrationSettings> settingsWoRepo)
    {
        _settingsRoRepo = settingsRoRepo;
        _settingsWoRepo = settingsWoRepo;
    }

    public DeepLIntegrationSettingsVm Get()
    {
        var settings = _settingsRoRepo.GetData(x => true).FirstOrDefault();
        return settings == null ? new DeepLIntegrationSettingsVm() : new DeepLIntegrationSettingsVm(settings);
    }

    public DeepLIntegrationSettingsVm Update(DeepLIntegrationSettingsIm im, Guid? modifiedByGuid = null)
    {
        var apiKey = im.ApiKey?.Trim() ?? "";
        var baseUrl = NormalizeBaseUrl(im.BaseUrl);
        var existing = _settingsRoRepo.GetData(x => true).FirstOrDefault();

        if (existing == null)
        {
            var inserted = _settingsWoRepo.InsertData(new DeepLIntegrationSettings()
            {
                Enabled = im.Enabled,
                ApiKey = apiKey,
                BaseUrl = baseUrl,
                Modified = DateTime.Now,
                ModifiedByGuid = modifiedByGuid
            });

            return new DeepLIntegrationSettingsVm(inserted);
        }

        var updated = _settingsWoRepo.UpdateData(x => x.Id == existing.Id, x =>
        {
            x.Enabled = im.Enabled;
            x.ApiKey = apiKey;
            x.BaseUrl = baseUrl;
            x.Modified = DateTime.Now;
            x.ModifiedByGuid = modifiedByGuid;
        }).First();

        return new DeepLIntegrationSettingsVm(updated);
    }

    public DeepLIntegrationSettings? GetEnabledSettingsOrNull()
    {
        var settings = _settingsRoRepo.GetData(x => true).FirstOrDefault();
        if (settings == null || settings.Enabled == false || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return null;
        }

        settings.BaseUrl = NormalizeBaseUrl(settings.BaseUrl);
        return settings;
    }

    /// <summary>Empty stays empty (the key picks the Free / Pro address); otherwise trimmed, no trailing slash.</summary>
    private static string NormalizeBaseUrl(string? baseUrl)
    {
        return baseUrl?.Trim().TrimEnd('/') ?? "";
    }
}
