using Microsoft.EntityFrameworkCore;

namespace Shop.Reference.Tests;

/// <summary>
/// Модель и миграции согласованы — модуль 08, раздел 8.4.
///
/// Проверка через GetPendingMigrations здесь была бы тавтологией: фикстура
/// только что применила все миграции, список неприменённых пуст всегда.
/// Забытую миграцию ловит другое — сравнение текущей модели со снимком
/// последней миграции. Это HasPendingModelChanges (EF Core 8+).
///
/// С EF Core 9 MigrateAsync сам бросает исключение при расхождении, так что
/// фикстура упала бы раньше. Тест всё равно нужен: он называет проблему
/// прямо, а не через падение инфраструктуры всех тестов разом.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class MigrationsTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Model_matches_last_migration()
    {
        await using var ctx = fixture.CreateContext();

        Assert.False(ctx.Database.HasPendingModelChanges(),
            "Модель изменена, а миграция не добавлена: dotnet ef migrations add <Имя>");
    }

    [Fact]
    public async Task Migrations_exist_and_are_applied()
    {
        await using var ctx = fixture.CreateContext();

        Assert.NotEmpty(await ctx.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
    }
}