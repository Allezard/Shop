using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.Property(l => l.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();
        b.Property(l => l.UnitPrice).HasPrecision(18, 2);

        b.HasOne(l => l.Product).WithMany()
         .HasForeignKey(l => l.ProductId)
         .OnDelete(DeleteBehavior.Restrict);

        // Индекса по order_id здесь нет: EF создаёт его сам под внешний ключ.
        // Индекса по product_id нет намеренно — по товару строки не ищут,
        // а лишний индекс замедляет запись. Проверено idx_scan = 0.
    }
}