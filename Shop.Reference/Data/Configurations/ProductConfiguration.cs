using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Sku).HasMaxLength(20);
        b.Property(p => p.Name).HasMaxLength(200);
        b.Property(p => p.Description).HasMaxLength(4000);
        b.Property(p => p.Price).HasPrecision(18, 2);

        b.HasIndex(p => p.Sku).IsUnique();

        // ВАЖНО: предикат HasFilter ниже совпадает с этим фильтром.
        // При его изменении ВСЕ частичные индексы перестанут применяться —
        // молча, без ошибок. Менять только вместе.
        b.HasQueryFilter(p => !p.IsDeleted);

        // Товары категории с ценой. Частичный — потому что глобальный фильтр
        // добавляет NOT is_deleted в каждый запрос; покрывающий — чтобы
        // получить Index Only Scan и Heap Fetches = 0.
        b.HasIndex(p => p.CategoryId)                            // ← 05
         .HasFilter("NOT is_deleted")
         .IncludeProperties(p => new { p.Name, p.Price });

        // Поиск по подстроке не саргабелен для B-tree в принципе:
        // дерево упорядочено по началу строки. Нужен триграммный.
        b.HasIndex(p => p.Name)                                  // ← 05
         .HasDatabaseName("ix_products_name_trgm")
         .HasMethod("gin")
         .HasOperators("gin_trgm_ops")
         .HasFilter("NOT is_deleted");

        b.HasOne(p => p.Category).WithMany()
         .HasForeignKey(p => p.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}