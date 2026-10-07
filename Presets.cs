namespace WukongAutoBench;

class Preset
{
    public required string Name { get; init; }

    public required int Quality { get; init; }

    public required int RenderScale { get; init; }

    public bool RayTracing { get; set; }
    public bool MotionBlur { get; init; }

    private static readonly string[] UiQualityKeys =
    {
        "ViewDistance", "AntiAliasing", "PostProcessing", "ShadowQuality", "TextureQuality",
        "FxQuality", "MaterialQuality", "VegetationQuality", "GlobalIllumination", "ReflectionQuality",
    };

    private static readonly string[] SgKeys =
    {
        "sg.ViewDistanceQuality", "sg.AntiAliasingQuality", "sg.ShadowQuality", "sg.GlobalIlluminationQuality",
        "sg.ReflectionQuality", "sg.PostProcessQuality", "sg.TextureQuality", "sg.EffectsQuality",
        "sg.FoliageQuality", "sg.ShadingQuality",
    };

    public static Preset Cpu() => new()
    {
        Name = "CPU",
        Quality = 1,
        RenderScale = 50,
        RayTracing = false,
        MotionBlur = false,
    };

    public static Preset Gpu(bool rayTracing) => new()
    {
        Name = "GPU",
        Quality = 5,
        RenderScale = 100,
        RayTracing = rayTracing,
        MotionBlur = true,
    };

    public void Apply(GameConfig cfg)
    {
        var q = Quality.ToString();
        var sg = (Quality - 1).ToString();

        cfg.SetUi("QualityLevel", q);
        foreach (var key in UiQualityKeys)
            cfg.SetUi(key, q);
        foreach (var key in SgKeys)
            cfg.Set(GameConfig.SgSection, key, sg);

        cfg.SetUi("ImageQuality", RenderHeight(cfg).ToString());
        cfg.Set(GameConfig.SgSection, "sg.ResolutionQuality", RenderScale.ToString());

        cfg.SetUi("Vsync", "0");
        cfg.Set(GameConfig.MainSection, "bUseVSync", "False");
        cfg.SetUi("LockFrameRate", "0");
        cfg.Set(GameConfig.MainSection, "FrameRateLimit", "0.000000");
        cfg.SetUi("InsertFrame", "0");

        cfg.SetUi("MotionBlur", MotionBlur ? "2" : "0");

        cfg.SetUi("Rtx", RayTracing ? "1" : "0");
        cfg.SetUi("RtxLevel", RayTracing ? "2" : "0");
        cfg.Set(GameConfig.SgSection, "sg.RayTracingQuality", RayTracing ? "2" : "0");
    }

    public int RenderHeight(GameConfig cfg)
    {
        if (!int.TryParse(cfg.Get(GameConfig.MainSection, "ResolutionSizeY"), out var screenH) || screenH <= 0)
            screenH = 1080;
        return screenH * RenderScale / 100;
    }

    public IEnumerable<(string, string)> Describe(GameConfig cfg)
    {
        var w = cfg.Get(GameConfig.MainSection, "ResolutionSizeX") ?? "?";
        var h = cfg.Get(GameConfig.MainSection, "ResolutionSizeY") ?? "?";

        yield return ("Разрешение экрана", $"{w}x{h} (не меняется)");
        yield return ("Масштаб рендеринга", $"{RenderScale}% (высота рендера {RenderHeight(cfg)} px)");
        yield return ("Качество графики", $"{QualityName(Quality)} (все группы = {Quality}, sg.* = {Quality - 1})");
        yield return ("Трассировка лучей", RayTracing ? "вкл., RtxLevel=2" : "выкл.");
        yield return ("Размытие в движении", MotionBlur ? "вкл." : "выкл.");
        yield return ("Генерация кадров", "выкл.");
        yield return ("VSync / лимит FPS", "выкл. / выкл.");
        yield return ("Апскейлер", "без изменений (как в конфиге)");
    }

    private static string QualityName(int q) => q switch
    {
        1 => "низкое",
        2 => "среднее",
        3 => "высокое",
        4 => "очень высокое",
        5 => "кинематографическое",
        _ => q.ToString(),
    };
}
