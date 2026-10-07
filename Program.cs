using System.Text;
using WukongAutoBench;

Console.OutputEncoding = Encoding.UTF8;
WinApi.EnableDpiAwareness();

string? benchDir = null;
string? only = null;
bool? rtOption = null;
int timeoutMin = 20;
string outRoot = Path.Combine(AppContext.BaseDirectory, "results");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dir":
            benchDir = args[++i];
            break;
        case "--only":
            only = args[++i].ToLowerInvariant();
            break;
        case "--rt":
            rtOption = true;
            break;
        case "--no-rt":
            rtOption = false;
            break;
        case "--timeout":
            timeoutMin = int.Parse(args[++i]);
            break;
        case "--out":
            outRoot = args[++i];
            break;
        case "-h":
        case "--help":
            PrintHelp();
            return 0;
        default:
            Console.WriteLine("Неизвестный аргумент: " + args[i]);
            PrintHelp();
            return 1;
    }
}

benchDir ??= Steam.FindBenchmarkDir();
if (benchDir == null || !Directory.Exists(benchDir))
{
    Console.WriteLine("Не нашёл Black Myth: Wukong Benchmark Tool. Укажите папку: --dir \"D:\\SteamLibrary\\steamapps\\common\\Black Myth Wukong Benchmark Tool\"");
    return 1;
}

var iniPath = Path.Combine(benchDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
if (!File.Exists(iniPath))
{
    Console.WriteLine("Нет файла настроек " + iniPath);
    Console.WriteLine("Запустите бенчмарк вручную один раз (он создаст конфиг и скомпилирует шейдеры), потом закройте.");
    return 1;
}

if (BenchmarkRunner.IsRunning())
{
    Console.WriteLine("Бенчмарк уже запущен, закройте его.");
    return 1;
}

Console.WriteLine("Бенчмарк: " + benchDir);

var cfg = new GameConfig(iniPath);
if (cfg.HasBackup)
{
    Console.WriteLine("Остался бэкап конфига с прошлого запуска, восстанавливаю.");
    cfg.RestoreBackup();
}

Console.WriteLine("Собираю информацию о системе...");
var sys = SystemInfo.Collect();
foreach (var (k, v) in sys.Items)
    Console.WriteLine($"  {k}: {v}");
Console.WriteLine();

bool rt = rtOption ?? SupportsRayTracing(sys.Get("Видеокарта"));

var presets = new List<Preset>();
if (only is null or "cpu")
    presets.Add(Preset.Cpu());
if (only is null or "gpu")
    presets.Add(Preset.Gpu(rt));

var outDir = Path.Combine(outRoot, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
Directory.CreateDirectory(outDir);

var runner = new BenchmarkRunner(benchDir, TimeSpan.FromMinutes(timeoutMin));
var passes = new List<PassResult>();

cfg.MakeBackup();
Console.CancelKeyPress += (_, _) =>
{
    BenchmarkRunner.Kill();
    cfg.RestoreBackup();
};

Console.WriteLine("Не трогайте мышь и клавиатуру, пока идут тесты (около 5 минут на проход).");
Console.WriteLine();

try
{
    foreach (var preset in presets)
    {
        var pass = RunPass(preset);

        if (pass.Result == null && preset.RayTracing)
        {
            Console.WriteLine("  Не получилось с трассировкой лучей, повторяю без неё");
            preset.RayTracing = false;
            pass = RunPass(preset);
        }

        passes.Add(pass);
        if (pass.Result != null)
        {
            File.Copy(pass.Result.RawPath, Path.Combine(outDir, preset.Name.ToLower() + "_raw.json"), true);
            Console.WriteLine($"  {preset.Name}: средний FPS {pass.Result.FPSAvg}");
        }
        Console.WriteLine();
    }
}
finally
{
    cfg.RestoreBackup();
    Console.WriteLine("Исходные настройки бенчмарка восстановлены.");
}

var report = Report.Build(sys, passes);
File.WriteAllText(Path.Combine(outDir, "report.md"), report, Encoding.UTF8);

Console.WriteLine();
Console.WriteLine(report);
Console.WriteLine("Отчёт сохранён: " + Path.Combine(outDir, "report.md"));

return passes.All(p => p.Result != null) ? 0 : 2;

PassResult RunPass(Preset preset)
{
    Console.WriteLine($"=== {preset.Name}-тест ===");

    cfg.RestoreBackup();
    cfg.MakeBackup();
    cfg.Load();
    preset.Apply(cfg);
    cfg.Save();

    var settings = preset.Describe(cfg).ToList();
    foreach (var (k, v) in settings)
        Console.WriteLine($"  {k}: {v}");

    try
    {
        var result = runner.Run();
        return new PassResult(preset, settings, result, result == null ? "бенчмарк закрылся без результата" : null);
    }
    catch (Exception e)
    {
        Console.WriteLine("  Ошибка: " + e.Message);
        return new PassResult(preset, settings, null, e.Message);
    }
}

static bool SupportsRayTracing(string gpuName) =>
    gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) &&
    gpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase);

static void PrintHelp()
{
    Console.WriteLine("""
        WukongAutoBench - автоматический прогон Black Myth: Wukong Benchmark Tool (CPU и GPU тест)

        Использование: WukongAutoBench.exe [параметры]

          --dir <путь>      папка бенчмарка (по умолчанию ищется через Steam)
          --only cpu|gpu    запустить только один тест
          --rt / --no-rt    включить/выключить трассировку лучей в GPU-тесте
                            (по умолчанию включается только на NVIDIA RTX)
          --timeout <мин>   таймаут на один проход, по умолчанию 20
          --out <путь>      куда сохранять результаты (по умолчанию results рядом с exe)
        """);
}
