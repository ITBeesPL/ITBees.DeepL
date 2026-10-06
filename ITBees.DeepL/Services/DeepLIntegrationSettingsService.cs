using ITBees.DeepL.Entities;
using ITBees.DeepL.Models;
using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;

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
    private readonly DeepLApiOptions _options;

    public DeepLIntegrationSettingsService(
        IReadOnlyRepository<DeepLIntegrationSettings> settingsRoRepo,
        IWriteOnlyRepository<DeepLIntegrationSettings> settingsWoRepo,
        DeepLApiOptions? options = null)
    {
        _settingsRoRepo = settingsRoRepo;
        _settingsWoRepo = settingsWoRepo;
        _options = options ?? new DeepLApiOptions();
    }

    public DeepLIntegrationSettingsVm Get()
    {
        var settings = _settingsRoRepo.GetData(x => true).FirstOrDefault();
        return settings == null ? new DeepLIntegrationSettingsVm() : new DeepLIntegrationSettingsVm(settings);
    }

    public DeepLIntegrationSettingsVm Update(DeepLIntegrationSettingsIm im, Guid? modifiedByGuid = null)
    {
        var apiKey = im.ApiKey?.Trim() ?? "";
        var baseUrl = ValidateBaseUrl(im.BaseUrl);
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

        settings.BaseUrl = settings.BaseUrl?.Trim().TrimEnd('/') ?? "";
        return settings;
    }

    /// <summary>
    /// Empty stays empty (the key picks the Free / Pro address). Anything else must be one of the allowed
    /// addresses - a mistyped or foreign address is refused with 400 instead of being saved and called later.
    /// </summary>
    private string ValidateBaseUrl(string? baseUrl)
    {
        var url = baseUrl?.Trim() ?? "";
        if (url == "")
        {
            return "";
        }

        try
        {
            return DeepLApiUrl.EnsureAllowed(url, _options);
        }
        catch (DeepLApiException e)
        {
            throw new FasApiErrorException(e.Message, 400);
        }
    }
}
