using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;

namespace MpvNet.Windows.WPF.Controls;

public partial class SearchControl
{
    private string? _hintText;
    private bool    _gotFocus;

    public bool HideClearButton { get; set; }

    public SearchControl() => InitializeComponent();

    public string HintText
    {
        get => _hintText ??= "";
        set
        {
            _hintText = value;
            UpdateControls();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Text = "";
        Keyboard.Focus(SearchTextBox);
    }

    private void UpdateControls()
    {
        HintTextBlock.Text = string.IsNullOrEmpty(Text) ? HintText : "";

        if (string.IsNullOrEmpty(Text) || HideClearButton || Text.Length > 30)
        {
            SearchTextBox.Padding        = new Thickness(2);
            SearchClearButton.Visibility = Visibility.Hidden;
        }
        else
        {
            SearchTextBox.Padding        = new Thickness(2, 2, 20, 2);
            SearchClearButton.Visibility = Visibility.Visible;
        }
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string),
                                    typeof(SearchControl), new PropertyMetadata(OnCustomerChangedCallBack));

    private static void OnCustomerChangedCallBack(
        DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        (sender as SearchControl)?.UpdateControls();

    private void SearchTextBoxGotFocus(object sender, RoutedEventArgs e) => _gotFocus = true;

    private void SearchTextBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_gotFocus) return;
        SearchTextBox?.SelectAll();
        _gotFocus = false;
    }

    private void SearchTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || string.IsNullOrEmpty(Text)) return;
        Text      = "";
        e.Handled = true;
    }
}
