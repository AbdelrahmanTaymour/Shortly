using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profile");

        builder.HasKey(up => up.UserId);

        builder.Property(up => up.Name).HasMaxLength(100);
        builder.Property(up => up.Bio).HasMaxLength(500);
        builder.Property(up => up.PhoneNumber).HasMaxLength(20);
        builder.Property(up => up.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(up => up.Website).HasMaxLength(500);
        builder.Property(up => up.Company).HasMaxLength(100);
        builder.Property(up => up.Location).HasMaxLength(100);
        builder.Property(up => up.Country).HasMaxLength(50);
        builder.Property(up => up.TimeZone).HasMaxLength(50);
        builder.Property(up => up.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}