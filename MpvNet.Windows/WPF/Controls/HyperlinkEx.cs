using System.Windows.Documents;
using System.Windows.Navigation;
using MpvNet.Help;

namespace MpvNet.Windows.WPF.Controls;

public class HyperlinkEx : Hyperlink
{
    private static void HyperLinkExRequestNavigate(object sender, RequestNavigateEventArgs e) =>
        ProcessHelp.ShellExecute(e.Uri.AbsoluteUri);

    public void SetUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        NavigateUri     =  new Uri(url);
        RequestNavigate += HyperLinkExRequestNavigate;
        Inlines.Clear();
        Inlines.Add("Manual");
    }
}
