using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;
using Shop.Legacy.Diagnostics;
using Shop.Legacy.Queries;
using Shop.Legacy.Services;

var builder = WebApplication.CreateBuilder(args);

// Контроллеры, а не minimal API — намеренно: скомпилированная лямбда даёт
// в стеках и профилях нечитаемое имя, а метод контроллера — осмысленное.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Диагностическая обвязка ──────────────────────────────────────────────────
// Перехватчики одиночные: их показания читает ручка /api/diag/queries.
builder.Services.AddSingleton<CountingInterceptor>();
builder.Services.AddSingleton<SaveCountingInterceptor>();
builder.Services.AddSingleton<PaymentGateway>();

var latencyMs = int.TryParse(Environment.GetEnvironmentVariable("SHOP_DB_LATENCY_MS"), out var ms)
    ? ms : 0;

builder.Services.AddDbContext<ShopContext>((sp, options) =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Shop"))
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(
            sp.GetRequiredService<CountingInterceptor>(),
            sp.GetRequiredService<SaveCountingInterceptor>());

    // Задержка сети из раздела 1.8. По умолчанию выключена.
    if (latencyMs > 0)
        options.AddInterceptors(new LatencyInterceptor(latencyMs));

    // Прокси: навигации подгружаются по обращению, без явных Include.
    options.UseLazyLoadingProxies();

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddScoped<OrderQueries>();
builder.Services.AddScoped<ProductQueries>();
builder.Services.AddScoped<ReviewQueries>();
builder.Services.AddScoped<ReportQueries>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<CheckoutService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();

if (latencyMs > 0)
    app.Logger.LogWarning("Задержка базы: {Ms} мс на команду (SHOP_DB_LATENCY_MS)", latencyMs);

app.Run();