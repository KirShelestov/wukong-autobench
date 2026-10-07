using System.Diagnostics;

namespace WukongAutoBench;

class BenchmarkRunner
{
    private static readonly string[] ProcessNames = { "b1-Win64-Shipping", "b1", "b1_benchmark" };

    private const double StartBtnX = 0.11, StartBtnY = 0.45;
    private const double ConfirmBtnX = 0.39, ConfirmBtnY = 0.58;

    private readonly string _benchDir;
    private readonly string _historyDir;
    private readonly TimeSpan _timeout;

    public BenchmarkRunner(string benchDir, TimeSpan timeout)
    {
        _benchDir = benchDir;
        _timeout = timeout;
        _historyDir = Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");
    }

    public static bool IsRunning() => FindProcesses().Any();

    public BenchResult? Run()
    {
        Directory.CreateDirectory(_historyDir);
        var oldFiles = Directory.GetFiles(_historyDir).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Start();

        var sw = Stopwatch.StartNew();
        bool seenProcess = false;
        int step = 0;

        try
        {
            while (sw.Elapsed < _timeout)
            {
                var result = FindNewResult(oldFiles);
                if (result != null)
                {
                    Log($"результат получен за {sw.Elapsed:mm\\:ss}");
                    return result;
                }

                var procs = FindProcesses();
                if (procs.Count > 0)
                {
                    seenProcess = true;
                }
                else if (seenProcess)
                {
                    Thread.Sleep(2000);
                    return FindNewResult(oldFiles);
                }
                else if (sw.Elapsed > TimeSpan.FromMinutes(3))
                {
                    throw new Exception("бенчмарк так и не запустился за 3 минуты");
                }

                var hwnd = procs.Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);
                if (hwnd != IntPtr.Zero)
                {
                    if (!WinApi.IsForeground(hwnd))
                        WinApi.Activate(hwnd);

                    if (WinApi.IsForeground(hwnd))
                        MenuStep(hwnd, step++);
                }

                if (step > 0 && step % 20 == 0)
                    Log($"идёт тест... {sw.Elapsed:mm\\:ss}");

                Thread.Sleep(3000);
            }

            throw new TimeoutException($"нет результата за {_timeout.TotalMinutes} мин");
        }
        finally
        {
            Kill();
        }
    }

    private static void MenuStep(IntPtr hwnd, int step)
    {
        switch (step % 3)
        {
            case 0:
                WinApi.PressKey(WinApi.ScanEnter);
                break;
            case 1:
                WinApi.ClickRelative(hwnd, StartBtnX, StartBtnY);
                break;
            case 2:
                WinApi.ClickRelative(hwnd, ConfirmBtnX, ConfirmBtnY);
                break;
        }
    }

    private void Start()
    {
        try
        {
            Process.Start(new ProcessStartInfo($"steam://rungameid/{Steam.BenchmarkAppId}") { UseShellExecute = true });
            Log("запуск через Steam");
            return;
        }
        catch (Exception e)
        {
            Log("не удалось запустить через Steam: " + e.Message);
        }

        var exe = FindExe() ?? throw new FileNotFoundException("не найден exe бенчмарка в " + _benchDir);
        Process.Start(new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe),
        });
        Log("запуск " + exe);
    }

    private string? FindExe()
    {
        string[] candidates =
        {
            Path.Combine(_benchDir, "b1.exe"),
            Path.Combine(_benchDir, "b1_benchmark.exe"),
            Path.Combine(_benchDir, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private BenchResult? FindNewResult(HashSet<string> oldFiles)
    {
        if (!Directory.Exists(_historyDir))
            return null;

        return Directory.GetFiles(_historyDir)
            .Where(f => !oldFiles.Contains(f))
            .OrderByDescending(File.GetLastWriteTime)
            .Select(BenchResult.TryLoad)
            .FirstOrDefault(r => r != null);
    }

    private static List<Process> FindProcesses() =>
        ProcessNames.SelectMany(Process.GetProcessesByName).ToList();

    public static void Kill()
    {
        foreach (var p in FindProcesses())
        {
            try
            {
                p.Kill(true);
                p.WaitForExit(10000);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private static void Log(string msg) => Console.WriteLine($"    [{DateTime.Now:HH:mm:ss}] {msg}");
}
