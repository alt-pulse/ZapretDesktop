using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using ZapretDesktop.Properties;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private static HwndSource? _hwndSource;
        private const int WmSettingChange = 0x001A;
        private const string MutexName = "Snail_Mutex";
        private const string ShowEventName = "Snail_ShowEvent";
        private static Mutex? _mutex;
        private static EventWaitHandle? _showEvent;
        public static MainWindow CurrentMainWindow { get; set; } = null!;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool isNewCreated);
            if (!isNewCreated)
            {
                try
                {
                    using var ev = EventWaitHandle.OpenExisting(ShowEventName);
                    ev.Set();
                }
                catch
                {
                    return;
                }
                Shutdown();
                return;
            }
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            ThreadPool.RegisterWaitForSingleObject(
                _showEvent,
                (_, _) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ShowWindow();
                        }
                    }));
                }, 
                null,
                -1,
                false);
            base.OnStartup(e);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);

        private static void EnableDarkTitleBar(Window window, int useDarkMode)
        {
            if (Environment.OSVersion.Version < new Version(10, 0, 17763)) 
                return;
            var hwnd = new WindowInteropHelper(window).Handle;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
        }

        public static void LanguageSwitch(string language)
        {
            var dictionaries = Current.Resources.MergedDictionaries;
            var currentLanguage = dictionaries.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("Language.ru.xaml") || d.Source.OriginalString.Contains("Language.en.xaml")));
            if (currentLanguage != null)
            {
                dictionaries.Remove(currentLanguage);
            }
            var newLanguage = new ResourceDictionary()
            {
                Source = new Uri($"Languages/Language.{language}.xaml", UriKind.Relative)
            };
            dictionaries.Add(newLanguage);
            Settings.Default.Language = language;
            Settings.Default.Save();
        }
        
        public static void DarkModeSwitch(bool darkMode)
        {
            var dictionaries = Current.Resources.MergedDictionaries;
            var currentTheme = dictionaries.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("LightTheme.xaml") || 
                                     d.Source.OriginalString.Contains("DarkTheme.xaml")));
            var currentIcons = dictionaries.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("IconsLightTheme.xaml") || 
                                     d.Source.OriginalString.Contains("IconsDarkTheme.xaml")));
            if (currentTheme != null)
            {
                dictionaries.Remove(currentTheme);
                dictionaries.Remove(currentIcons);
            }
            var newTheme = new ResourceDictionary();
            var newIcons = new ResourceDictionary();
            if (darkMode)
            {
                newTheme.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                newIcons.Source = new Uri("Icons/IconsDarkTheme.xaml", UriKind.Relative);
                EnableDarkTitleBar(CurrentMainWindow, 1);
                Settings.Default.IsDarkMode = true;
            }
            else
            {
                newTheme.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                newIcons.Source = new Uri("Icons/IconsLightTheme.xaml", UriKind.Relative);
                EnableDarkTitleBar(CurrentMainWindow, 0);
                Settings.Default.IsDarkMode = false;
            }
            Settings.Default.Save();
            dictionaries.Add(newTheme);
            dictionaries.Add(newIcons);
            CurrentMainWindow.RedrawNotifyIcon();
            // logo color reset
            bool? isConnectButtonChecked = CurrentMainWindow.ConnectButton.IsChecked;
            CurrentMainWindow.ConnectButton.Checked -= CurrentMainWindow.ConnectButton_OnChecked;
            CurrentMainWindow.ConnectButton.IsChecked =  true;
            CurrentMainWindow.ConnectButton.Checked += CurrentMainWindow.ConnectButton_OnChecked;
            CurrentMainWindow.ConnectButton.IsChecked =  false;
            CurrentMainWindow.ConnectButton.IsChecked = isConnectButtonChecked;
        }
        
        private static bool IsSystemInDarkMode()
        {
            const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey);
            if (key?.GetValue(valueName) is int lightTheme)
            {
                return lightTheme == 0;
            }
            return false;
        }

        public static void HookThemeListener()
        {
            if (_hwndSource == null)
            {
                var hwnd = new WindowInteropHelper(CurrentMainWindow).Handle;
                _hwndSource = HwndSource.FromHwnd(hwnd);
                _hwndSource?.AddHook(WndProc);
            }
            DarkModeSwitch(IsSystemInDarkMode());
            Settings.Default.IsDarkMode = null;
            Settings.Default.Save();
        }
        
        public static void UnhookThemeListener()
        {
            if (_hwndSource == null)
                return;
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WmSettingChange)
                return IntPtr.Zero;
            var param = Marshal.PtrToStringUni(lParam);
            if (param is not ("ImmersiveColorSet" or "WindowsThemeElement"))
                return IntPtr.Zero;
            DarkModeSwitch(IsSystemInDarkMode());
            Settings.Default.IsDarkMode = null;
            Settings.Default.Save();
            return IntPtr.Zero;
        }
    }
}
