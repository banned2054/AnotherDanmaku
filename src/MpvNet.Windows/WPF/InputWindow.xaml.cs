using MpvNet.Windows.UI;
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
    public           Theme?          Theme    => Theme.Current;

    private Binding? _focusedBinding;

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
