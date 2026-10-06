namespace ITBees.DeepL.Entities;

/// <summary>
/// DeepL API settings stored in the host application's database (one row), edited by the platform operator
/// in the admin panel ("Integracje" tab). Register the entity with <see cref="Setup.DbModelBuilder.Register"/>
/// in the host's OnModelCreating and add a migration.
/// </summary>
public class DeepLIntegrationSettings
{
    public int Id { get; set; }

    /// <summary>Main switch of the integration - translations are refused while it is off.</summary>
    public bool Enabled { get; set; }

    /// <summary>DeepL API authentication key (DeepL account -> API keys). Free plan keys end with ":fx".</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// DeepL API address without the trailing slash. Empty = chosen by the key: https://api-free.deepl.com
    /// for a Free plan key (":fx" suffix), https://api.deepl.com otherwise.
    /// </summary>
    public string BaseUrl { get; set; } = "";

    public DateTime? Modified { get; set; }
    public Guid? ModifiedByGuid { get; set; }
}
