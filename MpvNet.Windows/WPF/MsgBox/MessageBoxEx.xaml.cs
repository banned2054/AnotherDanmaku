using MpvNet.Help;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MpvNet.Windows.WPF.MsgBox;

public sealed partial class MessageBoxEx
{
    #region INotifyPropertyChanged

    private bool _isModified;

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (value == _isModified) return;
            _isModified = true;
            NotifyPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged == null) return;
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        if (propertyName != "IsModified")
            IsModified = true;
    }

    #endregion INotifyPropertyChanged

    #region fields

    private string?             _message;
    private string?             _messageTitle;
    private MessageBoxButton?   _buttons;
    private MessageBoxButtonEx? _buttonsEx;
    private ImageSource?        _messageIcon;
    private MessageBoxImage     _msgBoxImage;
    private double              _buttonWidth;
    private bool                _expanded;
    private bool                _isDefaultOk;
    private bool                _isDefaultCancel;
    private bool                _isDefaultYes;
    private bool                _isDefaultNo;
    private bool                _isDefaultAbort;
    private bool                _isDefaultRetry;
    private bool                _isDefaultIgnore;
    private bool                _usingExButtons;

    #endregion fields

    #region properties

    public string Message
    {
        get => _message!;
        set
        {
            if (value == _message) return;
            _message = value;
            NotifyPropertyChanged();
        }
    }

    public string MessageTitle
    {
        get => _messageTitle!;
        set
        {
            if (value == _messageTitle) return;
            _messageTitle = value;
            NotifyPropertyChanged();
        }
    }

    public MessageBoxResult MessageResult { get; set; }

    public MessageBoxResultEx MessageResultEx { get; set; }

    public MessageBoxButton? Buttons
    {
        get => _buttons;
        set
        {
            if (value == _buttons) return;
            _buttons = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged("ShowOk");
            NotifyPropertyChanged("ShowCancel");
            NotifyPropertyChanged("ShowYes");
            NotifyPropertyChanged("ShowNo");
        }
    }

    public MessageBoxButtonEx? ButtonsEx
    {
        get => _buttonsEx;
        set
        {
            if (value == _buttonsEx) return;
            _buttonsEx = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged("ShowOk");
            NotifyPropertyChanged("ShowCancel");
            NotifyPropertyChanged("ShowYes");
            NotifyPropertyChanged("ShowNo");
            NotifyPropertyChanged("ShowAbort");
            NotifyPropertyChanged("ShowRetry");
            NotifyPropertyChanged("ShowIgnore");
        }
    }

    public ImageSource? MessageIcon
    {
        get => _messageIcon;
        set
        {
            if (value == _messageIcon) return;
            _messageIcon = value;
            NotifyPropertyChanged();
        }
    }

    public double ButtonWidth
    {
        get => _buttonWidth;
        set
        {
            if (value == _buttonWidth) return;
            _buttonWidth = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultOk
    {
        get => _isDefaultOk;
        set
        {
            if (value == _isDefaultOk) return;
            _isDefaultOk = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultCancel
    {
        get => _isDefaultCancel;
        set
        {
            if (value == _isDefaultCancel) return;
            _isDefaultCancel = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultYes
    {
        get => _isDefaultYes;
        set
        {
            if (value == _isDefaultYes) return;
            _isDefaultYes = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultNo
    {
        get => _isDefaultNo;
        set
        {
            if (value == _isDefaultNo) return;
            _isDefaultNo = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultAbort
    {
        get => _isDefaultAbort;
        set
        {
            if (value == _isDefaultAbort) return;
            _isDefaultAbort = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultRetry
    {
        get => _isDefaultRetry;
        set
        {
            if (value == _isDefaultRetry) return;
            _isDefaultRetry = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultIgnore
    {
        get => _isDefaultIgnore;
        set
        {
            if (value == _isDefaultIgnore) return;
            _isDefaultIgnore = value;
            NotifyPropertyChanged();
        }
    }

    #endregion properties

    #region constructors

    private MessageBoxEx()
    {
        InitializeComponent();
        DataContext = this;
        LargestButtonWidth();
    }

    public MessageBoxEx(string          msg, string title, MessageBoxButton buttons = MessageBoxButton.OK,
                        MessageBoxImage image = MessageBoxImage.None)
    {
        InitializeComponent();
        DataContext = this;
        Init(msg, title, buttons, image);
    }

    public MessageBoxEx(string          msg, string title, MessageBoxButtonEx buttons = MessageBoxButtonEx.OK,
                        MessageBoxImage image = MessageBoxImage.None)
    {
        InitializeComponent();
        DataContext = this;
        Init(msg, title, buttons, image);
    }

    #endregion constructors

    #region non-static methods

    private void Init(string msg, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        InitTop(msg, title);
        _usingExButtons = false;
        ButtonsEx       = null;
        Buttons         = buttons;
        SetButtonTemplates();
        InitBottom(image);
        FindDefaultButton(staticButtonDefault);
    }

    private void Init(string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
    {
        InitTop(msg, title);
        _usingExButtons = true;
        Buttons         = null;
        ButtonsEx       = buttons;
        SetButtonTemplates();
        InitBottom(image);
        FindDefaultButtonEx(staticButtonDefault);
    }

    private void InitTop(string msg, string title)
    {
        // determine whether or not to show the details pane and checkbox
        ShowDetailsBtn = (string.IsNullOrEmpty(DetailsText)) ? Visibility.Collapsed : Visibility.Visible;
        ShowCheckBox   = (CheckBoxData == null) ? Visibility.Collapsed : Visibility.Visible;

        // Well, the binding for family/size don't appear to be working, so I have to set them 
        // manually. Weird...
        FontFamily = MsgFontFamily;
        FontSize   = MsgFontSize;
        LargestButtonWidth();

        // configure the form based on specified criteria
        Message      = msg;
        MessageTitle = (string.IsNullOrEmpty(title.Trim())) ? "Application Message" : title;

        // url (if specified)
        if (Url == null) return;
        tbUrl.Text    = (string.IsNullOrEmpty(UrlDisplayName)) ? Url.ToString() : UrlDisplayName;
        tbUrl.ToolTip = new ToolTip() { Content = Url.ToString() };
    }

    private void InitBottom(MessageBoxImage image)
    {
        MessageBackground ??= new SolidColorBrush(Colors.White);
        MessageForeground ??= new SolidColorBrush(Colors.Black);
        ButtonBackground  ??= new SolidColorBrush(ColorFromString("#cdcdcd"));

        MessageIcon = null;

        _msgBoxImage = image;

        if (DelegateObj != null)
        {
            var style = (Style)(FindResource("ImageOpacityChanger"));

            if (style != null)
            {
                imgMsgBoxIcon.Style = style;

                if (!string.IsNullOrEmpty(DelegateToolTip))
                {
                    var tooltip = new ToolTip()
                        { Content = DelegateToolTip };
                    // for some reason, Image elements can't do tooltips, so I assign the tootip 
                    // to the parent grid. This seems to work fine.
                    imgGrid.ToolTip = tooltip;
                }
            }
        }

        // multiple images have the same ordinal value, and are indicated in the comments below. 
        // WTF Microsoft? 
        switch ((int)image)
        {
            case 16 : // MessageBoxImage.Error, MessageBoxImage.Stop, MessageBox.Image.Hand
            {
                MessageIcon = GetIcon(SystemIcons.Error);

                if (!isSilent)
                    SystemSounds.Hand.Play();
            }
                break;

            case 64 : // MessageBoxImage.Information, MessageBoxImage.Asterisk 
            {
                MessageIcon = GetIcon(SystemIcons.Information);

                if (!isSilent)
                    SystemSounds.Asterisk.Play();
            }
                break;

            case 32 : // MessageBoxImage.Question
            {
                MessageIcon = GetIcon(SystemIcons.Question);

                if (!isSilent)
                    SystemSounds.Question.Play();
            }
                break;

            case 48 : // MessageBoxImage.Warning, MessageBoxImage.Exclamation
            {
                MessageIcon = GetIcon(SystemIcons.Warning);

                if (!isSilent)
                    SystemSounds.Exclamation.Play();
            }
                break;

            default :
                MessageIcon = null;
                break;
        }
    }

    public ImageSource GetIcon(Icon icon)
    {
        var image = Imaging.CreateBitmapSourceFromHIcon(
                                                        icon.Handle, Int32Rect.Empty,
                                                        BitmapSizeOptions.FromEmptyOptions());
        return image;
    }

    private void CenterInScreen()
    {
        var width  = ActualWidth;
        var height = ActualHeight;
        Left = (SystemParameters.WorkArea.Width  - width)  / 2 + SystemParameters.WorkArea.Left;
        Top  = (SystemParameters.WorkArea.Height - height) / 2 + SystemParameters.WorkArea.Top;
    }

    private void LargestButtonWidth()
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        var    panel = (StackPanel)stackButtons.Child;
        double width = 0;
        foreach (Button button in panel.Children)
        {
            // Using the FormattedText object 
            // will strip whitespace before measuring the text, so we convert spaces to double 
            // hyphens to compensate (I like to pad button Content with a leading and trailing 
            // space) so that the button is wide enough to present a more padded appearance.
            var formattedText = new FormattedText(
                                                  (button.Name == "btnDetails")
                                                      ? "--Details--"
                                                      : ((string)(button.Content)).Replace(" ", "--"),
                                                  CultureInfo.CurrentUICulture,
                                                  FlowDirection.LeftToRight,
                                                  typeface,
                                                  FontSize = FontSize,
                                                  System.Windows.Media.Brushes.Black,
                                                  VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (width < formattedText.Width)
            {
            }

            width = Math.Max(width, formattedText.Width);
        }

        ButtonWidth = Math.Ceiling(width /*width + polyArrow.Width+polyArrow.Margin.Right+Margin.Left*/);
    }

    private void SetButtonTemplates()
    {
        // set the button template (if specified)
        if (string.IsNullOrEmpty(ButtonTemplateName)) return;
        var foundResource = true;

        try
        {
            FindResource(ButtonTemplateName);
        }
        catch (Exception)
        {
            foundResource = false;
        }

        if (!foundResource) return;
        btnOK.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnYes.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnNo.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnCancel.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnAbort.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnRetry.SetResourceReference(TemplateProperty, ButtonTemplateName);
        btnIgnore.SetResourceReference(TemplateProperty, ButtonTemplateName);
    }

    private void FindDefaultButtonEx(MessageBoxButtonDefault buttonDefault)
    {
        // determine default button
        IsDefaultOk     = false;
        IsDefaultCancel = false;
        IsDefaultYes    = false;
        IsDefaultNo     = false;
        IsDefaultAbort  = false;
        IsDefaultRetry  = false;
        IsDefaultIgnore = false;

        if (buttonDefault == MessageBoxButtonDefault.None) return;
        switch (ButtonsEx)
        {
            case MessageBoxButtonEx.OK :
                IsDefaultOk = true;
                break;
            case MessageBoxButtonEx.OKCancel :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultOk = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultCancel = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    default :
                        IsDefaultOk = true;
                        break;
                }
            }
                break;
            case MessageBoxButtonEx.YesNoCancel :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Yes :
                        break;
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultYes = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.No :
                        IsDefaultNo = true;
                        break;
                    case MessageBoxButtonDefault.Button3 :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultCancel = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    default :
                        IsDefaultYes = true;
                        break;
                }
            }
                break;
            case MessageBoxButtonEx.YesNo :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Yes :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultYes = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.No :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultNo = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    default :
                        IsDefaultYes = true;
                        break;
                }
            }
                break;
            case MessageBoxButtonEx.RetryCancel :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Retry :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultRetry = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultCancel = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.Yes :
                    case MessageBoxButtonDefault.No :
                    case MessageBoxButtonDefault.Abort :
                    case MessageBoxButtonDefault.Ignore :
                    case MessageBoxButtonDefault.Button3 :
                    case MessageBoxButtonDefault.None :
                    default :
                        IsDefaultRetry = true;
                        break;
                }
            }
                break;
            case MessageBoxButtonEx.AbortRetryIgnore :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Abort :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultAbort = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.Retry :
                        IsDefaultRetry = true;
                        break;
                    case MessageBoxButtonDefault.Button3 :
                    case MessageBoxButtonDefault.Ignore :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultIgnore = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.Yes :
                    case MessageBoxButtonDefault.No :
                    default :
                        IsDefaultAbort = true;
                        break;
                }
            }
                break;
            case null :
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    private void FindDefaultButton(MessageBoxButtonDefault buttonDefault)
    {
        // determine default button
        IsDefaultOk     = false;
        IsDefaultCancel = false;
        IsDefaultYes    = false;
        IsDefaultNo     = false;
        IsDefaultAbort  = false;
        IsDefaultRetry  = false;
        IsDefaultIgnore = false;

        if (buttonDefault == MessageBoxButtonDefault.None) return;
        switch (Buttons)
        {
            case MessageBoxButton.OK :
                IsDefaultOk = true;
                break;
            case MessageBoxButton.OKCancel :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultOk = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultCancel = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    default :
                        IsDefaultOk = true;
                        break;
                }
            }
                break;
            case MessageBoxButton.YesNoCancel :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Yes :
                        break;
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultYes = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.No :
                        IsDefaultNo = true;
                        break;
                    case MessageBoxButtonDefault.Button3 :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultCancel = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.Abort :
                    case MessageBoxButtonDefault.Retry :
                    case MessageBoxButtonDefault.Ignore :
                    default :
                        IsDefaultYes = true;
                        break;
                }
            }
                break;
            case MessageBoxButton.YesNo :
            {
                switch (buttonDefault)
                {
                    case MessageBoxButtonDefault.Button1 :
                    case MessageBoxButtonDefault.Yes :
                    case MessageBoxButtonDefault.MostPositive :
                        IsDefaultYes = true;
                        break;
                    case MessageBoxButtonDefault.Button2 :
                    case MessageBoxButtonDefault.No :
                    case MessageBoxButtonDefault.LeastPositive :
                        IsDefaultNo = true;
                        break;
                    case MessageBoxButtonDefault.Forms :
                    case MessageBoxButtonDefault.OK :
                    case MessageBoxButtonDefault.Cancel :
                    case MessageBoxButtonDefault.Abort :
                    case MessageBoxButtonDefault.Retry :
                    case MessageBoxButtonDefault.Ignore :
                    case MessageBoxButtonDefault.Button3 :
                    default :
                        IsDefaultYes = true;
                        break;
                }
            }
                break;
            case null :
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion non-static methods

    #region event handlers

    #region buttons

    /// <summary>
    /// Handle the click event for the OK button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnOK_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.OK;
        MessageResultEx = MessageBoxResultEx.OK;
        DialogResult    = true;
    }

    /// <summary>
    /// Handle the click event for the Yes button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.Yes;
        MessageResultEx = MessageBoxResultEx.Yes;
        DialogResult    = true;
    }

    /// <summary>
    /// Handle the click event for the No button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.No;
        MessageResultEx = MessageBoxResultEx.No;
        DialogResult    = true;
    }

    private void BtnAbort_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.None;
        MessageResultEx = MessageBoxResultEx.Abort;
        DialogResult    = true;
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.None;
        MessageResultEx = MessageBoxResultEx.Retry;
        DialogResult    = true;
    }

    private void BtnIgnore_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.None;
        MessageResultEx = MessageBoxResultEx.Ignore;
        DialogResult    = true;
    }

    /// <summary>
    /// Handle the click event for the Cancel button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        MessageResult   = MessageBoxResult.Cancel;
        MessageResultEx = MessageBoxResultEx.Cancel;
        DialogResult    = true;
    }

    #endregion buttons

    private void NotifiableWindow_SizeChanged(object sender, SizeChangedEventArgs e) => CenterInScreen();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        imgMsgBoxIcon.ToolTip = (_msgBoxImage == MessageBoxImage.Error) ? MsgBoxIconToolTip : null;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        DetailsText         = null;
        CheckBoxData        = null;
        staticButtonDefault = MessageBoxButtonDefault.Forms;

        if (MessageResult != MessageBoxResult.None) return;
        if (_usingExButtons)
        {
            switch (ButtonsEx)
            {
                case MessageBoxButtonEx.OK :
                    MessageResultEx = MessageBoxResultEx.OK;
                    break;
                case MessageBoxButtonEx.YesNoCancel :
                case MessageBoxButtonEx.OKCancel :
                case MessageBoxButtonEx.RetryCancel :
                case MessageBoxButtonEx.AbortRetryIgnore :
                    MessageResultEx = MessageBoxResultEx.Cancel;
                    break;
                case MessageBoxButtonEx.YesNo :
                    MessageResultEx = MessageBoxResultEx.No;
                    break;
                case null :
                    break;
                default :
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            switch (Buttons)
            {
                case MessageBoxButton.OK :
                    MessageResult = MessageBoxResult.OK;
                    break;
                case MessageBoxButton.YesNoCancel :
                case MessageBoxButton.OKCancel :
                    MessageResult = MessageBoxResult.Cancel;
                    break;
                case MessageBoxButton.YesNo :
                    MessageResult = MessageBoxResult.No;
                    break;
                case null :
                    break;
                default :
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void ImgMsgBoxIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DelegateObj == null || _msgBoxImage != MessageBoxImage.Error || Buttons != MessageBoxButton.OK) return;
        DelegateObj.PerformAction(Message);

        if (!ExitAfterErrorAction) return;
        MessageResult = MessageBoxResult.None;
        DialogResult  = true;
    }

    private void TbUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ProcessHelp.ShellExecute(Url?.ToString());

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool EnableMenuItem(IntPtr hMenu, uint uIdEnableItem, uint uEnable);

    private const uint MfByCommand = 0x00000000;
    private const uint MfGrayed    = 0x00000001;
    private const uint ScClose     = 0xF060;

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        if (enableCloseButton) return;
        var hWnd    = new WindowInteropHelper(this);
        var sysMenu = GetSystemMenu(hWnd.Handle, false);
        EnableMenuItem(sysMenu, ScClose, MfByCommand | MfGrayed);
    }

    #endregion event handlers
}
