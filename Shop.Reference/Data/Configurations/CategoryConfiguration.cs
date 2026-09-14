using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.Name).HasMaxLength(120);

        b.HasOne(c => c.Parent).WithMany()
         .HasForeignKey(c => c.ParentId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}