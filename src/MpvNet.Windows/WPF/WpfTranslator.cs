using NGettext.Wpf;
using System.Globalization;

namespace MpvNet.Windows.WPF;

public class WpfTranslator : ITranslator
{
    private string _localizerLanguage = "";

    private static IEnumerable<Language> Languages { get; } = new Language[]
    {
        new("english", "en", "en"),
        new("chinese-china", "zh-CN", "zh"), // Chinese (Simplified)
        new("german", "de", "de"),
        new("japanese", "ja", "ja"),
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

    private void InitNGettextWpf()
    {
        if (Translation.Localizer != null && _localizerLanguage == App.Language) return;
        CompositionRoot.Compose("mpvnet", GetCulture(App.Language), Folder.Startup + "Locale");
        _localizerLanguage = App.Language;
    }

    private static string GetSystemLanguage()
    {
        var twoLetterName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return twoLetterName == "zh"
            ? "chinese-china" // Chinese (Simplified)
            : new CultureInfo(twoLetterName).EnglishName.ToLowerInvariant();
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

    private class Language
    {
        public string MpvNetName      { get; }
        public string CultureInfoName { get; }
        public string TwoLetterName   { get; }

        public Language(string mpvNetName, string cultureInfoName, string twoLetterName)
        {
            MpvNetName      = mpvNetName;
            CultureInfoName = cultureInfoName;
            TwoLetterName   = twoLetterName;
        }
    }
}
