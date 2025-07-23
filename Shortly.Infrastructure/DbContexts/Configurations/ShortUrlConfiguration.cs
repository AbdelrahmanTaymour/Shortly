using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

/// <summary>
/// Provides the entity configuration for the ShortUrl model in the database.
/// </summary>
/// <remarks>
/// This class implements the IEntityTypeConfiguration interface for the ShortUrl entity.
/// It defines configuration rules such as unique constraints, default values, and property behaviors.
/// </remarks>
/// <seealso cref="ShortUrl"/>
public class ShortUrlConfiguration: IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.HasIndex(x => x.ShortCode).IsUnique();
        builder.Property(e => e.AccessCount).HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}