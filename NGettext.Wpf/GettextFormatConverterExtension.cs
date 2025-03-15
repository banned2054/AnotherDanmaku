using NGettext.Wpf.Common;
using System.Windows.Markup;

namespace NGettext.Wpf
{
    public class GettextFormatConverterExtension : MarkupExtension
    {
        public GettextFormatConverterExtension(string msgId)
        {
            MsgId = msgId;
        }

        [ConstructorArgument("msgId")] public string MsgId { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new GettextStringFormatConverter(MsgId);
        }
    }
}
