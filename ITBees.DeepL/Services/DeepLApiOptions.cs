namespace ITBees.DeepL.Services;

/// <summary>
/// Host-side options of the DeepL client, registered as a singleton by DeepLIntegrationSetup.Register.
/// </summary>
public class DeepLApiOptions
{
    /// <summary>
    /// Addresses accepted as the API address besides the official DeepL endpoints - e.g. a company proxy in
    /// front of DeepL. Full https URLs without user info, query or fragment ("https://deepl-proxy.example.com").
    /// The operator can only pick an address from this list in the panel; anything else is refused before any
    /// request is sent, so a mistyped or hostile address cannot make the server call other hosts (SSRF).
    /// </summary>
    public List<string> AdditionalAllowedBaseUrls { get; set; } = new();
}
