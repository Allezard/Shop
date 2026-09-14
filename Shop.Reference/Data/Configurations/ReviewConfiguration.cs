using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.Property(r => r.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();
        b.Property(r => r.Text).HasMaxLength(2000);

        b.HasIndex(r => r.ProductId);

        // Индекса по created_at НЕТ намеренно. Лента листается keyset-ом
        // по первичному ключу: UUID v7 уже упорядочен по времени создания,
        // и отдельный индекс был бы дублированием.
        //
        // Отзывы — самая пишущая таблица, и у неё ровно два индекса.
        // Это прямое следствие выбора ключа в модуле 02.
    }
}