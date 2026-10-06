using ITBees.DeepL.Entities;
using ITBees.DeepL.Services;

namespace ITBees.DeepL.Models;

public class DeepLIntegrationSettingsVm
{
    public DeepLIntegrationSettingsVm()
    {
    }

    public DeepLIntegrationSettingsVm(DeepLIntegrationSettings x)
    {
        Enabled = x.Enabled;
        ApiKey = x.ApiKey;
        BaseUrl = x.BaseUrl;
        // Display only - the allow-list check happens when the address is saved or used, so a row saved with an
        // address that is no longer allowed still shows up in the panel (and fails the connection test).
        EffectiveBaseUrl = string.IsNullOrWhiteSpace(x.BaseUrl)
            ? DeepLApiUrl.Resolve(x.ApiKey, "")
            : x.BaseUrl.Trim().TrimEnd('/');
        Modified = x.Modified;
    }

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = "";

    /// <summary>Custom API address; empty when the address follows the key (Free / Pro plan).</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>The address requests actually go to: BaseUrl when set, otherwise chosen by the key.</summary>
    public string EffectiveBaseUrl { get; set; } = DeepLApiUrl.ProApiUrl;

    public DateTime? Modified { get; set; }
}

public class DeepLIntegrationSettingsIm
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Leave empty to let the key decide between the Free and the Pro endpoint. Only the DeepL endpoints and
    /// proxies configured by the host are accepted; anything else is refused with 400.
    /// </summary>
    public string BaseUrl { get; set; } = "";
}
