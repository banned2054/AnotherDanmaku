using MpvNet.Windows.WPF.MsgBox;
using System.Threading;
using System.Windows;
using Forms = System.Windows.Forms;

namespace MpvNet.Windows.WPF;

public class Msg
{
    public static void ShowInfo(object msg) => Show(msg, MessageBoxImage.Information);

    public static void ShowError(object msg) => Show(msg, MessageBoxImage.Error);

    public static void ShowWarning(object msg) => Show(msg, MessageBoxImage.Warning);

    public static MessageBoxResult ShowQuestion(object           msg,
                                                MessageBoxButton buttons = MessageBoxButton.OKCancel)
    {
        return Show(msg, MessageBoxImage.Question, buttons);
    }

    public static void ShowException(Exception exception)
    {
        Show(exception.Message, MessageBoxImage.Error, MessageBoxButton.OK, exception.ToString());
    }

    public static MessageBoxResult Show(
        object           msg,
        MessageBoxImage  img,
        MessageBoxButton buttons = MessageBoxButton.OK,
        string?          details = null)
    {
        var state = Thread.CurrentThread.GetApartmentState();

        return state == ApartmentState.STA ? Fn() : Application.Current.Dispatcher.Invoke(Fn);

        MessageBoxResult Fn()
        {
            MessageBoxEx.DetailsText = details;

            return MessageBoxEx.OpenMessageBox((msg + "").Trim(),
                                               Forms.Application.ProductName!, buttons, img);
        }
    }
}
