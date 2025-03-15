using MpvNet.ExtensionMethod;

namespace MpvNet.Windows;

public class Conf
{
    public static List<Setting> LoadConf(string content)
    {
        var settingsList = new List<Setting>();

        foreach (var section in ConfParser.Parse(content))
        {
            Setting? baseSetting;

            if (section.HasName("option"))
            {
                var optionSetting = new OptionSetting();
                baseSetting           = optionSetting;
                optionSetting.Default = section.GetValue("default");
                optionSetting.Value   = optionSetting.Default;

                foreach (var it in section.GetValues("option"))
                {
                    var opt = new OptionSettingOption();

                    if (it.Value.ContainsEx(" "))
                    {
                        opt.Name = it.Value![..it.Value!.IndexOf(" ", StringComparison.Ordinal)];
                        opt.Help = it.Value[it.Value.IndexOf(" ", StringComparison.Ordinal)..].Trim();
                    }
                    else
                        opt.Name = it.Value;

                    if (opt.Name == optionSetting.Default)
                        opt.Text = opt.Name + " (Default)";

                    opt.OptionSetting = optionSetting;
                    optionSetting.Options.Add(opt);
                }
            }
            else
            {
                var stringSetting = new StringSetting();
                baseSetting           = stringSetting;
                stringSetting.Default = section.HasName("default") ? section.GetValue("default") : "";
            }

            baseSetting.Name      = section.GetValue("name");
            baseSetting.File      = section.GetValue("file");
            baseSetting.Directory = section.GetValue("directory");

            if (section.HasName("help")) baseSetting.Help   = section.GetValue("help");
            if (section.HasName("url")) baseSetting.Url     = section.GetValue("url");
            if (section.HasName("width")) baseSetting.Width = Convert.ToInt32(section.GetValue("width"));
            if (section.HasName("option-name-width"))
                baseSetting.OptionNameWidth = Convert.ToInt32(section.GetValue("option-name-width"));
            if (section.HasName("type")) baseSetting.Type = section.GetValue("type");

            if (baseSetting.Help.ContainsEx("\\n"))
                baseSetting.Help = baseSetting.Help?.Replace("\\n", "\n");

            settingsList.Add(baseSetting);
        }

        return settingsList;
    }
}

public class ConfItem
{
    public string Comment     { get; set; } = string.Empty;
    public string File        { get; set; } = string.Empty;
    public string LineComment { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public string Section     { get; set; } = string.Empty;
    public string Value       { get; set; } = string.Empty;

    public bool     IsSectionItem { get; set; }
    public Setting? SettingBase   { get; set; }
}

public class ConfParser
{
    public static List<ConfSection> Parse(string content)
    {
        var          lines        = content.Split('\n');
        var          sections     = new List<ConfSection>();
        ConfSection? currentGroup = null;

        foreach (var it in lines)
        {
            var line = it.Trim();

            if (line.StartsWith('#'))
                continue;

            if (line == "")
            {
                currentGroup = new ConfSection();
                sections.Add(currentGroup);
            }
            else if (line.Contains('='))
            {
                var name  = line[..line.IndexOf("=", StringComparison.Ordinal)].Trim();
                var value = line[(line.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim();

                currentGroup?.Items.Add(new StringPair(name, value));
            }
        }

        return sections;
    }
}

public class ConfSection
{
    public List<StringPair> Items { get; set; } = new();

    public bool HasName(string name)
    {
        return Items.Any(i => i.Name == name);
    }

    public string? GetValue(string name)
    {
        return (from i in Items where i.Name == name select i.Value).FirstOrDefault();
    }

    public List<StringPair> GetValues(string name) => Items.Where(i => i.Name == name).ToList();
}
