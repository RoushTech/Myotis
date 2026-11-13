namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PersonalAccessToken : IEntityTypeConfiguration<PersonalAccessToken>
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public required byte[] TokenHash { get; set; }

    // The start of the token, shown so people can tell their tokens apart.
    public required string Prefix { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public void Configure(EntityTypeBuilder<PersonalAccessToken> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.Prefix).HasMaxLength(40);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
