namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InstanceSetting : IEntityTypeConfiguration<InstanceSetting>
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Configure(EntityTypeBuilder<InstanceSetting> builder)
    {
        builder.Property(s => s.Key).HasMaxLength(200).UseCollation("NOCASE");
        builder.HasIndex(s => s.Key).IsUnique();
    }
}
