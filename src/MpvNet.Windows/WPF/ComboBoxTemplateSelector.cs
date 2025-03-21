using System.Windows.Controls;
using System.Windows;

namespace MpvNet.Windows.WPF;

public class ComboBoxTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object? item, DependencyObject container)
    {
        var presenter = (ContentPresenter)container;

        if (presenter.TemplatedParent is ComboBox)
            return (DataTemplate)presenter.FindResource("ComboBoxCollapsedDataTemplate");
        return (DataTemplate)presenter.FindResource("ComboBoxExpandedDataTemplate");
    }
}
