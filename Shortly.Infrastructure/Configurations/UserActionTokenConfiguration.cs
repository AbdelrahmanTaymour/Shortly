using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserActionTokenConfiguration : IEntityTypeConfiguration<UserActionToken>
{
    public void Configure(EntityTypeBuilder<UserActionToken> builder)
    {
        builder.ToTable("user_action_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TokenType).HasConversion<byte>();
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Used).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_user_action_tokens_token_hash");

        builder.HasIndex(x => new { x.UserId, x.TokenType, x.Used })
            .HasDatabaseName("ix_user_action_tokens_user_id_token_type_used");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_user_action_tokens_users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}