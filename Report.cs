using System.Globalization;
using System.Text;

namespace WukongAutoBench;

record PassResult(Preset Preset, List<(string, string)> Settings, BenchResult? Result, string? Error);

static class Report
{
    public static string Build(SystemInfo sys, List<PassResult> passes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Black Myth: Wukong Benchmark Tool - результаты");
        sb.AppendLine();
        sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("## Компьютер");
        sb.AppendLine();
        Table(sb, new[] { "Параметр", "Значение" }, sys.Items.Select(i => new[] { i.Item1, i.Item2 }));

        var game = passes.Select(p => p.Result).FirstOrDefault(r => r != null);
        if (game != null)
            sb.AppendLine($"Версия бенчмарка: {game.GameVer}, бенчмарк видит GPU как \"{game.GPUModel}\", VRAM {game.VideoMemSize}, RAM {game.SysMem}");
        sb.AppendLine();

        sb.AppendLine("## Результаты");
        sb.AppendLine();
        var rows = new List<string[]>();
        foreach (var p in passes)
        {
            var r = p.Result;
            if (r == null)
            {
                rows.Add(new[] { p.Preset.Name + "-тест", "ошибка: " + p.Error, "", "", "", "", "", "", "", "" });
                continue;
            }
            rows.Add(new[]
            {
                p.Preset.Name + "-тест",
                F(r.FPSAvg), F(r.FPS95), F(r.Fps1Low), F(r.FPSMin), F(r.FPSMax),
                F(r.AvgFrameTimeMs, 2), F(r.AvgCpuFrameTimeMs, 2), F(r.AvgGpuFrameTimeMs, 2),
                F(r.CpuBoundPercent, 0) + "%",
            });
        }
        Table(sb, new[] { "Тест", "FPS ср.", "FPS 95%", "1% low", "FPS мин", "FPS макс", "Кадр, мс", "CPU, мс", "GPU, мс", "Упор в CPU" }, rows);

        sb.AppendLine("FPS ср./95%/мин/макс - из файла бенчмарка, 1% low и времена кадра посчитаны по покадровым записям (Records).");
        sb.AppendLine("CPU, мс / GPU, мс - среднее время кадра на процессоре и видеокарте, \"упор в CPU\" - доля кадров, где CPU считал дольше GPU.");
        sb.AppendLine();

        foreach (var p in passes)
        {
            sb.AppendLine($"## Настройки {p.Preset.Name}-теста");
            sb.AppendLine();
            Table(sb, new[] { "Параметр", "Значение" }, p.Settings.Select(s => new[] { s.Item1, s.Item2 }));
            if (p.Result != null)
            {
                var r = p.Result;
                sb.AppendLine($"По данным бенчмарка: {r.ScreenResolution}, качество {r.QualityLevel}, масштаб {r.ImageQuality}%, " +
                              $"RT {(r.Rtx != 0 ? "вкл." : "выкл.")}, генерация кадров {(r.InsertFrame != 0 ? "вкл." : "выкл.")}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static void Table(StringBuilder sb, string[] header, IEnumerable<string[]> rows)
    {
        var all = new List<string[]> { header };
        all.AddRange(rows);

        var widths = new int[header.Length];
        foreach (var row in all)
            for (int i = 0; i < row.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);

        void Line(string[] row) =>
            sb.AppendLine("| " + string.Join(" | ", row.Select((c, i) => c.PadRight(widths[i]))) + " |");

        Line(header);
        sb.AppendLine("|" + string.Join("|", widths.Select(w => new string('-', w + 2))) + "|");
        foreach (var row in all.Skip(1))
            Line(row);
        sb.AppendLine();
    }

    private static string F(double v, int digits = 1) => v.ToString("F" + digits, CultureInfo.InvariantCulture);
}
