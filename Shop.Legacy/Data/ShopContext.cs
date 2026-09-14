using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Domain;

namespace Shop.Legacy.Data;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(120);
            e.Property(c => c.Email).HasMaxLength(160);
            e.Property(c => c.Phone).HasMaxLength(32);
            e.HasIndex(c => c.Email).IsUnique();

            e.ComplexProperty(c => c.ShippingAddress, a =>
            {
                a.Property(x => x.City).HasMaxLength(80);
                a.Property(x => x.Street).HasMaxLength(160);
                a.Property(x => x.PostalCode).HasMaxLength(16);
            });
        });

        b.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(120);
            e.HasOne(c => c.Parent).WithMany().HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Tag>(e => e.Property(t => t.Name).HasMaxLength(64));

        b.Entity<Product>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(20);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Price).HasPrecision(18, 2);

            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.CategoryId);

            // Снятые с продажи позиции не должны попадать в выдачу.
            // Фильтр глобальный, чтобы не дублировать условие в каждом запросе.
            e.HasQueryFilter(p => !p.IsDeleted);
        });

        b.Entity<Order>(e =>
        {
            e.Property(o => o.Number).HasMaxLength(16);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(o => o.Number).IsUnique();
            e.HasIndex(o => o.PlacedAt);
            e.HasIndex(o => new { o.Status, o.PlacedAt });

            e.HasOne(o => o.Customer).WithMany(c => c.Orders)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(o => o.Lines).WithOne(l => l.Order)
             .HasForeignKey(l => l.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            // Идентификатор — Guid: заказы приходят из нескольких каналов,
            // и глобально уникальный ключ избавляет от согласования нумерации.
        });

        b.Entity<OrderLine>(e =>
        {
            e.Property(l => l.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(l => l.OrderId);
            e.HasIndex(l => l.ProductId);

            e.HasOne(l => l.Product).WithMany()
             .HasForeignKey(l => l.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.HasIndex(p => p.OrderId);

            // TPH — стратегия по умолчанию. Дискриминатор задан явно ради
            // читаемых значений в SQL: 'card' вместо 'CardPayment'.
            e.HasDiscriminator<string>("payment_type")
             .HasValue<CardPayment>("card")
             .HasValue<BankTransferPayment>("bank_transfer")
             .HasValue<CryptoPayment>("crypto");

            e.HasOne(p => p.Order).WithMany(o => o.Payments)
             .HasForeignKey(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CardPayment>().Property(p => p.Last4).HasMaxLength(4);
        b.Entity<BankTransferPayment>().Property(p => p.Bic).HasMaxLength(11);
        b.Entity<CryptoPayment>().Property(p => p.Wallet).HasMaxLength(64);

        b.Entity<Review>(e =>
        {
            e.Property(r => r.Text).HasMaxLength(2000);
            e.HasIndex(r => r.CreatedAt);
            e.HasIndex(r => r.ProductId);
            e.HasIndex(r => r.Rating);

            e.HasOne(r => r.Product).WithMany()
             .HasForeignKey(r => r.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}