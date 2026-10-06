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
        EffectiveBaseUrl = DeepLApiUrl.Resolve(x.ApiKey, x.BaseUrl);
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

    /// <summary>Leave empty to let the key decide between the Free and the Pro endpoint.</summary>
    public string BaseUrl { get; set; } = "";
}
