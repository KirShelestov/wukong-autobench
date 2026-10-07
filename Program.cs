using System.Text;
using WukongAutoBench;

Console.OutputEncoding = Encoding.UTF8;
WinApi.EnableDpiAwareness();

var benchDir = args.Length > 0 ? args[0] : Steam.FindBenchmarkDir();
if (benchDir == null)
{
    Console.WriteLine("Бенчмарк не найден, передайте путь первым аргументом");
    return 1;
}

var iniPath = Path.Combine(benchDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
if (!File.Exists(iniPath))
{
    Console.WriteLine("Нет файла " + iniPath + ", запустите бенчмарк вручную один раз");
    return 1;
}

if (BenchmarkRunner.IsRunning())
{
    Console.WriteLine("Бенчмарк уже запущен, закройте его.");
    return 1;
}

var cfg = new GameConfig(iniPath);
if (cfg.HasBackup)
    cfg.RestoreBackup();

var runner = new BenchmarkRunner(benchDir, TimeSpan.FromMinutes(20));
var results = new List<(Preset, BenchResult?)>();

cfg.MakeBackup();
Console.CancelKeyPress += (_, _) =>
{
    BenchmarkRunner.Kill();
    cfg.RestoreBackup();
};

try
{
    foreach (var preset in new[] { Preset.Cpu(), Preset.Gpu(false) })
    {
        Console.WriteLine($"=== {preset.Name}-тест ===");
        cfg.RestoreBackup();
        cfg.MakeBackup();
        cfg.Load();
        preset.Apply(cfg);
        cfg.Save();

        BenchResult? r = null;
        try
        {
            r = runner.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка: " + e.Message);
        }
        results.Add((preset, r));
    }
}
finally
{
    cfg.RestoreBackup();
}

foreach (var (p, r) in results)
{
    if (r == null)
        Console.WriteLine($"{p.Name}: нет результата");
    else
        Console.WriteLine($"{p.Name}: avg {r.FPSAvg}, 95% {r.FPS95}, 1% low {r.Fps1Low:F1}, min {r.FPSMin}, max {r.FPSMax}, кадров {r.Frames}");
}
return 0;
