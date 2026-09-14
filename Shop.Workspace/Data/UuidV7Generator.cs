using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Shop.Workspace.Data;

/// <summary>
/// Генератор упорядоченных идентификаторов — вариант B из раздела 2.2.
///
/// Ключ известен ДО SaveChanges, поэтому граф связанных объектов
/// собирается и сохраняется одним вызовом. Заодно это делает повторную
/// вставку распознаваемой — см. модуль 07, идемпотентность.
///
/// Альтернатива на стороне базы: .HasDefaultValueSql("uuidv7()")
/// (PostgreSQL 18+).
/// </summary>
public sealed class UuidV7Generator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}