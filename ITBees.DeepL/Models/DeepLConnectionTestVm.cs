namespace ITBees.DeepL.Models;

/// <summary>
/// Key and address to check; empty fields mean the saved settings - so a key can be checked before it is saved.
/// </summary>
public class DeepLConnectionTestIm
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}

public class DeepLConnectionTestVm
{
    public bool Success { get; set; }

    /// <summary>Message for the operator (on failure - the reason).</summary>
    public string Message { get; set; } = "";

    /// <summary>API address the test went to.</summary>
    public string ApiUrl { get; set; } = "";

    /// <summary>"Free" or "Pro" - the DeepL plan the address belongs to; null for a custom address.</summary>
    public string? Plan { get; set; }

    /// <summary>Characters translated in the current billing period.</summary>
    public long CharacterCount { get; set; }

    /// <summary>Character limit of the current billing period (0 = not reported).</summary>
    public long CharacterLimit { get; set; }

    /// <summary>CharacterCount as a percentage of CharacterLimit, 0-100.</summary>
    public int UsagePercent { get; set; }
}
