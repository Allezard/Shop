namespace Shop.Load;

internal static class Help
{
    public static void Print(TextWriter output)
    {
        output.WriteLine("""
            Shop.Load — генератор нагрузки для стенда.
 
            Создаёт устойчивую нагрузку на выбранный сценарий. Без трафика
            приложение ведёт себя нормально — как и в реальности.
 
            Запуск из папки проекта Shop.Load:
              dotnet run -- [сценарий] [параметры]
 
            Сценарии:
            """);

        foreach (var (name, description) in Scenarios.Descriptions)
            output.WriteLine($"  {name,-10} {description}");

        output.WriteLine("""
              check      только проверить, что стенд отвечает
 
            Параметры:
              --duration <с>        длительность, 5–3600 (по умолчанию 60)
              --concurrency <n>     параллельных потоков, 1–128 (по умолчанию 8)
              --delay-ms <мс>       пауза после каждого запроса, 0–5000 (по умолчанию 0)
              --rps <n>             держать заданный темп, 1–10000; несовместим с --delay-ms
              --base-url <адрес>    адрес стенда (по умолчанию http://localhost:5080)
              --json                сводка в JSON (сообщения уходят в stderr)
              -h, --help            эта справка
 
            Примеры:
              dotnet run -- reviews --duration 60 --concurrency 8
              dotnet run -- mixed --rps 50 --concurrency 16
              dotnet run -- check
 
            Коды выхода: 0 — успех, 1 — стенд недоступен, 2 — неверные параметры, 130 — прервано Ctrl+C.
            """);
    }
}