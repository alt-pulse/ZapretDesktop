using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Reflection;
using ZapretDesktop.Properties;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using ToolTip = System.Windows.Controls.ToolTip;
using Path = System.IO.Path;

namespace ZapretDesktop;

public partial class SettingsControl
{
    public SettingsControl()
    {
        InitializeComponent();
    }
    
    private int? MyIndexOf(string str, char symbol, int number)
    {
        int matchNumber = 0;
        int? index = null;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == symbol)
            {
                matchNumber++;
                if (matchNumber == number)
                {
                    index = i;
                    break;
                }
            }
        }
        return index;
    }
    
    private string GetShortenedPath(string path)
    {
        if (path.Count(c => c == '\\') < 4)
            return path;
        int? firstIndex = MyIndexOf(path, '\\', 2);
        int? preLastIndex = MyIndexOf(path, '\\', path.Count(c => c == '\\') - 1);
        if (!firstIndex.HasValue || !preLastIndex.HasValue)
            return path;
        var firstPart = path[..(firstIndex.Value + 1)];
        var lastPart = path[preLastIndex.Value..];
        return $"{firstPart}...{lastPart}";
    }
    
    private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        switch (Settings.Default.IsDarkMode)
        {
            case false:
                ThemeSelectorTextBlock.Text = "Светлая";
                break;
            case true:
                ThemeSelectorTextBlock.Text = "Темная";
                break;
            case null:
                ThemeSelectorTextBlock.Text = "Система";
                break;
        }
        SetPath(StrategyFolder, Settings.Default.StrategyPath);
        SetPath(ListFolder, Settings.Default.ListPath);
        SetPath(BinFolder, Settings.Default.BinPath);
        ZapretVersion.Text = Settings.Default.ZapretVersion;
        AppVersion.Text = Assembly.GetExecutingAssembly().
            GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    }

    private void SetPath(TextBlock textBlock, string path)
    {
        var fullPath = Path.GetFullPath(path);
        textBlock.Text = GetShortenedPath(fullPath);
        if (textBlock.ToolTip is ToolTip toolTip)
        {
            toolTip.Content = fullPath;
        }
    }
    
    private void StrategyFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folderBrowserDialog = new FolderBrowserDialog();
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;
        Settings.Default.StrategyPath = folderBrowserDialog.SelectedPath;
        Settings.Default.Save();
        SetPath(StrategyFolder, Settings.Default.StrategyPath);
        App.CurrentMainWindow.ReadStrategyFiles();
        App.CurrentMainWindow.BackgroundBlurOn();
        if (App.CurrentMainWindow.Strategies.Children.Count > 0 &&
            App.CurrentMainWindow.Strategies.Children[0] is StrategyButton btn)
        {
            App.CurrentMainWindow.ConnectButton.IsChecked = false;
            btn.StrategyButtonBody.IsChecked = true;
        }
        else
        {
            App.CurrentMainWindow.ConnectButton.IsChecked = null;
        }
        App.CurrentMainWindow.UpdateSelectedStrategyNameButton();
    }

    private void ListFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folderBrowserDialog = new FolderBrowserDialog();
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;
        Settings.Default.ListPath = folderBrowserDialog.SelectedPath;
        Settings.Default.Save();
        SetPath(ListFolder, Settings.Default.ListPath);
        App.CurrentMainWindow.ReadListFiles();
        App.CurrentMainWindow.BackgroundBlurOn();
    }

    private void BinFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folderBrowserDialog = new FolderBrowserDialog();
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;
        Settings.Default.BinPath = folderBrowserDialog.SelectedPath;
        Settings.Default.Save();
        SetPath(BinFolder, Settings.Default.BinPath);
    }
    
    private ContextMenu CreateContextMenu(Button placementTarget, List<string> menuItemsHeaders)
    {
        var menu = new ContextMenu()
        {
            PlacementTarget = placementTarget,
            Placement = PlacementMode.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        foreach (var header in menuItemsHeaders)
        {
            var menuItem = new MenuItem() { HorizontalContentAlignment = HorizontalAlignment.Right };
            menuItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, header);
            menuItem.Click += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (placementTarget.Content is StackPanel stackPanel && 
                        stackPanel.Children[0] is TextBlock textBlock)
                    {
                        textBlock.Text = menuItem.Header.ToString();
                    }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
            if (placementTarget.Content is StackPanel stackPanel && 
                stackPanel.Children[0] is TextBlock textBlock && 
                textBlock.Text == menuItem.Header.ToString())
            {
                menuItem.IsEnabled = false;
            }
            menu.Items.Add(menuItem);
        }
        menu.IsOpen = true;
        return menu;
    }
    
    private void ThemeSelector_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        var menu = CreateContextMenu(btn, 
            ["LightThemeMenuItemText", "DarkThemeMenuItemText", "SystemThemeMenuItemText"]);
        if (menu.Items[0] is MenuItem menuItemLight)
        {
            menuItemLight.Click += (_, _) =>
            {
                App.UnhookThemeListener();
                App.DarkModeSwitch(false);
                Settings.Default.Save();
            };
        }
        if (menu.Items[1] is MenuItem menuItemDark)
        {
            menuItemDark.Click += (_, _) =>
            {
                App.UnhookThemeListener();
                App.DarkModeSwitch(true);
                Settings.Default.Save();
            };
        }
        if (menu.Items[2] is MenuItem menuItemSystem)
        {
            menuItemSystem.Click += (_, _) =>
            {
                App.HookThemeListener();
                Settings.Default.Save();
            };
        }
    }

    private void UpdateThemeSelector()
    {
        switch (Settings.Default.IsDarkMode)
        {
            case false:
                ThemeSelectorTextBlock.SetResourceReference(TextBlock.TextProperty, "LightThemeMenuItemText");
                break;
            case true:
                ThemeSelectorTextBlock.SetResourceReference(TextBlock.TextProperty, "DarkThemeMenuItemText");
                break;
            case null:
                ThemeSelectorTextBlock.SetResourceReference(TextBlock.TextProperty, "SystemThemeMenuItemText");
                break;
        }
    }

    private void UpdateLanguageSelector()
    {
        switch (Settings.Default.Language)
        {
            case "en":
                LanguageSelectorTextBlock.SetResourceReference(TextBlock.TextProperty, "EnglishMenuItemText");
                break;
            case "ru":
                LanguageSelectorTextBlock.SetResourceReference(TextBlock.TextProperty, "RussianMenuItemText");
                break;
        }
    }
    
    private void ThemeSelector_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateThemeSelector();
    }
    
    private void LanguageSelector_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLanguageSelector();
    }

    private void SetLanguageMenuItemClick(object? item, string language)
    {
        if (item is not MenuItem menuItem)
            return;
        menuItem.Click += (_, _) =>
        {
            App.LanguageSwitch(language);
            UpdateThemeSelector();
        };
    }
    
    private void LanguageSelector_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        var menu = CreateContextMenu(btn, ["EnglishMenuItemText", "RussianMenuItemText"]);
        SetLanguageMenuItemClick(menu.Items[0], "en");
        SetLanguageMenuItemClick(menu.Items[1], "ru");
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        };
        Process.Start(processStartInfo);
        e.Handled = true;
    }
}