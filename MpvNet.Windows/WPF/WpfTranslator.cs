using NGettext.Wpf;
using System.Globalization;

namespace MpvNet.Windows.WPF;

public class WpfTranslator : ITranslator
{
    private string _localizerLangauge = string.Empty;

    private static IEnumerable<Language> Languages { get; } = new Language[]
    {
        new("english", "en", "en"),
        new("chinese-china", "zh-CN", "zh"), // Chinese (Simplified)
        new("french", "fr", "fr"),
        new("german", "de", "de"),
        new("japanese", "ja", "ja"),
        new("korean", "ko", "ko"),
        new("polish", "pl", "pl"),
        new("russian", "ru", "ru"),
        new("turkish", "tr", "tr"),
    };

    public string Gettext(string msgId)
    {
        InitNGettextWpf();
        return Translation._(msgId);
    }

    public string GetParticularString(string context, string text)
    {
        InitNGettextWpf();
        return Translation.GetParticularString(context, text);
    }

    void InitNGettextWpf()
    {
        if (Translation.Localizer == null || _localizerLangauge != App.Language)
        {
            CompositionRoot.Compose("mpvnet", GetCulture(App.Language), Folder.Startup + "Locale");
            _localizerLangauge = App.Language;
        }
    }

    private static string GetSystemLanguage()
    {
        var twoLetterName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return twoLetterName == "zh"
            ? "chinese-china"
            : // Chinese (Simplified)
            new CultureInfo(twoLetterName).EnglishName.ToLowerInvariant();
    }

    private static CultureInfo GetCulture(string name)
    {
        if (name == "system")
            name = GetSystemLanguage();

        foreach (var lang in Languages)
            if (lang.MpvNetName == name)
                return new CultureInfo(lang.CultureInfoName);

        return new CultureInfo("en");
    }

    private class Language(string mpvNetName, string cultureInfoName, string twoLetterName)
    {
        public string MpvNetName      { get; } = mpvNetName;
        public string CultureInfoName { get; } = cultureInfoName;
    }
}
