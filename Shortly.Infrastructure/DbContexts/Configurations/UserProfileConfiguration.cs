using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Primary Key
        builder.HasKey(up => up.UserId);
        
        // Properties configuration
        builder.Property(u => u.Name).HasMaxLength(100);
        builder.Property(up => up.Bio).HasMaxLength(500);
        builder.Property(up => up.PhoneNumber).HasMaxLength(20);
        builder.Property(up => up.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(up => up.Website).HasMaxLength(500);
        builder.Property(up => up.Company).HasMaxLength(100);
        builder.Property(up => up.Location).HasMaxLength(100);
        builder.Property(up => up.Country).HasMaxLength(50);
        builder.Property(up => up.TimeZone).HasMaxLength(50);
        builder.Property(up => up.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}