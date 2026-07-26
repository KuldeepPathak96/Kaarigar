using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<EmployerProfile> EmployerProfiles { get; set; }
    public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
    public DbSet<EmployeeSkill> EmployeeSkills { get; set; }
    public DbSet<JobPost> JobPosts { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<OtpRecord> OtpRecords { get; set; }
    public DbSet<MobileVerificationOtp> MobileVerificationOtps { get; set; }
    public DbSet<BusinessCategory> BusinessCategories { get; set; }
    public DbSet<HourlyRateOption> HourlyRateOptions { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<JobSkill> JobSkills { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Area> Areas { get; set; }
    public DbSet<KaarigarRating> KaarigarRatings { get; set; }
    public DbSet<LearningVideo> LearningVideos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USER_ACCOUNT constraints
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasIndex(u => u.ContactNbr).IsUnique();
            e.HasIndex(u => u.EmailId).IsUnique().HasFilter("[EMAIL_ID] IS NOT NULL");
            e.HasIndex(u => u.RoleCd);
            e.Property(u => u.RoleCd)
             .HasConversion<string>()
             .HasMaxLength(20);
        });

        // EMPLOYER_PROFILE constraints — FK_EMPLOYER_PROFILE_USER_ACCOUNT is ON DELETE CASCADE
        modelBuilder.Entity<EmployerProfile>(e =>
        {
            e.HasIndex(ep => ep.UserAccountId).IsUnique();
            e.HasOne(ep => ep.UserAccount)
             .WithMany()
             .HasForeignKey(ep => ep.UserAccountId)
             .OnDelete(DeleteBehavior.Cascade);

            // BUSINESS_CATEGORY has no ON DELETE CASCADE in the schema — Restrict.
            e.HasIndex(ep => ep.BusinessCategoryId);
            e.HasOne(ep => ep.BusinessCategory)
             .WithMany()
             .HasForeignKey(ep => ep.BusinessCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BusinessCategory>(e =>
        {
            e.HasIndex(c => c.CategoryName).IsUnique();
        });

        // EMPLOYEE_PROFILE constraints — FK_EMPLOYEE_PROFILE_USER_ACCOUNT is ON DELETE CASCADE
        modelBuilder.Entity<EmployeeProfile>(e =>
        {
            e.HasIndex(ep => ep.UserAccountId).IsUnique();
            e.HasOne(ep => ep.UserAccount)
             .WithMany()
             .HasForeignKey(ep => ep.UserAccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // EMPLOYEE_SKILL — FK_EMPLOYEE_SKILL_USER_ACCOUNT is ON DELETE CASCADE
        modelBuilder.Entity<EmployeeSkill>(e =>
        {
            e.HasIndex(es => new { es.UserAccountId, es.SkillId }).IsUnique();
        });

        // JOB_POST — FK_JOB_POST_EMPLOYER has NO "ON DELETE CASCADE" in the
        // script (defaults to NO ACTION), so map it as Restrict.
        modelBuilder.Entity<JobPost>(e =>
        {
            e.HasOne(jp => jp.EmployerUserAccount)
             .WithMany()
             .HasForeignKey(jp => jp.EmployerUserAccountId)
             .OnDelete(DeleteBehavior.Restrict); // matches FK_JOB_POST_EMPLOYER (no cascade specified)

            e.Property(jp => jp.LatitudeNbr).HasPrecision(9, 6);
            e.Property(jp => jp.LongitudeNbr).HasPrecision(9, 6);
            e.Property(jp => jp.DailyWageAmt).HasPrecision(10, 2);
            e.Property(jp => jp.HourlyWageAmt).HasPrecision(10, 2);

            e.HasIndex(jp => jp.EmployerUserAccountId);
            e.HasIndex(jp => jp.StatusCd);
        });


        // JOB_APPLICATION — FK_JOB_APPLICATION_JOB_POST and
        // FK_JOB_APPLICATION_EMPLOYEE both have NO "ON DELETE CASCADE"
        // (defaults to NO ACTION) ? Restrict.
        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasIndex(a => a.EmployeeUserAccountId);
            e.HasIndex(a => a.JobPostId);
            e.HasIndex(a => new { a.JobPostId, a.EmployeeUserAccountId }).IsUnique();

            e.HasOne(a => a.JobPost)
             .WithMany()
             .HasForeignKey(a => a.JobPostId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.EmployeeUserAccount)
             .WithMany()
             .HasForeignKey(a => a.EmployeeUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // NOTIFICATION_LOG — all FKs default to NO ACTION ? Restrict.
        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasIndex(n => n.EmployeeUserAccountId);
            e.HasIndex(n => n.EmployerUserAccountId);

            e.HasOne(n => n.EmployeeUserAccount)
             .WithMany()
             .HasForeignKey(n => n.EmployeeUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(n => n.EmployerUserAccount)
             .WithMany()
             .HasForeignKey(n => n.EmployerUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(n => n.JobPost)
             .WithMany()
             .HasForeignKey(n => n.JobPostId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // OTP_RECORD — both FKs default to NO ACTION ? Restrict.
        modelBuilder.Entity<OtpRecord>(e =>
        {
            e.HasIndex(o => o.GeneratedTs);
            e.HasOne(o => o.EmployeeUserAccount)
             .WithMany()
             .HasForeignKey(o => o.EmployeeUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.JobPost)
             .WithMany()
             .HasForeignKey(o => o.JobPostId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.EmployerUserAccount)
             .WithMany()
             .HasForeignKey(o => o.EmployerUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // MOBILE_VERIFICATION_OTP  used for REGISTRATION and FORGOT_PASSWORD OTPs.
        // No FK to USER_ACCOUNT in the schema (keyed by CONTACT_NBR instead), so
        // just index the lookup columns.
        modelBuilder.Entity<MobileVerificationOtp>(e =>
        {
            e.HasIndex(o => o.ContactNbr);
            e.HasIndex(o => new { o.ContactNbr, o.PurposeCd, o.IsUsedFl });
        });
        modelBuilder.Entity<Skill>(e =>
        {
            e.HasIndex(s => s.SkillName).IsUnique();
        });

        modelBuilder.Entity<JobSkill>(e =>
        {
            e.HasIndex(js => new { js.JobPostId, js.SkillId }).IsUnique();

            e.HasOne(js => js.JobPost)
             .WithMany(jp => jp.JobSkills)
             .HasForeignKey(js => js.JobPostId)
             .OnDelete(DeleteBehavior.Cascade);   // matches FK_JOB_SKILL_JOB_POST ON DELETE CASCADE

            e.HasOne(js => js.Skill)
             .WithMany()
             .HasForeignKey(js => js.SkillId)
             .OnDelete(DeleteBehavior.Restrict);  // matches FK_JOB_SKILL_SKILL (no cascade)
        });

        // EMPLOYEE_DOCUMENT â€” FK_EMPLOYEE_DOCUMENT_USER_ACCOUNT is ON DELETE
        // CASCADE; FK_EMPLOYEE_DOCUMENT_REVIEWED_BY has no cascade specified.
        modelBuilder.Entity<EmployeeDocument>(e =>
        {
            e.HasIndex(d => d.UserAccountId);
            e.HasIndex(d => d.ReviewStatusCd);
            e.HasIndex(d => new { d.UserAccountId, d.DocumentTypeCd }).IsUnique();

            e.HasOne(d => d.UserAccount)
             .WithMany()
             .HasForeignKey(d => d.UserAccountId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.ReviewedByUserAccount)
             .WithMany()
             .HasForeignKey(d => d.ReviewedByUserAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<City>(e =>
        {
            e.HasIndex(c => c.CityName).IsUnique();
        });

        modelBuilder.Entity<Area>(e =>
        {
            e.HasIndex(a => a.CityId);
            e.HasIndex(a => new { a.CityId, a.AreaName }).IsUnique();

            e.HasOne(a => a.City)
             .WithMany()
             .HasForeignKey(a => a.CityId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HourlyRateOption>(e =>
        {
            e.Property(r => r.HourlyRateAmt).HasPrecision(10, 2);
        });

    }
}