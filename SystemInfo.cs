using System.Management;
using Microsoft.Win32;

namespace WukongAutoBench;

class SystemInfo
{
    public List<(string, string)> Items { get; } = new();

    public static SystemInfo Collect()
    {
        var info = new SystemInfo();

        foreach (var cpu in Query("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor"))
        {
            info.Add("Процессор", Str(cpu["Name"]));
            info.Add("Ядра / потоки", $"{cpu["NumberOfCores"]} / {cpu["NumberOfLogicalProcessors"]}");
            info.Add("Базовая частота", $"{cpu["MaxClockSpeed"]} МГц");
        }

        int gpuIndex = 0;
        foreach (var gpu in Query("SELECT Name, DriverVersion, AdapterRAM, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController"))
        {
            var name = Str(gpu["Name"]);
            var suffix = gpuIndex++ == 0 ? "" : $" #{gpuIndex}";
            info.Add("Видеокарта" + suffix, name);
            info.Add("Драйвер" + suffix, Str(gpu["DriverVersion"]));
            info.Add("Видеопамять" + suffix, VramText(name, gpu["AdapterRAM"]));
            if (gpu["CurrentHorizontalResolution"] != null)
                info.Add("Экран" + suffix, $"{gpu["CurrentHorizontalResolution"]}x{gpu["CurrentVerticalResolution"]} @ {gpu["CurrentRefreshRate"]} Гц");
        }

        ulong totalRam = 0;
        int sticks = 0;
        uint speed = 0;
        foreach (var m in Query("SELECT Capacity, ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory"))
        {
            totalRam += Convert.ToUInt64(m["Capacity"] ?? 0UL);
            sticks++;
            var s = Convert.ToUInt32(m["ConfiguredClockSpeed"] ?? m["Speed"] ?? 0u);
            if (s > speed)
                speed = s;
        }
        if (sticks > 0)
            info.Add("Оперативная память", $"{totalRam / (1024 * 1024 * 1024)} ГБ ({sticks} шт., {speed} МТ/с)");

        foreach (var b in Query("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
            info.Add("Материнская плата", $"{Str(b["Manufacturer"])} {Str(b["Product"])}");

        foreach (var os in Query("SELECT Caption, Version, OSArchitecture FROM Win32_OperatingSystem"))
            info.Add("ОС", $"{Str(os["Caption"])} {Str(os["Version"])} {Str(os["OSArchitecture"])}");

        return info;
    }

    public string Get(string key) => Items.FirstOrDefault(i => i.Item1 == key).Item2 ?? "";

    private void Add(string key, string value) => Items.Add((key, value));

    private static IEnumerable<ManagementBaseObject> Query(string wql)
    {
        var list = new List<ManagementBaseObject>();
        try
        {
            using var searcher = new ManagementObjectSearcher(wql);
            foreach (var o in searcher.Get())
                list.Add(o);
        }
        catch (Exception e)
        {
            Console.WriteLine($"WMI: не удалось выполнить запрос ({e.Message})");
        }
        return list;
    }

    private static string VramText(string gpuName, object? adapterRam)
    {
        try
        {
            const string classKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
            using var root = Registry.LocalMachine.OpenSubKey(classKey);
            if (root != null)
            {
                foreach (var sub in root.GetSubKeyNames())
                {
                    using var k = root.OpenSubKey(sub);
                    if (k?.GetValue("DriverDesc") as string != gpuName)
                        continue;
                    if (k.GetValue("HardwareInformation.qwMemorySize") is long bytes && bytes > 0)
                        return $"{bytes / (1024 * 1024)} МБ";
                }
            }
        }
        catch (Exception)
        {
        }

        if (adapterRam != null && Convert.ToUInt64(adapterRam) > 0)
            return $"{Convert.ToUInt64(adapterRam) / (1024 * 1024)} МБ";
        return "н/д";
    }

    private static string Str(object? o) => o?.ToString()?.Trim() ?? "н/д";
}
