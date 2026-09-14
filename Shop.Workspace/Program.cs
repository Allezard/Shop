using Microsoft.EntityFrameworkCore;
using Shop.Workspace.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ShopContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Shop"))
    .UseSnakeCaseNamingConvention());

// Пакета Microsoft.EntityFrameworkCore.Proxies здесь нет намеренно.
// Почему — разбирается в модуле 03.

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.Run();