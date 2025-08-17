using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Path = System.IO.Path;
using ZapretDesktop.Properties;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SetForegroundWindow(IntPtr hwnd);
        
        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        private ContextMenu _notifyContextMenu = null!;
        private bool _isExit;
        private StrategyButton? _selectedStrategy;
        private ListButton? _selectedList;
        
        public StrategyButton? SelectedStrategy 
        { 
            get => _selectedStrategy;
            set => _selectedStrategy = value;
        }
        
        public ListButton? SelectedList
        {
            get => _selectedList; 
            set => _selectedList = value;
        }
        
        public MainWindow()
        {
            App.CurrentMainWindow = this;
            InitializeComponent();
            
            ReadStrategyFiles();
            ReadListFiles();
            if (Strategies.Children.Count > 0 && Strategies.Children[0] is StrategyButton firstStrategy)
            {
                firstStrategy.StrategyButtonBody.IsChecked = true;
            }
            else
            {
                ConnectButton.IsChecked = null;
            }
            UpdateSelectedStrategyNameButton();
            
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.Text = Title;
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        public void RedrawNotifyIcon()
        {
            if (TryFindResource("AppIcon") is not ImageSource icon)
                return;
            var uri = new Uri(icon.ToString());
            var streamInfo = Application.GetResourceStream(uri);
            if (streamInfo is null)
                return;
            using var stream = streamInfo.Stream;
            _notifyIcon.Icon = new System.Drawing.Icon(stream);
        }
        
        public void ReadStrategyFiles()
        {
            Strategies.Children.Clear();
            var files = Directory.GetFiles($@"{Settings.Default.StrategyPath}\", "*.bat").ToList();
            try
            {
                var serviceFile = $@"{Settings.Default.StrategyPath}\service.bat";
                var serviceFileContent = File.ReadAllLines(serviceFile);
                foreach (var line in serviceFileContent)
                {
                    var match = Regex.Match(line, "set\\s+\"LOCAL_VERSION=(.*?)\"");
                    if (!match.Success)
                        continue;
                    Settings.Default.ZapretVersion = match.Groups[1].Value;
                    Settings.Default.Save();
                    Title = $"Zapret {Settings.Default.ZapretVersion}";
                    break;
                }
                files.Remove(serviceFile);
            }
            catch (FileNotFoundException) { }
            foreach (var file in files)
            {
                var cuttingStartIndex = file.LastIndexOf('\\') + 1;
                var btn = new StrategyButton()
                {
                    StrategyFileName = file[cuttingStartIndex..],
                    StrategyName = { Text = file[cuttingStartIndex..^4] }
                };
                Strategies.Children.Add(btn);
            }
        }

        public void ReadListFiles()
        {
            Lists.Children.Clear();
            try
            {
                var listFiles = Directory.GetFiles($@"{Settings.Default.ListPath}\", "*.txt").ToList();
                foreach (var file in listFiles)
                {
                    var cuttingStartIndex = file.LastIndexOf('\\') + 1;
                    var btn = new ListButton()
                    {
                        ListFileName = file[cuttingStartIndex..],
                        ListName = { Text = file[cuttingStartIndex..^4] }
                    };
                    Lists.Children.Add(btn);
                }
            }
            catch (DirectoryNotFoundException) { }
        }
        
        private void ExitApplication()
        {
            _isExit = true;
            _notifyIcon.Dispose();
            KillWinws();
            Close();
        }

        private void CreateContextMenu()
        {
            _notifyContextMenu = new ContextMenu()
            {
                Placement = PlacementMode.AbsolutePoint,
                StaysOpen = false,
                Focusable = true
            };
            var connectionInfoItem = new MenuItem() { IsHitTestVisible = false };
            var connectItem = new MenuItem()
            {
                Header = $"{TryFindResource("ConnectButtonUncheckedText")}",
                Foreground = new SolidColorBrush(Color.FromRgb(83, 197, 94)),
                Icon = new Image() { Height = 14, Source = Application.Current.Resources["ConnectIcon"] as DrawingImage }
            };
            var disconnectItem = new MenuItem()
            {
                Header = $"{TryFindResource("ConnectButtonCheckedText")}",
                Foreground = Brushes.Red,
                Icon = new Image() { Height = 14, Source = Application.Current.Resources["DisconnectIcon"] as DrawingImage },
                IsEnabled = false
            };
            connectItem.Click += (_, _) =>
            {
                ConnectButton.IsChecked = true;
                connectItem.IsEnabled = false;
                disconnectItem.IsEnabled = true;
            };
            disconnectItem.Click += (_, _) =>
            {
                ConnectButton.IsChecked = false;
                disconnectItem.IsEnabled = false;
                connectItem.IsEnabled = true;
            };
            var openItem = new MenuItem()
            {
                Header = $"{TryFindResource("OpenMenuItemText")}",
                Icon = new Image() { Height = 12, Source = Application.Current.Resources["OpenIcon"] as DrawingImage }
            };
            openItem.Click += (_, _) => ShowWindow();
            var closeItem = new MenuItem()
            {
                Header = $"{TryFindResource("ExitMenuItemText")}",
                Icon = new Image()
                {
                    Height = 14, 
                    Margin = new Thickness(4, 0, 0, 0),
                    Source = Application.Current.Resources["ExitIcon"] as DrawingImage
                }
            };
            closeItem.Click += (_, _) => ExitApplication();

            _notifyContextMenu.Items.Add(connectionInfoItem);
            _notifyContextMenu.Items.Add(connectItem);
            _notifyContextMenu.Items.Add(disconnectItem);
            _notifyContextMenu.Items.Add(openItem);
            _notifyContextMenu.Items.Add(closeItem);
        }

        public void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    ShowWindow();
                    break;
                case System.Windows.Forms.MouseButtons.Right:
                    CreateContextMenu();
                    ShowContextMenu();
                    break;
            }
        }
        
        private void ShowContextMenu()
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(App.CurrentMainWindow).Handle);
            if (source?.CompositionTarget is null)
                return;
            var transform = source.CompositionTarget.TransformFromDevice;
            var scaledPoint = transform.Transform
                (new Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y));
            if (_notifyContextMenu.Items[0] is MenuItem menuItem)
            {
                if (Strategies.Children.Count > 0)
                {
                    menuItem.Visibility = Visibility.Visible;
                    if (_selectedStrategy is not null)
                    {
                        menuItem.Header = $"{_selectedStrategy.StrategyName.Text}: {ConnectionStatus.Text}";
                    }
                }
                else
                {
                    menuItem.Visibility = Visibility.Collapsed;
                }
            }

            if (_notifyContextMenu.Items[1] is MenuItem connectItem &&
                _notifyContextMenu.Items[2] is MenuItem disconnectItem)
            {
                switch (ConnectButton.IsChecked)
                {
                    case true:
                        connectItem.IsEnabled = false;
                        disconnectItem.IsEnabled = _selectedList is null;
                        break;
                    case false:
                        connectItem.IsEnabled = _selectedList is null;
                        disconnectItem.IsEnabled = false;
                        break;
                    case null:
                        connectItem.IsEnabled = false;
                        disconnectItem.IsEnabled = false;
                        break;
                }
            }
            
            _notifyContextMenu.IsOpen = true;
            _notifyContextMenu.HorizontalOffset = scaledPoint.X - 12;
            _notifyContextMenu.VerticalOffset = scaledPoint.Y - _notifyContextMenu.ActualHeight + 16;
            var scaleTransform = 
                (ScaleTransform)_notifyContextMenu.Template.FindName("PART_BorderScale", _notifyContextMenu);
            scaleTransform.CenterY = _notifyContextMenu.ActualHeight;
            if (PresentationSource.FromVisual(_notifyContextMenu) is HwndSource hwndSource)
            {
                _ = SetForegroundWindow(hwndSource.Handle);
            }
            _notifyContextMenu.Focus();
        }

        private void Window_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isExit)
                return;
            Hide();
            e.Cancel = true;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            switch (Settings.Default.IsDarkMode)
            {
                case true:
                    App.UnhookThemeListener();
                    App.DarkModeSwitch(true);
                    break;
                case false:
                    App.UnhookThemeListener();
                    App.DarkModeSwitch(false);
                    break;
                case null:
                    App.HookThemeListener();
                    break;
            }
            App.LanguageSwitch(Settings.Default.Language);
            Activate();
        }

        public void UpdateSelectedStrategyNameButton()
        {
            if (Strategies.Children.Count > 0 && _selectedStrategy is not null)
            {
                SelectedStrategyNameText.ClearValue(TextBlock.FontFamilyProperty);
                SelectedStrategyNameText.Text = _selectedStrategy.StrategyName.Text;
            }
            else
            {
                SelectedStrategyNameText.FontFamily = new FontFamily("Verdana");
                SelectedStrategyNameText.Text = ":/";
            }
        }
        
        private void HideButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectedStrategyNameButton();
        }

        public async void ConnectButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_selectedStrategy is null)
                return;
            ConnectionStatus.SetResourceReference(TextBlock.TextProperty, "ConnectionStatusConnectingText");
            foreach (StrategyButton btn in Strategies.Children)
            {
                if (btn != _selectedStrategy)
                {
                    btn.IsEnabled = false;
                }
            }
            AddButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;
            SettingsButtonClone.IsEnabled = false;
            ConnectButton.IsHitTestVisible = false;
            bool isSuccess = await RunWinws(_selectedStrategy);
            ConnectButton.IsHitTestVisible = true;
            if (isSuccess)
            {
                ConnectionStatus.SetResourceReference(TextBlock.TextProperty, "ConnectionStatusConnectedText");
            }
            else
            {
                ConnectButton.IsChecked = false;
            }
        }

        private async Task<bool> RunWinws(StrategyButton strategy)
        {
            await Task.Delay(1000);
            var errorPopup = Application.Current.Resources["ErrorPopup"] as CustomPopup;
            var strategyPath = $@"{Settings.Default.StrategyPath}\{strategy.StrategyFileName}";
            
            var batPath = Path.GetFullPath(strategyPath);
            if (!File.Exists(batPath))
            {
                var text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                           $"{strategyPath} " +
                           $"{TryFindResource("ErrorPopupFileNotFoundMessageText")}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
                return false;
            }
            var binPath = Path.GetFullPath(Settings.Default.BinPath);
            var listsPath = Settings.Default.ListPath;
            var lines = File.ReadAllLines(batPath);
            // searching winws.exe run line
            var startIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("winws.exe"))
                    continue;
                startIndex = i;
                break;
            }
            if (startIndex == -1)
            {
                var text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                           $"{strategy.StrategyFileName} " +
                           $"{TryFindResource("ErrorPopupIncorrectFileMessageText")}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
                return false;
            }
            // combine lines with ^ into one
            var fullLines = new List<string>();
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                fullLines.Add(line);
                if (!line.EndsWith("^")) break;
            }
            var combined = string.Join(" ", fullLines.ToArray()).Replace("^", "").Trim();
            // searching winws.exe arguments
            var match = Regex.Match(combined, @"winws\.exe""?\s+(.*)");
            if (!match.Success)
            {
                var text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                           $"{strategy.StrategyFileName} " +
                           $"{TryFindResource("ErrorPopupIncorrectFileMessageText")}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
                return false;
            }

            string args = match.Groups[1].Value;
            args = args.Replace("%BIN%", binPath + Path.DirectorySeparatorChar);
            args = args.Replace("%LISTS%", listsPath + Path.DirectorySeparatorChar);
            args = args.Replace("%GameFilter%", "1024-65535").Replace("%%GameFilter%%", "1024-65535");
            var exePath = Path.Combine(Path.GetFullPath(Settings.Default.BinPath), "winws.exe");
            if (!File.Exists(exePath))
            {
                var text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                           $"winws.exe {TryFindResource("ErrorPopupFileNotFoundMessageText")}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
                return false;
            }
            
            var proc = new Process();
            proc.StartInfo.FileName = exePath;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                proc.Start();
                return true;
            }
            catch (Exception ex)
            {
                var text = $"{TryFindResource("ErrorPopupWinwsRunFailedMessageText")}: {ex.Message}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
                return false;
            }
        }

        private void KillWinws()
        {
            foreach (var proc in Process.GetProcessesByName("winws"))
            {
                proc.Kill();
                proc.WaitForExit();
            }
        }

        private void ConnectButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            KillWinws();
            foreach (StrategyButton btn in Strategies.Children)
            {
                if (btn != _selectedStrategy)
                {
                    btn.IsEnabled = true;
                }
            }
            AddButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
            SettingsButtonClone.IsEnabled = true;
            ConnectionStatus.SetResourceReference(TextBlock.TextProperty, "ConnectionStatusDisconnectedText");
        }

        public void BackgroundBlurOff()
        {
            BgBlur.Radius = 0;
            BgDark.Visibility = Visibility.Collapsed;
        }

        public void BackgroundBlurOn()
        {
            BgBlur.Radius = 15;
            BgDark.Visibility = Visibility.Visible;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Multiselect = false,
                Title = TryFindResource("SelectFileText").ToString(),
                Filter = $"{TryFindResource("BatchFileText")} (*.bat)|*.bat"
            };
            if (ofd.ShowDialog() != true)
                return;
            var fileName = ofd.FileName[(ofd.FileName.LastIndexOf('\\') + 1)..];
            var filePath = $"{Settings.Default.StrategyPath}\\{fileName}";
            try
            {
                File.Copy(ofd.FileName, filePath, false);
                var btn = new StrategyButton()
                {
                    StrategyFileName = fileName,
                    StrategyName = { Text = fileName[..^4] }
                };
                Strategies.Children.Add(btn);
                btn.StrategyButtonBody.IsChecked = true;
                ConnectButton.IsChecked = false;
            }
            catch (IOException)
            {
                var errorPopup = TryFindResource("ErrorPopup") as CustomPopup;
                var text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                           $"{fileName} {TryFindResource("ErrorPopupFileAlreadyExistsMessageText")}";
                if (errorPopup?.PopupContent is TextBlock textBlock)
                {
                    textBlock.Text = text;
                    errorPopup.Show(PopupGrid);
                }
                else
                {
                    MessageBox.Show(text);
                }
            }
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryFindResource("SettingsPopup") is not CustomPopup settingsPopup)
                return;
            settingsPopup.Show(PopupGrid);
        }
    }
}
