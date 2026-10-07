using System.Text.Json;
using System.Text.Json.Serialization;

namespace WukongAutoBench;

class BenchResult
{
    public double FPSAvg { get; set; }
    public double FPSMax { get; set; }
    public double FPSMin { get; set; }
    public double FPS95 { get; set; }
    public double CPUAvg { get; set; }
    public double GPUAvg { get; set; }

    public string GameVer { get; set; } = "";
    public string CPUModel { get; set; } = "";
    public string GPUModel { get; set; } = "";
    public string GpuDriverVer { get; set; } = "";
    public string VideoMemSize { get; set; } = "";
    public string SysMem { get; set; } = "";

    public string ScreenResolution { get; set; } = "";
    public int QualityLevel { get; set; }
    public int ImageQuality { get; set; }
    public int Rtx { get; set; }
    public int Dlss { get; set; }
    public int InsertFrame { get; set; }
    public int Dx12 { get; set; }

    public List<FrameRecord> Records { get; set; } = new();

    [JsonIgnore] public string RawPath { get; set; } = "";

    public int Frames => Records.Count;

    public double AvgFrameTimeMs => FrameTimes().DefaultIfEmpty(0).Average();

    public double AvgCpuFrameTimeMs => Records.Select(r => r.CPUFrameTime).DefaultIfEmpty(0).Average();

    public double AvgGpuFrameTimeMs => Records.Select(r => r.GPUFrameTime).DefaultIfEmpty(0).Average();

    public double Fps1Low
    {
        get
        {
            var times = FrameTimes().OrderByDescending(t => t).ToList();
            if (times.Count == 0)
                return 0;
            int n = Math.Max(1, times.Count / 100);
            return 1000.0 / times.Take(n).Average();
        }
    }

    public double CpuBoundPercent =>
        Records.Count == 0 ? 0 : 100.0 * Records.Count(r => r.CPUFrameTime > r.GPUFrameTime) / Records.Count;

    private IEnumerable<double> FrameTimes() =>
        Records.Where(r => r.FrameRate > 0).Select(r => 1000.0 / r.FrameRate);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static BenchResult? TryLoad(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var result = JsonSerializer.Deserialize<BenchResult>(fs, JsonOptions);
            if (result == null || result.FPSAvg <= 0)
                return null;
            result.RawPath = path;
            return result;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

class FrameRecord
{
    public double FrameRate { get; set; }
    public double CPUUsage { get; set; }
    public double GPUUsage { get; set; }
    public double CPUFrameTime { get; set; }
    public double GPUFrameTime { get; set; }
}
