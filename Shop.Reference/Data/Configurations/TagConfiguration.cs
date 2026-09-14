using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.Property(t => t.Name).HasMaxLength(64);
        b.HasIndex(t => t.Name).IsUnique();

        // Связующая таблица создаётся EF сама. Явная сущность понадобится,
        // если у связи появятся собственные поля — кто и когда поставил тег.
        b.HasMany(t => t.Products).WithMany(p => p.Tags);
    }
}