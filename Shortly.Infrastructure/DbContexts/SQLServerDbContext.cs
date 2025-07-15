using Microsoft.EntityFrameworkCore;
using Shortly.Core.Entities;
using Shortly.Infrastructure.DbContexts.Configurations;

namespace Shortly.Infrastructure.DbContexts;

/// <summary>
/// Represents the Entity Framework Core DbContext for managing Shortly application database operations.
/// Provides DbSet properties for entity mapping and implements custom model configurations.
/// </summary>
public class SQLServerDbContext: DbContext
{
    public SQLServerDbContext(DbContextOptions<SQLServerDbContext> dbContextOptions) : base(dbContextOptions)
    {
    }
    
    public DbSet<ShortUrl> ShortUrls { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>
    /// Configures the model for the ShortlyDbContext by applying entity configurations and other custom settings.
    /// </summary>
    /// <param name="modelBuilder">An instance of <see cref="ModelBuilder"/> used to configure the model and its entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new ShortUrlConfiguration());

        
    }
}