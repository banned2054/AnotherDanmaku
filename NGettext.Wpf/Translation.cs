using System.Globalization;

namespace NGettext.Wpf
{
    public static class Translation
    {
        public static string _(string msgId) => Localizer?.Gettext(msgId) ?? "";

        public static string _(string msgId, params object[] parameters)
        {
            return parameters.Any()
                ? Localizer?.Gettext(msgId, parameters) ?? ""
                : Localizer?.Gettext(msgId)             ?? "";
        }

        public static ILocalizer? Localizer { get; set; }

        public static string GetParticularString(string context, string text, params object[] args)
        {
            if (Localizer is not null)
                return args.Any()
                    ? Localizer.Catalog.GetParticularString(context, text, args)
                    : Localizer.Catalog.GetParticularString(context, text);
            CompositionRoot.WriteMissingInitializationErrorMessage();
            return (args.Any() ? string.Format(CultureInfo.InvariantCulture, text, args) : text);
        }
    }
}
