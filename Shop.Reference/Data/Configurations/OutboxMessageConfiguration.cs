using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 07

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.Property(m => m.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();
        b.Property(m => m.Type).HasMaxLength(120);

        // Частичный индекс: отправленные в него не попадают, поэтому он
        // остаётся маленьким независимо от размера архива.
        b.HasIndex(m => m.Id)
         .HasDatabaseName("ix_outbox_pending")
         .HasFilter("sent_at IS NULL");
    }
}