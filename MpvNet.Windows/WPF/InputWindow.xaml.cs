using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MpvNet.Windows.WPF;

public partial class InputWindow : Window
{
    private readonly ICollectionView _collectionView;
    private readonly string          _startupContent;
    public           List<Binding>   Bindings { get; }
    private          Binding?        _focusedBinding;

    public InputWindow()
    {
        InitializeComponent();
        DataContext = this;

        Bindings = App.InputConf.HasMenu
            ? InputHelp.Parse(App.InputConf.Content)
            : InputHelp.GetEditorBindings(App.InputConf.Content);

        _startupContent                         =  InputHelp.ConvertToString(Bindings);
        SearchControl.SearchTextBox.TextChanged += SearchTextBox_TextChanged;
        DataGrid.SelectionMode                  =  DataGridSelectionMode.Single;
        var collectionViewSource = new CollectionViewSource() { Source = Bindings };
        _collectionView        = collectionViewSource.View;
        _collectionView.Filter = item => Filter((Binding)item);
        DataGrid.ItemsSource   = _collectionView;
    }

    private bool Filter(Binding item)
    {
        if (item.Command == "")
            return false;

        var searchText = SearchControl.SearchTextBox.Text.ToLower();

        if (searchText is "" or "?")
            return true;

        if (searchText.Length == 1)
            return item.Input.ToLower().Replace("ctrl+", "").Replace("shift+", "").Replace("alt+", "") ==
                   searchText.ToLower();
        if (searchText.StartsWith("i ") || searchText.StartsWith("i:") || searchText.Length == 1)
        {
            if (searchText.Length > 1)
                searchText = searchText[2..].Trim();

            if (searchText.Length < 3)
                return item.Input.ToLower().Replace("ctrl+", "").Replace("shift+", "").Replace("alt+", "")
                           .Contains(searchText);
            else
                return item.Input.ToLower().Contains(searchText);
        }

        if (searchText.StartsWith("n ") || searchText.StartsWith("n:"))
            return item.Comment.ToLower().Contains(searchText[2..].Trim());
        if (searchText.StartsWith("c ") || searchText.StartsWith("c:"))
            return item.Command.ToLower().Contains(searchText[2..].Trim());
        return item.Command.ToLower().Contains(searchText) ||
               item.Comment.ToLower().Contains(searchText) ||
               item.Input.ToLower().Contains(searchText);
    }

    private void ShowLearnWindow(Binding? binding)
    {
        var window = new LearnWindow
        {
            Owner     = this,
            InputItem = binding
        };
        window.ShowDialog();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Escape :
                Close();
                break;
            case Key.F3 :
            case Key.F6 :
            case Key.F when Keyboard.Modifiers == ModifierKeys.Control :
                Keyboard.Focus(SearchControl.SearchTextBox);
                SearchControl.SearchTextBox.SelectAll();
                break;
            case Key.None :
                break;
            case Key.Cancel :
                break;
            case Key.Back :
                break;
            case Key.Tab :
                break;
            case Key.LineFeed :
                break;
            case Key.Clear :
                break;
            case Key.Enter :
                break;
            case Key.Pause :
                break;
            case Key.Capital :
                break;
            case Key.HangulMode :
                break;
            case Key.JunjaMode :
                break;
            case Key.FinalMode :
                break;
            case Key.HanjaMode :
                break;
            case Key.ImeConvert :
                break;
            case Key.ImeNonConvert :
                break;
            case Key.ImeAccept :
                break;
            case Key.ImeModeChange :
                break;
            case Key.Space :
                break;
            case Key.PageUp :
                break;
            case Key.Next :
                break;
            case Key.End :
                break;
            case Key.Home :
                break;
            case Key.Left :
                break;
            case Key.Up :
                break;
            case Key.Right :
                break;
            case Key.Down :
                break;
            case Key.Select :
                break;
            case Key.Print :
                break;
            case Key.Execute :
                break;
            case Key.PrintScreen :
                break;
            case Key.Insert :
                break;
            case Key.Delete :
                break;
            case Key.Help :
                break;
            case Key.D0 :
                break;
            case Key.D1 :
                break;
            case Key.D2 :
                break;
            case Key.D3 :
                break;
            case Key.D4 :
                break;
            case Key.D5 :
                break;
            case Key.D6 :
                break;
            case Key.D7 :
                break;
            case Key.D8 :
                break;
            case Key.D9 :
                break;
            case Key.A :
                break;
            case Key.B :
                break;
            case Key.C :
                break;
            case Key.D :
                break;
            case Key.E :
                break;
            case Key.G :
                break;
            case Key.H :
                break;
            case Key.I :
                break;
            case Key.J :
                break;
            case Key.K :
                break;
            case Key.L :
                break;
            case Key.M :
                break;
            case Key.N :
                break;
            case Key.O :
                break;
            case Key.P :
                break;
            case Key.Q :
                break;
            case Key.R :
                break;
            case Key.S :
                break;
            case Key.T :
                break;
            case Key.U :
                break;
            case Key.V :
                break;
            case Key.W :
                break;
            case Key.X :
                break;
            case Key.Y :
                break;
            case Key.Z :
                break;
            case Key.LWin :
                break;
            case Key.RWin :
                break;
            case Key.Apps :
                break;
            case Key.Sleep :
                break;
            case Key.NumPad0 :
                break;
            case Key.NumPad1 :
                break;
            case Key.NumPad2 :
                break;
            case Key.NumPad3 :
                break;
            case Key.NumPad4 :
                break;
            case Key.NumPad5 :
                break;
            case Key.NumPad6 :
                break;
            case Key.NumPad7 :
                break;
            case Key.NumPad8 :
                break;
            case Key.NumPad9 :
                break;
            case Key.Multiply :
                break;
            case Key.Add :
                break;
            case Key.Separator :
                break;
            case Key.Subtract :
                break;
            case Key.Decimal :
                break;
            case Key.Divide :
                break;
            case Key.F1 :
                break;
            case Key.F2 :
                break;
            case Key.F4 :
                break;
            case Key.F5 :
                break;
            case Key.F7 :
                break;
            case Key.F8 :
                break;
            case Key.F9 :
                break;
            case Key.F10 :
                break;
            case Key.F11 :
                break;
            case Key.F12 :
                break;
            case Key.F13 :
                break;
            case Key.F14 :
                break;
            case Key.F15 :
                break;
            case Key.F16 :
                break;
            case Key.F17 :
                break;
            case Key.F18 :
                break;
            case Key.F19 :
                break;
            case Key.F20 :
                break;
            case Key.F21 :
                break;
            case Key.F22 :
                break;
            case Key.F23 :
                break;
            case Key.F24 :
                break;
            case Key.NumLock :
                break;
            case Key.Scroll :
                break;
            case Key.LeftShift :
                break;
            case Key.RightShift :
                break;
            case Key.LeftCtrl :
                break;
            case Key.RightCtrl :
                break;
            case Key.LeftAlt :
                break;
            case Key.RightAlt :
                break;
            case Key.BrowserBack :
                break;
            case Key.BrowserForward :
                break;
            case Key.BrowserRefresh :
                break;
            case Key.BrowserStop :
                break;
            case Key.BrowserSearch :
                break;
            case Key.BrowserFavorites :
                break;
            case Key.BrowserHome :
                break;
            case Key.VolumeMute :
                break;
            case Key.VolumeDown :
                break;
            case Key.VolumeUp :
                break;
            case Key.MediaNextTrack :
                break;
            case Key.MediaPreviousTrack :
                break;
            case Key.MediaStop :
                break;
            case Key.MediaPlayPause :
                break;
            case Key.LaunchMail :
                break;
            case Key.SelectMedia :
                break;
            case Key.LaunchApplication1 :
                break;
            case Key.LaunchApplication2 :
                break;
            case Key.Oem1 :
                break;
            case Key.OemPlus :
                break;
            case Key.OemComma :
                break;
            case Key.OemMinus :
                break;
            case Key.OemPeriod :
                break;
            case Key.Oem2 :
                break;
            case Key.Oem3 :
                break;
            case Key.AbntC1 :
                break;
            case Key.AbntC2 :
                break;
            case Key.Oem4 :
                break;
            case Key.Oem5 :
                break;
            case Key.Oem6 :
                break;
            case Key.Oem7 :
                break;
            case Key.Oem8 :
                break;
            case Key.Oem102 :
                break;
            case Key.ImeProcessed :
                break;
            case Key.System :
                break;
            case Key.DbeAlphanumeric :
                break;
            case Key.DbeKatakana :
                break;
            case Key.DbeHiragana :
                break;
            case Key.DbeSbcsChar :
                break;
            case Key.DbeDbcsChar :
                break;
            case Key.DbeRoman :
                break;
            case Key.Attn :
                break;
            case Key.CrSel :
                break;
            case Key.DbeEnterImeConfigureMode :
                break;
            case Key.DbeFlushString :
                break;
            case Key.DbeCodeInput :
                break;
            case Key.DbeNoCodeInput :
                break;
            case Key.DbeDetermineString :
                break;
            case Key.DbeEnterDialogConversionMode :
                break;
            case Key.OemClear :
                break;
            case Key.DeadCharProcessed :
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => _collectionView.Refresh();

    private void Window_Loaded(object sender, RoutedEventArgs e) => Keyboard.Focus(SearchControl.SearchTextBox);

    private void Window_Closed(object sender, EventArgs e)
    {
        var newContent = InputHelp.ConvertToString(Bindings);

        if (_startupContent == newContent)
            return;

        if (App.InputConf.HasMenu)
            File.WriteAllText(App.InputConf.Path, App.InputConf.Content = newContent);
        else
        {
            newContent = InputHelp.ConvertToString(InputHelp.GetReducedBindings(Bindings));
            newContent = newContent.Replace(App.MenuSyntax + " ", "# ");
            File.WriteAllText(App.InputConf.Path, App.InputConf.Content = newContent);
        }

        Msg.ShowInfo(_("Changes will be available on next startup."));
    }

    private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Column.DisplayIndex == 1)
            e.Cancel = true;
    }

    private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (e.AddedCells.Count > 0)
            _focusedBinding = e.AddedCells[0].Item as Binding;
    }

    private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            e.Handled = true;

        switch (e.Key)
        {
            case Key.Left :
            case Key.Up :
            case Key.Right :
            case Key.Down :
            case Key.Tab :
                break;
            case Key.None :
            case Key.Cancel :
            case Key.Back :
            case Key.LineFeed :
            case Key.Clear :
            case Key.Enter :
            case Key.Pause :
            case Key.Capital :
            case Key.HangulMode :
            case Key.JunjaMode :
            case Key.FinalMode :
            case Key.HanjaMode :
            case Key.Escape :
            case Key.ImeConvert :
            case Key.ImeNonConvert :
            case Key.ImeAccept :
            case Key.ImeModeChange :
            case Key.Space :
            case Key.PageUp :
            case Key.Next :
            case Key.End :
            case Key.Home :
            case Key.Select :
            case Key.Print :
            case Key.Execute :
            case Key.PrintScreen :
            case Key.Insert :
            case Key.Delete :
            case Key.Help :
            case Key.D0 :
            case Key.D1 :
            case Key.D2 :
            case Key.D3 :
            case Key.D4 :
            case Key.D5 :
            case Key.D6 :
            case Key.D7 :
            case Key.D8 :
            case Key.D9 :
            case Key.A :
            case Key.B :
            case Key.C :
            case Key.D :
            case Key.E :
            case Key.F :
            case Key.G :
            case Key.H :
            case Key.I :
            case Key.J :
            case Key.K :
            case Key.L :
            case Key.M :
            case Key.N :
            case Key.O :
            case Key.P :
            case Key.Q :
            case Key.R :
            case Key.S :
            case Key.T :
            case Key.U :
            case Key.V :
            case Key.W :
            case Key.X :
            case Key.Y :
            case Key.Z :
            case Key.LWin :
            case Key.RWin :
            case Key.Apps :
            case Key.Sleep :
            case Key.NumPad0 :
            case Key.NumPad1 :
            case Key.NumPad2 :
            case Key.NumPad3 :
            case Key.NumPad4 :
            case Key.NumPad5 :
            case Key.NumPad6 :
            case Key.NumPad7 :
            case Key.NumPad8 :
            case Key.NumPad9 :
            case Key.Multiply :
            case Key.Add :
            case Key.Separator :
            case Key.Subtract :
            case Key.Decimal :
            case Key.Divide :
            case Key.F1 :
            case Key.F2 :
            case Key.F3 :
            case Key.F4 :
            case Key.F5 :
            case Key.F6 :
            case Key.F7 :
            case Key.F8 :
            case Key.F9 :
            case Key.F10 :
            case Key.F11 :
            case Key.F12 :
            case Key.F13 :
            case Key.F14 :
            case Key.F15 :
            case Key.F16 :
            case Key.F17 :
            case Key.F18 :
            case Key.F19 :
            case Key.F20 :
            case Key.F21 :
            case Key.F22 :
            case Key.F23 :
            case Key.F24 :
            case Key.NumLock :
            case Key.Scroll :
            case Key.LeftShift :
            case Key.RightShift :
            case Key.LeftCtrl :
            case Key.RightCtrl :
            case Key.LeftAlt :
            case Key.RightAlt :
            case Key.BrowserBack :
            case Key.BrowserForward :
            case Key.BrowserRefresh :
            case Key.BrowserStop :
            case Key.BrowserSearch :
            case Key.BrowserFavorites :
            case Key.BrowserHome :
            case Key.VolumeMute :
            case Key.VolumeDown :
            case Key.VolumeUp :
            case Key.MediaNextTrack :
            case Key.MediaPreviousTrack :
            case Key.MediaStop :
            case Key.MediaPlayPause :
            case Key.LaunchMail :
            case Key.SelectMedia :
            case Key.LaunchApplication1 :
            case Key.LaunchApplication2 :
            case Key.Oem1 :
            case Key.OemPlus :
            case Key.OemComma :
            case Key.OemMinus :
            case Key.OemPeriod :
            case Key.Oem2 :
            case Key.Oem3 :
            case Key.AbntC1 :
            case Key.AbntC2 :
            case Key.Oem4 :
            case Key.Oem5 :
            case Key.Oem6 :
            case Key.Oem7 :
            case Key.Oem8 :
            case Key.Oem102 :
            case Key.ImeProcessed :
            case Key.System :
            case Key.DbeAlphanumeric :
            case Key.DbeKatakana :
            case Key.DbeHiragana :
            case Key.DbeSbcsChar :
            case Key.DbeDbcsChar :
            case Key.DbeRoman :
            case Key.Attn :
            case Key.CrSel :
            case Key.DbeEnterImeConfigureMode :
            case Key.DbeFlushString :
            case Key.DbeCodeInput :
            case Key.DbeNoCodeInput :
            case Key.DbeDetermineString :
            case Key.DbeEnterDialogConversionMode :
            case Key.OemClear :
            case Key.DeadCharProcessed :
            default :
                ShowLearnWindow(_focusedBinding);
                break;
        }
    }

    private void DataGridCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ShowLearnWindow(_focusedBinding);

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            CommandColumn.MaxWidth = 800;
            CommandColumn.Width    = 800;
        }
        else
        {
            CommandColumn.MaxWidth = 322;
            CommandColumn.Width    = 322;
        }
    }
}
