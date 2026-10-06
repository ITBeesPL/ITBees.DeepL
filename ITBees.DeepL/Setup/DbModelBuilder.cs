using ITBees.DeepL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITBees.DeepL.Setup;

public static class DbModelBuilder
{
    /// <summary>
    /// Registers the DeepL settings entity in the host's EF model - call it in OnModelCreating and add a migration
    /// (one table: DeepLIntegrationSettings).
    /// </summary>
    public static void Register(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeepLIntegrationSettings>().HasKey(x => x.Id);
    }
}
