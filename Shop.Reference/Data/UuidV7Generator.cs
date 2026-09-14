using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Shop.Reference.Data;                                            // ← каркас

/// <summary>
/// Ключ известен ДО SaveChanges: граф связанных объектов собирается
/// и сохраняется одним вызовом, а повторная вставка становится
/// распознаваемой — нарушение уникальности вместо тихого дубля.
/// </summary>
public sealed class UuidV7Generator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;
    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}