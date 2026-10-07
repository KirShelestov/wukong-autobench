using System.Text;
using System.Text.RegularExpressions;

namespace WukongAutoBench;

class GameConfig
{
    public const string MainSection = "/Script/GSGameSettings.GSGameUserSettings";
    public const string SgSection = "ScalabilityGroups";

    private readonly string _path;
    private readonly string _backupPath;
    private List<string> _lines = new();
    private Encoding _encoding = new UTF8Encoding(false);

    public GameConfig(string path)
    {
        _path = path;
        _backupPath = path + ".autobench.bak";
    }

    public string FilePath => _path;
    public bool HasBackup => File.Exists(_backupPath);

    public void Load()
    {
        using var reader = new StreamReader(_path, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        _encoding = reader.CurrentEncoding;
        _lines = text.Replace("\r\n", "\n").Split('\n').ToList();
    }

    public void Save()
    {
        File.WriteAllText(_path, string.Join("\r\n", _lines), _encoding);
    }

    public void MakeBackup()
    {
        File.Copy(_path, _backupPath, true);
    }

    public void RestoreBackup()
    {
        if (!File.Exists(_backupPath))
            return;
        File.Copy(_backupPath, _path, true);
        File.Delete(_backupPath);
    }

    public string? Get(string section, string key)
    {
        var (start, end) = FindSection(section);
        if (start < 0)
            return null;

        for (int i = start + 1; i < end; i++)
        {
            if (_lines[i].StartsWith(key + "="))
                return _lines[i].Substring(key.Length + 1);
        }
        return null;
    }

    public void Set(string section, string key, string value)
    {
        var (start, end) = FindSection(section);
        if (start < 0)
        {
            _lines.Add("");
            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
            return;
        }

        for (int i = start + 1; i < end; i++)
        {
            if (_lines[i].StartsWith(key + "="))
            {
                _lines[i] = $"{key}={value}";
                return;
            }
        }

        int pos = end;
        while (pos > start + 1 && string.IsNullOrWhiteSpace(_lines[pos - 1]))
            pos--;
        _lines.Insert(pos, $"{key}={value}");
    }

    public string? GetUi(string key)
    {
        var data = Get(MainSection, "UISettingData");
        if (data == null)
            return null;
        var m = UiRegex(key).Match(data);
        return m.Success ? m.Groups[1].Value : null;
    }

    public void SetUi(string key, string value)
    {
        var data = Get(MainSection, "UISettingData");
        if (data == null)
            throw new InvalidOperationException("В конфиге нет UISettingData - запустите бенчмарк вручную хотя бы один раз");

        var re = UiRegex(key);
        var item = $"(\"{key}\", \"{value}\")";
        if (re.IsMatch(data))
            data = re.Replace(data, item);
        else if (data == "()")
            data = $"({item})";
        else
            data = data.Substring(0, data.Length - 1) + "," + item + ")";

        Set(MainSection, "UISettingData", data);
    }

    private static Regex UiRegex(string key) =>
        new Regex("\\(\"" + Regex.Escape(key) + "\",\\s*\"([^\"]*)\"\\)");

    private (int start, int end) FindSection(string section)
    {
        int start = _lines.FindIndex(l => l.Trim() == $"[{section}]");
        if (start < 0)
            return (-1, -1);

        int end = start + 1;
        while (end < _lines.Count && !_lines[end].TrimStart().StartsWith('['))
            end++;
        return (start, end);
    }
}
