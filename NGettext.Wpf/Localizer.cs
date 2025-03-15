using System.Globalization;

namespace NGettext.Wpf
{
    public interface ILocalizer
    {
        ICatalog        Catalog        { get; }
        ICultureTracker CultureTracker { get; }
    }

    public class Localizer : IDisposable, ILocalizer
    {
        string _domainName;
        string _localeFolder;

        public Localizer(ICultureTracker cultureTracker, string domainName, string localeFolder)
        {
            _domainName    = domainName;
            _localeFolder  = localeFolder;
            CultureTracker = cultureTracker;

            if (cultureTracker == null)
                throw new ArgumentNullException(nameof(cultureTracker));

            cultureTracker.CultureChanging += ResetCatalog!;
            ResetCatalog(cultureTracker.CurrentCulture);
        }

        private void ResetCatalog(object sender, CultureEventArgs e)
        {
            ResetCatalog(e.CultureInfo);
        }

        private void ResetCatalog(CultureInfo cultureInfo)
        {
            Catalog = GetCatalog(cultureInfo);
        }

        public ICatalog GetCatalog(CultureInfo cultureInfo) =>
            new Catalog(_domainName, _localeFolder, cultureInfo);

        public ICatalog Catalog { get; private set; }

        public ICultureTracker CultureTracker { get; }

        public void Dispose()
        {
            CultureTracker.CultureChanging -= ResetCatalog!;
        }
    }

    public static class LocalizerExtensions
    {
        internal struct MsgIdWithContext
        {
            internal string Context { get; set; }
            internal string MsgId   { get; set; }
        }

        internal static MsgIdWithContext ConvertToMsgIdWithContext(string msgId)
        {
            var result = new MsgIdWithContext { MsgId = msgId };

            if (!msgId.Contains("|")) return result;
            var pipePosition = msgId.IndexOf('|');
            result.Context = msgId[..pipePosition];
            result.MsgId   = msgId[(pipePosition + 1)..];

            return result;
        }

        internal static string Gettext(this ILocalizer localizer, string msgId, params object[] values)
        {
            var msgIdWithContext = ConvertToMsgIdWithContext(msgId);

            return localizer.Catalog.GetParticularString(msgIdWithContext.Context, msgIdWithContext.MsgId, values);
        }

        internal static string? Gettext(this ILocalizer localizer, string msgId)
        {
            var msgIdWithContext = ConvertToMsgIdWithContext(msgId);

            return localizer.Catalog.GetParticularString(msgIdWithContext.Context, msgIdWithContext.MsgId);
        }
    }
}
