using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CreatorSaaS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<VideoJob> VideoJobs { get; set; }
    public DbSet<VideoScene> VideoScenes { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Configure enums as strings
        var enumConverter = new EnumToStringConverter<VideoJobStatus>();
        var enumConverter2 = new EnumToStringConverter<SubscriptionStatus>();
        mb.UsePropertyBuilder<VideoJobStatus>().HasConversion(enumConverter);
        mb.UsePropertyBuilder<SubscriptionStatus>().HasConversion(enumConverter2);

        // ─── Tenant ─────────────────────────────────────────────────────────────

        mb.Entity<Tenant>(t =>
        {
            t.ToTable("Tenants");
            t.HasKey(x => x.Id);
            t.Property(x => x.Name).IsRequired().HasMaxLength(255);
            t.Property(x => x.Slug).IsRequired().HasMaxLength(255);
            t.HasIndex(x => x.Slug).IsUnique();
            t.Property(x => x.Plan).IsRequired().HasMaxLength(50);
            t.Property(x => x.IsActive).HasDefaultValue(true);
            t.Property(x => x.VideosCreatedThisMonth).HasDefaultValue(0);
            t.HasMany(x => x.Users).WithOne(u => u.Tenant).HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Cascade);
            t.HasMany(x => x.Projects).WithOne(p => p.Tenant).HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── User ───────────────────────────────────────────────────────────────

        mb.Entity<User>(u =>
        {
            u.ToTable("Users");
            u.HasKey(x => x.Id);
            u.Property(x => x.Email).IsRequired().HasMaxLength(255);
            u.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            u.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            u.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            u.Property(x => x.Role).IsRequired().HasMaxLength(50);
            u.Property(x => x.PasswordHash).IsRequired();
            u.HasMany(x => x.VideoJobs).WithOne(v => v.CreatedByUser).HasForeignKey(v => v.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        // ─── Project ────────────────────────────────────────────────────────────

        mb.Entity<Project>(p =>
        {
            p.ToTable("Projects");
            p.HasKey(x => x.Id);
            p.Property(x => x.Name).IsRequired().HasMaxLength(255);
            p.Property(x => x.DefaultLanguage).HasMaxLength(10).HasDefaultValue("en");
            p.Property(x => x.DefaultStyle).HasMaxLength(50);
            p.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            p.HasMany(x => x.Channels).WithOne(c => c.Project).HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
            p.HasMany(x => x.VideoJobs).WithOne(v => v.Project).HasForeignKey(v => v.ProjectId).OnDelete(DeleteBehavior.NoAction);
        });

        // ─── Channel ────────────────────────────────────────────────────────────

        mb.Entity<Channel>(c =>
        {
            c.ToTable("Channels");
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).IsRequired().HasMaxLength(255);
            c.Property(x => x.YouTubeChannelId).HasMaxLength(255);
            c.Property(x => x.IsConnected).HasDefaultValue(false);
            c.HasMany(x => x.VideoJobs).WithOne(v => v.Channel).HasForeignKey(v => v.ChannelId).OnDelete(DeleteBehavior.SetNull);
        });

        // ─── VideoJob ───────────────────────────────────────────────────────────

        mb.Entity<VideoJob>(v =>
        {
            v.ToTable("VideoJobs");
            v.HasKey(x => x.Id);
            v.Property(x => x.Topic).IsRequired().HasMaxLength(500);
            v.Property(x => x.Language).IsRequired().HasMaxLength(10);
            v.Property(x => x.Style).IsRequired().HasMaxLength(50);
            v.Property(x => x.Status).IsRequired().HasConversion<string>();
            v.Property(x => x.CurrentStep).HasDefaultValue(0);
            v.Property(x => x.RetryCount).HasDefaultValue(0);
            v.Property(x => x.IsVariant).HasDefaultValue(false);
            v.HasIndex(x => new { x.TenantId, x.Status });
            v.HasIndex(x => new { x.ProjectId, x.Status });
            v.HasIndex(x => x.YouTubeVideoId);
            v.HasMany(x => x.Scenes).WithOne(s => s.VideoJob).HasForeignKey(s => s.VideoJobId).OnDelete(DeleteBehavior.Cascade);
            v.HasMany(x => x.Variants).WithOne(vr => vr.ParentJobId == vr.Id ? null : new VideoJob()).OnDelete(DeleteBehavior.NoAction);
        });

        // ─── VideoScene ─────────────────────────────────────────────────────────

        mb.Entity<VideoScene>(s =>
        {
            s.ToTable("VideoScenes");
            s.HasKey(x => x.Id);
            s.Property(x => x.Title).IsRequired().HasMaxLength(500);
            s.Property(x => x.Narration).IsRequired();
            s.Property(x => x.Order).IsRequired();
            s.Property(x => x.AudioGenerated).HasDefaultValue(false);
            s.Property(x => x.BRollFetched).HasDefaultValue(false);
            s.HasIndex(x => new { x.VideoJobId, x.Order });
        });

        // ─── Subscription ───────────────────────────────────────────────────────

        mb.Entity<Subscription>(s =>
        {
            s.ToTable("Subscriptions");
            s.HasKey(x => x.Id);
            s.Property(x => x.Plan).IsRequired().HasMaxLength(50);
            s.Property(x => x.Status).IsRequired().HasConversion<string>();
            s.HasIndex(x => x.StripeSubscriptionId).IsUnique();
            s.HasIndex(x => x.StripeCustomerId);
        });

        // Global query filters for soft delete
        mb.Entity<Tenant>().HasQueryFilter(x => !x.IsDeleted);
        mb.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
        mb.Entity<Project>().HasQueryFilter(x => !x.IsDeleted);
        mb.Entity<Channel>().HasQueryFilter(x => !x.IsDeleted);
        mb.Entity<VideoJob>().HasQueryFilter(x => !x.IsDeleted);
        mb.Entity<VideoScene>().HasQueryFilter(x => !x.IsDeleted);
    }
}
