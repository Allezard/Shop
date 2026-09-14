using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        // UUID v7: упорядочен по времени, плотность страниц индекса ~96%
        // против ~69% у случайного v4.
        b.Property(o => o.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();

        b.Property(o => o.Number).HasMaxLength(16);
        b.Property(o => o.Total).HasPrecision(18, 2);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.Notes).HasMaxLength(4000);

        b.HasIndex(o => o.Number).IsUnique();

        // Заказы покупателя, свежие сверху: равенство → сортировка.
        // Порядок колонок обратный дал бы узел Sort в плане.
        b.HasIndex(o => new { o.CustomerId, o.PlacedAt });       // ← 05

        // Редкий статус: частичный индекс вместо бесполезного полного —
        // завершённых заказов 82%, индекс по статусу планировщик не выберет.
        b.HasIndex(o => o.PlacedAt)                              // ← 05
         .HasDatabaseName("ix_orders_awaiting_payment")
         .HasFilter("status = 'AwaitingPayment'");

        // Маркер конкурентности бесплатен: системная колонка PostgreSQL.
        // Нужен здесь, потому что статус заказа могут менять два оператора.
        b.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();  // ← 04

        // Между агрегатами — Restrict: заказ без покупателя есть
        // исторический факт, стирать его нельзя.
        b.HasOne(o => o.Customer).WithMany(c => c.Orders)
         .HasForeignKey(o => o.CustomerId)
         .OnDelete(DeleteBehavior.Restrict);

        // Внутри агрегата — Cascade: строки без заказа не существуют.
        b.HasMany(o => o.Lines).WithOne(l => l.Order)
         .HasForeignKey(l => l.OrderId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}