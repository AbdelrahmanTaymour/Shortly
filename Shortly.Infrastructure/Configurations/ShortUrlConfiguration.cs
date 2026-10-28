using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.ToTable("short_urls");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.OriginalUrl)
            .HasConversion(url => url.Value, value => OriginalUrl.Create(value).Value)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.ShortCode)
            .HasConversion(code => code.Value, value => ShortCode.Create(value).Value)
            .IsRequired()
            .HasMaxLength(ShortCode.MaxLength)
            .IsUnicode(false);

        builder.Property(s => s.OwnerType).HasConversion<byte>();
        builder.Property(s => s.AnonymousSessionId).HasMaxLength(128).IsUnicode(false);
        builder.Property(s => s.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(s => s.PasswordHash).HasMaxLength(256).IsUnicode(false);
        builder.Property(s => s.Title).HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.ShortCode)
            .IsUnique()
            .HasDatabaseName("ux_short_urls_short_code");

        builder.HasIndex(s => new { s.UserId, s.CreatedAtUtc })
            .HasDatabaseName("ix_short_urls_user_id_created_at")
            .IsDescending(false, true)
            .HasFilter("user_id IS NOT NULL")
            .IncludeProperties(s => new { s.ShortCode, s.OriginalUrl, s.TotalClicks, s.IsActive });

        builder.HasIndex(s => new { s.OrganizationId, s.CreatedAtUtc })
            .HasDatabaseName("ix_short_urls_organization_id_created_at")
            .IsDescending(false, true)
            .HasFilter("organization_id IS NOT NULL")
            .IncludeProperties(s => new { s.ShortCode, s.OriginalUrl, s.TotalClicks, s.IsActive });

        builder.HasIndex(s => s.CreatedByMemberId)
            .HasDatabaseName("ix_short_urls_created_by_member_id")
            .HasFilter("created_by_member_id IS NOT NULL");

        builder.HasIndex(s => s.AnonymousSessionId)
            .HasDatabaseName("ix_short_urls_anonymous_session_id")
            .HasFilter("anonymous_session_id IS NOT NULL");

        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("ix_short_urls_expires_at")
            .HasFilter("expires_at IS NOT NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("fk_short_urls_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .HasConstraintName("fk_short_urls_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(s => s.CreatedByMemberId)
            .HasConstraintName("fk_short_urls_organization_members")
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_short_urls_single_owner",
            """
            (owner_type = 1 AND user_id IS NOT NULL AND organization_id IS NULL AND created_by_member_id IS NULL AND anonymous_session_id IS NULL)
            OR (owner_type = 2 AND user_id IS NULL AND organization_id IS NOT NULL AND created_by_member_id IS NOT NULL AND anonymous_session_id IS NULL)
            OR (owner_type = 3 AND user_id IS NULL AND organization_id IS NULL AND created_by_member_id IS NULL AND anonymous_session_id IS NOT NULL)
            """));
    }
}