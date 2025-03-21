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

        public static string Noop(string msgId) => msgId;

        public static string GetPluralString(string singularMsgId, string pluralMsgId, int n, params object[] args)
        {
            if (Localizer is not null)
                return args.Any()
                    ? Localizer.Catalog.GetPluralString(singularMsgId, pluralMsgId, n, args)
                    : Localizer.Catalog.GetPluralString(singularMsgId, pluralMsgId, n);
            CompositionRoot.WriteMissingInitializationErrorMessage();
            return string.Format(CultureInfo.InvariantCulture, n == 1 ? singularMsgId : pluralMsgId, args);

        }

        public static string GetParticularPluralString(string          context, string text, string pluralText, int n,
                                                       params object[] args)
        {
            if (Localizer is not null)
                return args.Any()
                    ? Localizer.Catalog.GetParticularPluralString(context, text, pluralText, n, args)
                    : Localizer.Catalog.GetParticularPluralString(context, text, pluralText, n);
            CompositionRoot.WriteMissingInitializationErrorMessage();
            return string.Format(CultureInfo.InvariantCulture, n == 1 ? text : pluralText, args);

        }

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
