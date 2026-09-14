namespace Shop.Reference.Domain;                                          // ← 02

/// <summary>
/// Отдельный агрегат: платёж меняется независимо от заказа.
/// Стратегия наследования — TPH, обоснование в конфигурации.
/// </summary>
public abstract class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }

    public Guid OrderId { get; set; }
}

public class CardPayment : Payment { public string Last4 { get; set; } = null!; }
public class BankTransferPayment : Payment { public string Bic { get; set; } = null!; }
public class CryptoPayment : Payment { public string Wallet { get; set; } = null!; }