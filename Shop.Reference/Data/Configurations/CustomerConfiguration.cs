using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.Property(c => c.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();

        b.Property(c => c.Name).HasMaxLength(120);
        b.Property(c => c.Email).HasMaxLength(160);
        b.Property(c => c.PhoneNumber).HasMaxLength(32);         // ← 06

        b.HasIndex(c => c.Email).IsUnique();

        // Комплексный тип, а не owned: адрес — значение, идентичности у него нет.
        b.ComplexProperty(c => c.ShippingAddress, a =>
        {
            a.Property(x => x.City).HasMaxLength(80);
            a.Property(x => x.Street).HasMaxLength(160);
            a.Property(x => x.PostalCode).HasMaxLength(16);
        });
    }
}