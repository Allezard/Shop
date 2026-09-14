namespace Shop.Reference.Domain;                                          // ← 02

/// <summary>
/// Комплексный тип: значение без идентичности, в трекере места не занимает.
/// Один экземпляр можно присвоить двум владельцам — с owned type так нельзя.
/// </summary>
public readonly record struct Address(string City, string Street, string PostalCode);