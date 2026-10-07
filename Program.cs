using System.Text;
using WukongAutoBench;

Console.OutputEncoding = Encoding.UTF8;

var benchDir = args.Length > 0 ? args[0] : Steam.FindBenchmarkDir();
if (benchDir == null)
{
    Console.WriteLine("Бенчмарк не найден, передайте путь первым аргументом");
    return 1;
}

var iniPath = Path.Combine(benchDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
if (!File.Exists(iniPath))
{
    Console.WriteLine("Нет файла " + iniPath);
    return 1;
}

var mode = args.Length > 1 ? args[1] : "cpu";
var preset = mode == "gpu" ? Preset.Gpu(false) : Preset.Cpu();

var cfg = new GameConfig(iniPath);
cfg.MakeBackup();
cfg.Load();
preset.Apply(cfg);
cfg.Save();

foreach (var (k, v) in preset.Describe(cfg))
    Console.WriteLine($"{k}: {v}");
Console.WriteLine("Записано в " + iniPath + ", бэкап рядом");
return 0;
