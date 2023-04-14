using LogChecker;
using LogChecker.ConsoleParametersParser;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

string directoryPath = "";

var parameters = new ConsoleParametersBuilder(args)
    .AddDirectoryPath()
    .AddIgnoreTimeoutException()
    .Build();

if (!string.IsNullOrEmpty(parameters.DirectoryPath))
{
    directoryPath = parameters.DirectoryPath;
}
else
{
    Console.WriteLine("Введите путь к папке, в которой лежат логи");
    directoryPath = Console.ReadLine();
}

var encoderSettings = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = true
};

Dictionary<string, LogElementsCounter> exceptions = new();

if (Directory.Exists(directoryPath))
{
    var files = Directory.GetFiles(directoryPath);
    foreach (var item in files)
    {
        foreach (var line in await File.ReadAllLinesAsync(item, Encoding.UTF8))
        {
            try
            {
                var log = JsonSerializer.Deserialize<LogElement>(line, encoderSettings);
                if (log != null && log?.Exception != null)
                {
                    if (!parameters.IsIgnoreTimeoutException || (parameters.IsIgnoreTimeoutException && !log.Exception.Contains("System.TimeoutException")))
                    {
                        if (!exceptions.TryGetValue(log?.Exception!, out var elem))
                        {
                            exceptions.Add(log?.Exception!, new LogElementsCounter { LogElement = log! });
                        }
                        else
                        {
                            elem.Count += 1;
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }

    foreach (var item in exceptions.Where(x => x.Value.Count >= 10).GroupBy(x => x.Value.LogElement.GetApplicationName()))
    {
        Console.WriteLine(item.Key);
        foreach (var log in item.OrderByDescending(x => x.Value.Count))
        {
            Console.WriteLine($"Количество: {log.Value.Count}\n");
            var text = JsonSerializer.Serialize(log.Value.LogElement, encoderSettings);
            Console.WriteLine(text);
            Console.WriteLine(Environment.NewLine);
        }
        Console.WriteLine(new string('-', 120));
    }
}
else
{
    Console.WriteLine("Путь к папке указан неверно");
}
