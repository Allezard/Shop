using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Reference.Domain;

namespace Shop.Reference.Data.Configurations;                             // ← 02

/// <summary>
/// TPH выбран по двум числам: платежи читаются вместе с заказом (значит TPT
/// дал бы три LEFT JOIN на каждое чтение), а потомки добавляют по одному полю
/// (значит таблица не разрастётся). Распределение перекошено — 85% карты, —
/// и в TPT две таблицы были бы почти пустыми при тех же соединениях.
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(p => p.Id).HasValueGenerator<UuidV7Generator>().ValueGeneratedOnAdd();
        b.Property(p => p.Amount).HasPrecision(18, 2);

        b.HasIndex(p => p.OrderId);

        b.HasDiscriminator<string>("payment_type")
         .HasValue<CardPayment>("card")
         .HasValue<BankTransferPayment>("bank_transfer")
         .HasValue<CryptoPayment>("crypto");

        // Цена TPH — потеря NOT NULL на полях потомков: в общей таблице
        // last4 обязан допускать NULL, потому что у крипты его нет.
        // Возвращаем гарантию проверочными ограничениями.
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_payment_card",
                "payment_type <> 'card' OR last4 IS NOT NULL");
            t.HasCheckConstraint("ck_payment_bank",
                "payment_type <> 'bank_transfer' OR bic IS NOT NULL");
            t.HasCheckConstraint("ck_payment_crypto",
                "payment_type <> 'crypto' OR wallet IS NOT NULL");
        });
    }
}

public sealed class CardPaymentConfiguration : IEntityTypeConfiguration<CardPayment>
{
    public void Configure(EntityTypeBuilder<CardPayment> b) =>
        b.Property(p => p.Last4).HasMaxLength(4);
}

public sealed class BankTransferPaymentConfiguration : IEntityTypeConfiguration<BankTransferPayment>
{
    public void Configure(EntityTypeBuilder<BankTransferPayment> b) =>
        b.Property(p => p.Bic).HasMaxLength(11);
}

public sealed class CryptoPaymentConfiguration : IEntityTypeConfiguration<CryptoPayment>
{
    public void Configure(EntityTypeBuilder<CryptoPayment> b) =>
        b.Property(p => p.Wallet).HasMaxLength(64);
}