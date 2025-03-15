using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace NGettext.Wpf
{
    [MarkupExtensionReturnType(typeof(string))]
    public class GettextExtension(string msgId, params object[] @params) : MarkupExtension, IWeakCultureObserver
    {
        private DependencyObject   _dependencyObject   = null!;
        private DependencyProperty _dependencyProperty = null!;

        [ConstructorArgument("params")] public object[] Params { get; set; } = @params;

        [ConstructorArgument("msgId")] public string MsgId { get; set; } = msgId;

        public GettextExtension(string msgId) : this(msgId, new object[] { })
        {
        }

        public static ILocalizer Localizer { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
            if (provideValueTarget.TargetObject is DependencyObject dependencyObject)
            {
                _dependencyObject = dependencyObject;
                if (DesignerProperties.GetIsInDesignMode(_dependencyObject))
                {
                    return Gettext();
                }

                AttachToCultureChangedEvent();

                _dependencyProperty = (DependencyProperty)provideValueTarget.TargetProperty;

                KeepGettextExtensionAliveForAsLongAsDependencyObject();
            }
            else
            {
                System.Console.WriteLine("NGettext.Wpf: Target object of type {0} is not yet implemented",
                                         provideValueTarget.TargetObject?.GetType());
            }

            return Gettext();
        }

        private string Gettext()
        {
            return (Params.Any() ? Localizer.Gettext(MsgId, Params) : Localizer.Gettext(MsgId))!;
        }

        private void KeepGettextExtensionAliveForAsLongAsDependencyObject()
        {
            SetGettextExtension(_dependencyObject, this);
        }

        private void AttachToCultureChangedEvent()
        {
            Localizer.CultureTracker.AddWeakCultureObserver(this);
        }

        public void HandleCultureChanged(ICultureTracker sender, CultureEventArgs eventArgs)
        {
            _dependencyObject.SetValue(_dependencyProperty, Gettext());
        }

        public static readonly DependencyProperty GettextExtensionProperty = DependencyProperty.RegisterAttached(
             "GettextExtension", typeof(GettextExtension), typeof(GettextExtension),
             new PropertyMetadata(default(GettextExtension)));

        public static void SetGettextExtension(DependencyObject element, GettextExtension value)
        {
            element.SetValue(GettextExtensionProperty, value);
        }
    }
}
