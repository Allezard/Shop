using Microsoft.EntityFrameworkCore;
using Shop.Reference.Domain;

namespace Shop.Reference.Data;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();        // ← 02
    public DbSet<Order> Orders => Set<Order>();                 // ← 02
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();     // ← 02
    public DbSet<Product> Products => Set<Product>();           // ← 02
    public DbSet<Category> Categories => Set<Category>();       // ← 02
    public DbSet<Tag> Tags => Set<Tag>();                       // ← 02
    public DbSet<Payment> Payments => Set<Payment>();           // ← 02
    public DbSet<Review> Reviews => Set<Review>();              // ← 02
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>(); // ← 07

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Триграммный индекс в ProductConfiguration требует расширения.
        // Объявленное в модели, оно попадает в миграцию и создаётся везде,
        // где миграции применяются: в базе разработчика, в тестах, в проде.
        b.HasPostgresExtension("pg_trgm");                      // ← 05

        // Одна строка — и все файлы из Data/Configurations подхватываются сами.
        b.ApplyConfigurationsFromAssembly(typeof(ShopContext).Assembly);
    }
}