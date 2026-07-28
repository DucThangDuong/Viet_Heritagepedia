using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public partial class VietHeritagePediaContext : DbContext
{
    public VietHeritagePediaContext()
    {
    }

    public VietHeritagePediaContext(DbContextOptions<VietHeritagePediaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Contribution> Contributions { get; set; }

    public virtual DbSet<ContributionLike> ContributionLikes { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthProvider> UserAuthProviders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Contribu__3214EC07358FBF15");

            entity.HasIndex(e => new { e.LocationId, e.ContributionType, e.WorkflowState }, "IX_Contributions_Location_Type_State");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ContributionType).HasDefaultValue(2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NoSqlDocumentId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SourceDocumentUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Author).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contributions_Users");

            entity.HasOne(d => d.Location).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contributions_Locations");
        });

        modelBuilder.Entity<ContributionLike>(entity =>
        {
            entity.HasKey(e => new { e.ContributionId, e.UserId }).HasName("PK__Contribu__BFA2AD004A811F4F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Contribution).WithMany(p => p.ContributionLikes)
                .HasForeignKey(d => d.ContributionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContributionLikes_Contributions");

            entity.HasOne(d => d.User).WithMany(p => p.ContributionLikes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContributionLikes_Users");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Location__3214EC077C675152");

            entity.HasIndex(e => new { e.Region, e.Province, e.IsPlainRegion, e.Category, e.IsActive }, "IX_Locations_Filters");

            entity.HasIndex(e => e.Slug, "IX_Locations_Slug");

            entity.HasIndex(e => e.Slug, "UQ__Location__BC7B5FB6A0106D22").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CoverImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .HasDefaultValue("Thừa Thiên Huế");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .HasDefaultValue("Trung Bộ");
            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.VietnameseName).HasMaxLength(200);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OutboxMe__3214EC070003D431");

            entity.HasIndex(e => e.ProcessedAt, "IX_OutboxMessages_ProcessedAt").HasFilter("([ProcessedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MessageType)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07DC1309AE");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105347D54019D").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Role).HasMaxLength(100).HasDefaultValue("Thành viên");
        });

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAuth__3214EC0709AC3F46");

            entity.HasIndex(e => e.UserId, "IX_UserAuthProviders_UserId");
            entity.HasIndex(e => new { e.ProviderName, e.ProviderKey }, "UQ_Provider_ProviderKey").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.ProviderKey).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.ProviderName).HasMaxLength(50).IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.UserAuthProviders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserAuthProviders_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
