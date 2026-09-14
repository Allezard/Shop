using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Shop.Reference.Data;
using Testcontainers.PostgreSql;

namespace Shop.Reference.Tests;

/// <summary>
/// Настоящая база в контейнере — ровно то, что описывает модуль 08, раздел
/// 8.2. Схема создаётся теми же миграциями, что поедут в продакшен, а не
/// <c>EnsureCreated()</c>: иначе тесты проверяют схему, которой не будет,
/// и заодно не проверяют сами миграции.
///
/// Контейнер поднимается ОДИН РАЗ на весь прогон — через
/// <see cref="DatabaseCollection"/> ниже, а не по экземпляру на класс.
/// Секунды на старте того стоят: разница между «один раз» и «на каждый класс»
/// на сотне тестов ощутима.
///
/// Изоляция — очисткой (Respawn), не откатом транзакции. Причина —
/// <see cref="Services.CheckoutService"/>: он сам открывает транзакции
/// (<c>BeginTransactionAsync</c> внутри <c>CreateExecutionStrategy</c>), а
/// вложенных транзакций в PostgreSQL нет. С откатом такой код либо упадёт,
/// либо проверит не то. См. модуль 08, раздел 8.3 — «начинайте с очистки».
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    // Образ передаётся в конструктор, не через .WithImage(...): начиная
    // с Testcontainers v4 конструктор без параметров объявлен устаревшим
    // у всех билдеров, не только у PostgreSqlBuilder.
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18-alpine")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _db.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();

        // Расширение pg_trgm создаёт сама миграция: оно объявлено в модели
        // через HasPostgresExtension. Создавать его здесь руками не нужно.
        await using (var ctx = CreateContext())
        {
            await ctx.Database.MigrateAsync();

            // Если в проекте нет ни одной миграции, MigrateAsync молча ничего
            // не делает, и дальше Respawn падает с невнятным «No tables found».
            // Лучше упасть здесь — с сообщением, которое говорит, что делать.
            var applied = await ctx.Database.GetAppliedMigrationsAsync();
            if (!applied.Any())
                throw new InvalidOperationException(
                    "В Shop.Reference нет ни одной миграции — схема не создана. " +
                    "Выполните: dotnet ef migrations add InitialCreate --project Shop.Reference");
        }

        await using var setup = new NpgsqlConnection(ConnectionString);
        await setup.OpenAsync();

        // __EFMigrationsHistory исключена намеренно: очистив её, мы бы
        // заставили EF применять миграции заново на уже существующей схеме,
        // и следующий тест упал бы на попытке создать существующую таблицу.
        _respawner = await Respawner.CreateAsync(setup, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    public ShopContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<ShopContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention();

        if (interceptors.Length > 0)
            builder.AddInterceptors(interceptors);

        return new ShopContext(builder.Options);
    }

    public async ValueTask ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}