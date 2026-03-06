using HWSETA_Impact_Hub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HWSETA_Impact_Hub.Infrastructure
{
    /// <summary>
    /// Used exclusively by EF Core design-time tools (dotnet ef migrations add/update).
    /// Passes enc: null so ConfigureBeneficiary falls into its else-branch,
    /// applying plain nvarchar column sizes with no Value Converters attached.
    /// The real runtime DbContext (registered in Program.cs) receives the
    /// IAesEncryptionService singleton and activates the converters normally.
    /// </summary>
    public sealed class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Load appsettings so the connection string is available
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json.");

            optionsBuilder.UseSqlServer(connectionString);

            // enc: null — design-time only, no encryption keys needed for migrations
            return new ApplicationDbContext(optionsBuilder.Options, enc: null);
        }
    }
}