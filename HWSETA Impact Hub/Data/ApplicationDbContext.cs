using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Data
{
    public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<Employer> Employers => Set<Employer>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<AuditEvent>(e =>
            {
                e.ToTable("AuditEvents");
                e.HasKey(x => x.Id);

                e.Property(x => x.ActionType).HasMaxLength(40).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
                e.Property(x => x.EntityId).HasMaxLength(80);

                e.Property(x => x.UserEmail).HasMaxLength(256);
                e.Property(x => x.UserRole).HasMaxLength(80);

                e.Property(x => x.IpAddress).HasMaxLength(80);
                e.Property(x => x.UserAgent).HasMaxLength(512);

                e.Property(x => x.RequestPath).HasMaxLength(256);
                e.Property(x => x.HttpMethod).HasMaxLength(16);

                // Store JSON as text in SQL Server now
                e.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");

                // Concurrency
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            b.Entity<Programme>(e =>
            {
                e.ToTable("Programmes");
                e.HasKey(x => x.Id);

                e.Property(x => x.ProgrammeName).HasMaxLength(200).IsRequired();
                e.Property(x => x.ProgrammeType).HasMaxLength(60).IsRequired();
                e.Property(x => x.Province).HasMaxLength(60).IsRequired();
                e.Property(x => x.Notes).HasColumnType("nvarchar(max)");

                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasIndex(x => new { x.CohortYear, x.Province });
            });

            b.Entity<Provider>(e =>
            {
                e.ToTable("Providers");
                e.HasKey(x => x.Id);

                e.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
                e.Property(x => x.AccreditationNo).HasMaxLength(80).IsRequired();
                e.Property(x => x.Province).HasMaxLength(60).IsRequired();

                e.Property(x => x.ContactEmail).HasMaxLength(256);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasIndex(x => x.AccreditationNo).IsUnique();
            });

            b.Entity<Employer>(e =>
            {
                e.ToTable("Employers");
                e.HasKey(x => x.Id);

                e.Property(x => x.EmployerName).HasMaxLength(200).IsRequired();
                e.Property(x => x.Sector).HasMaxLength(120).IsRequired();
                e.Property(x => x.Province).HasMaxLength(60).IsRequired();

                e.Property(x => x.ContactEmail).HasMaxLength(256);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasIndex(x => new { x.Province, x.Sector });
            });
        }
    }
}