using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Data
{
    public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<Employer> Employers => Set<Employer>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Cohort> Cohorts => Set<Cohort>();
        public DbSet<EnrollmentStatusHistory> EnrollmentStatusHistories => Set<EnrollmentStatusHistory>();
        public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
        public DbSet<Address> Addresses => Set<Address>();

        // ── Lookups (all map to single "Lookups" table via TPH) ───────────
        public DbSet<Province> Provinces => Set<Province>();
        public DbSet<Gender> Genders => Set<Gender>();
        public DbSet<Race> Races => Set<Race>();
        public DbSet<CitizenshipStatus> CitizenshipStatuses => Set<CitizenshipStatus>();
        public DbSet<DisabilityStatus> DisabilityStatuses => Set<DisabilityStatus>();
        public DbSet<DisabilityType> DisabilityTypes => Set<DisabilityType>();
        public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
        public DbSet<EmploymentStatus> EmploymentStatuses => Set<EmploymentStatus>();
        public DbSet<QualificationType> QualificationTypes => Set<QualificationType>();
        public DbSet<FundingType> FundingTypes => Set<FundingType>();
        public DbSet<EmployerRegistrationType> EmployerRegistrationTypes => Set<EmployerRegistrationType>();
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // ══════════════════════════════════════════════════════════════
            // STEP 1 — Identity (always first)
            // ══════════════════════════════════════════════════════════════
            base.OnModelCreating(b);

            // ══════════════════════════════════════════════════════════════
            // STEP 2 — Lookup TPH table
            // All 12 lookup subtypes share one "Lookups" table.
            // EF adds a "Discriminator" column automatically.
            // ══════════════════════════════════════════════════════════════
            b.Entity<LookupBase>(e =>
            {
                e.ToTable("Lookups");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.IsActive).HasDefaultValue(true);
            });

            // Register each subtype — no extra config needed per type
            b.Entity<Province>();
            b.Entity<Gender>();
            b.Entity<Race>();
            b.Entity<CitizenshipStatus>();
            b.Entity<DisabilityStatus>();
            b.Entity<DisabilityType>();
            b.Entity<EducationLevel>();
            b.Entity<EmploymentStatus>();
            b.Entity<QualificationType>();
            b.Entity<FundingType>();
            b.Entity<EmployerRegistrationType>();
            b.Entity<DocumentType>();

            // ══════════════════════════════════════════════════════════════
            // STEP 3 — Domain entity configurations
            // ══════════════════════════════════════════════════════════════
            ConfigureAuditEvent(b);
            ConfigureProgramme(b);
            ConfigureProvider(b);
            ConfigureEmployer(b);
            ConfigureBeneficiary(b);
            ConfigureCohort(b);
            ConfigureEnrollment(b);
            ConfigureEnrollmentStatusHistory(b);
            ConfigureAddress(b);

            // ══════════════════════════════════════════════════════════════
            // STEP 4 — Global NoAction sweep (MUST be absolutely last)
            //
            // WHY: All lookup types use TPH. EF stores typeof(LookupBase)
            // as the PrincipalEntityType for every FK that points at any
            // lookup subtype (Gender, Race, Province, ProviderType, etc.).
            // SQL Server error 1785 fires because multiple paths cascade to
            // the same LookupBase table. The fix is NoAction — NOT Restrict.
            // Restrict still causes EF to emit ON DELETE CASCADE in certain
            // EF Core versions. NoAction emits ON DELETE NO ACTION which is
            // exactly what SQL Server requires.
            //
            // This sweep runs after ALL conventions have finalised so
            // nothing can overwrite it.
            // ══════════════════════════════════════════════════════════════
            foreach (var entityType in b.Model.GetEntityTypes())
            {
                foreach (var fk in entityType.GetForeignKeys())
                {
                    if (fk.PrincipalEntityType.ClrType == typeof(LookupBase)
                        || fk.PrincipalEntityType.ClrType.IsSubclassOf(typeof(LookupBase)))
                    {
                        fk.DeleteBehavior = DeleteBehavior.NoAction;
                    }
                }
            }
        }

        // ── AuditEvent ───────────────────────────────────────────────────
        private static void ConfigureAuditEvent(ModelBuilder b)
        {
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
                e.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Programme ────────────────────────────────────────────────────
        private static void ConfigureProgramme(ModelBuilder b)
        {
            b.Entity<Programme>(e =>
            {
                e.ToTable("Programmes");
                e.HasIndex(x => x.ProgrammeCode)
                 .IsUnique()
                 .HasFilter("[ProgrammeCode] IS NOT NULL");
                e.Property(x => x.ProgrammeName).HasMaxLength(200).IsRequired();
                e.HasOne(x => x.QualificationType)
                 .WithMany()
                 .HasForeignKey(x => x.QualificationTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Provider ─────────────────────────────────────────────────────
        private static void ConfigureProvider(ModelBuilder b)
        {
            b.Entity<Provider>(e =>
            {
                e.ToTable("Providers");
                e.HasIndex(x => x.AccreditationNo).IsUnique();
                e.HasIndex(x => x.ProviderCode)
                 .IsUnique()
                 .HasFilter("[ProviderCode] IS NOT NULL");
                e.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
                e.Property(x => x.AccreditationNo).HasMaxLength(60).IsRequired();
                e.Property(x => x.ContactName).HasMaxLength(120);
                e.Property(x => x.ContactEmail).HasMaxLength(256);
                e.Property(x => x.ContactPhone).HasMaxLength(30);
                e.Property(x => x.Phone).HasMaxLength(30);
                e.HasOne(x => x.Address)
                 .WithMany()
                 .HasForeignKey(x => x.AddressId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.ProviderType)
                 .WithMany()
                 .HasForeignKey(x => x.ProviderTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Employer ─────────────────────────────────────────────────────
        private static void ConfigureEmployer(ModelBuilder b)
        {
            b.Entity<Employer>(e =>
            {
                e.ToTable("Employers");
                e.HasIndex(x => x.EmployerCode).IsUnique();
                e.Property(x => x.EmployerCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.RegistrationNumber).HasMaxLength(50).IsRequired();
                e.Property(x => x.SetaLevyNumber).HasMaxLength(50).IsRequired();
                e.HasOne(x => x.Address)
                 .WithMany()
                 .HasForeignKey(x => x.AddressId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Beneficiary ──────────────────────────────────────────────────
        private static void ConfigureBeneficiary(ModelBuilder b)
        {
            b.Entity<Beneficiary>(e =>
            {
                e.ToTable("Beneficiaries");
                e.HasIndex(x => new { x.IdentifierType, x.IdentifierValue }).IsUnique();
                e.Property(x => x.IdentifierValue).HasMaxLength(80).IsRequired();
                e.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
                e.Property(x => x.LastName).HasMaxLength(120).IsRequired();
                e.Property(x => x.MobileNumber).HasMaxLength(30).IsRequired();
                e.Property(x => x.Email).HasMaxLength(256);
                e.HasOne(x => x.Address)
                 .WithMany()
                 .HasForeignKey(x => x.AddressId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Gender)
                 .WithMany()
                 .HasForeignKey(x => x.GenderId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Race)
                 .WithMany()
                 .HasForeignKey(x => x.RaceId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.CitizenshipStatus)
                 .WithMany()
                 .HasForeignKey(x => x.CitizenshipStatusId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.DisabilityStatus)
                 .WithMany()
                 .HasForeignKey(x => x.DisabilityStatusId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.DisabilityType)
                 .WithMany()
                 .HasForeignKey(x => x.DisabilityTypeId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.EducationLevel)
                 .WithMany()
                 .HasForeignKey(x => x.EducationLevelId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.EmploymentStatus)
                 .WithMany()
                 .HasForeignKey(x => x.EmploymentStatusId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Cohort ───────────────────────────────────────────────────────
        private static void ConfigureCohort(ModelBuilder b)
        {
            b.Entity<Cohort>(e =>
            {
                e.ToTable("Cohorts");
                e.HasIndex(x => x.CohortCode).IsUnique();
                e.Property(x => x.CohortCode).HasMaxLength(60).IsRequired();
                e.HasOne(x => x.Programme)
                 .WithMany()
                 .HasForeignKey(x => x.ProgrammeId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Provider)
                 .WithMany()
                 .HasForeignKey(x => x.ProviderId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Employer)
                 .WithMany()
                 .HasForeignKey(x => x.EmployerId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Enrollment ───────────────────────────────────────────────────
        private static void ConfigureEnrollment(ModelBuilder b)
        {
            b.Entity<Enrollment>(e =>
            {
                e.ToTable("Enrollments");
                e.HasIndex(x => new { x.BeneficiaryId, x.CohortId }).IsUnique();
                e.HasOne(x => x.Beneficiary)
                 .WithMany()
                 .HasForeignKey(x => x.BeneficiaryId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Cohort)
                 .WithMany()
                 .HasForeignKey(x => x.CohortId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.Notes).HasMaxLength(500);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── EnrollmentStatusHistory ──────────────────────────────────────
        private static void ConfigureEnrollmentStatusHistory(ModelBuilder b)
        {
            b.Entity<EnrollmentStatusHistory>(e =>
            {
                e.ToTable("EnrollmentStatusHistory");
                e.HasKey(x => x.Id);
                e.Property(x => x.Status).IsRequired();
                e.Property(x => x.StatusDate).IsRequired();
                e.Property(x => x.Reason).HasMaxLength(200);
                e.Property(x => x.Comment).HasMaxLength(1000);
                // Cascade IS correct here — history rows are owned by the
                // enrollment and have no path to LookupBase, so no cycle risk.
                e.HasOne(x => x.Enrollment)
                 .WithMany()
                 .HasForeignKey(x => x.EnrollmentId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }

        // ── Address ──────────────────────────────────────────────────────
        private static void ConfigureAddress(ModelBuilder b)
        {
            b.Entity<Address>(e =>
            {
                e.ToTable("Addresses");
                e.Property(x => x.AddressLine1).HasMaxLength(200).IsRequired();
                e.Property(x => x.City).HasMaxLength(80).IsRequired();
                e.Property(x => x.PostalCode).HasMaxLength(12).IsRequired();
                e.HasOne(x => x.Province)
                 .WithMany()
                 .HasForeignKey(x => x.ProvinceId)
                 .OnDelete(DeleteBehavior.NoAction);
                e.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}