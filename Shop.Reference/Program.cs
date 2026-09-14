using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;
using Shop.Reference.Queries;
using Shop.Reference.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ShopContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Shop"),
                   npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5))
        .UseSnakeCaseNamingConvention();

    // Приложение читает намного больше, чем пишет: отслеживание выключено
    // по умолчанию и включается явным AsTracking там, где нужно.
    // Забытый AsTracking даёт ВИДИМУЮ ошибку — изменения не сохранились.
    // Забытый AsNoTracking не даёт никакой, он просто медленнее.
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();

        // Декартово произведение не доедет до ревью — упадёт у автора.
        options.ConfigureWarnings(w =>
            w.Throw(Microsoft.EntityFrameworkCore.Diagnostics
                .RelationalEventId.MultipleCollectionIncludeWarning));
    }
});

builder.Services.AddScoped<OrderQueries>();
builder.Services.AddScoped<ProductQueries>();
builder.Services.AddScoped<ReviewQueries>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<OutboxPublisher>();
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<IMessageBus, LoggingMessageBus>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.Run();