using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Animation;
using ZapretDesktop.Properties;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для ListButton.xaml
    /// </summary>
    public partial class ListButton
    {
        private string _listFileName = null!;
        private string _listFileText  = null!;
        private readonly EditingPanel _listPanel = new EditingPanel();

        public string ListFileName
        {
            get => _listFileName; 
            set => _listFileName = value; 
        }

        public string ListFileText
        {
            get => _listFileText;  
            set => _listFileText = value; 
        }
        
        public ListButton()
        {
            InitializeComponent();
        }

        private async void UserControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            App.CurrentMainWindow.BackgroundBlurOn();
            var txt = await Task.Run(() => File.ReadAllText($"{Settings.Default.ListPath}\\{this.ListFileName}"));
            _listFileText = txt;
            _listPanel.ListContent.Text = txt;
            _listPanel.List = this;
            App.CurrentMainWindow.BackgroundBlurOff();
        }
        
        private void ListButtonBody_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (ListButtonBody.IsChecked == true)
            {
                CloseListButton.Visibility = Visibility.Visible;
            }
        }

        private void ListButtonBody_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (ListButtonBody.IsChecked == true)
            {
                CloseListButton.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void ListButtonBody_OnChecked(object sender, RoutedEventArgs e)
        {
            if (App.CurrentMainWindow.SelectedList == null) // open lists
            {
                foreach (StrategyButton btn in App.CurrentMainWindow.Strategies.Children)
                {
                    btn.IsEnabled = false;
                }
                App.CurrentMainWindow.HideButton.IsEnabled = false;
                App.CurrentMainWindow.SettingsButton.IsEnabled = false;
                App.CurrentMainWindow.AddButton.IsEnabled = false;
                if (App.CurrentMainWindow.ConnectButton.IsChecked == true)
                {
                    foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
                    {
                        btn._listPanel.ListContent.IsReadOnly = true;
                        btn._listPanel.WarningMessage.Visibility = Visibility.Visible;
                    }
                }
                AllListButtonsOff();
                App.CurrentMainWindow.GridPanel.Children.Add(_listPanel);
                var connectionGridTranslateAnim = new DoubleAnimation()
                {
                    From = 0,
                    To = -App.CurrentMainWindow.ConnectionGrid.ActualHeight,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
                };
                App.CurrentMainWindow.ConnectionGridTranslate.BeginAnimation(TranslateTransform.YProperty, connectionGridTranslateAnim);
                await Task.Delay(600);
                App.CurrentMainWindow.ConnectionGrid.Visibility = Visibility.Collapsed;
                AllListButtonsOn();
            }
            else // left-right movement
            {
                EditingPanel newPanel = _listPanel;
                EditingPanel oldPanel = App.CurrentMainWindow.SelectedList._listPanel;
                var moveAnim = new DoubleAnimation()
                {
                    From = 0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
                };
                if (App.CurrentMainWindow.Lists.Children.IndexOf(this) < App.CurrentMainWindow.Lists.Children.IndexOf(App.CurrentMainWindow.SelectedList))
                {
                    Canvas.SetRight(newPanel, oldPanel.ActualWidth);
                    moveAnim.To = oldPanel.ActualWidth;
                }
                else
                {
                    Canvas.SetLeft(newPanel, oldPanel.ActualWidth);
                    moveAnim.To = -oldPanel.ActualWidth;
                }
                App.CurrentMainWindow.GridPanel.Children.Add(newPanel);
                oldPanel.EditingPanelTranslate.BeginAnimation(TranslateTransform.XProperty, moveAnim);
                newPanel.EditingPanelTranslate.BeginAnimation(TranslateTransform.XProperty, moveAnim);
                AllListButtonsOff();
                await Task.Delay(600);
                Canvas.SetRight(newPanel, double.NaN);
                Canvas.SetLeft(newPanel, double.NaN);
                oldPanel.EditingPanelTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                newPanel.EditingPanelTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                App.CurrentMainWindow.GridPanel.Children.Remove(App.CurrentMainWindow.SelectedList._listPanel); 
                AllListButtonsOn();
            }
            App.CurrentMainWindow.SelectedList = this;
            if (UnsaveIcon.Visibility == Visibility.Collapsed) 
            {
                ListFileText = _listPanel.ListContent.Text;
            }
            if (IsMouseOver)
            {
                CloseListButton.Visibility = Visibility.Visible;
            }
        }

        private async void ListButtonBody_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (App.CurrentMainWindow.SelectedList == null) // close lists
            {
                if (App.CurrentMainWindow.ConnectButton.IsChecked == true)
                {
                    if (App.CurrentMainWindow.SelectedStrategy is null)
                        return;
                    App.CurrentMainWindow.SelectedStrategy.IsEnabled = true;
                    foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
                    {
                        btn._listPanel.ListContent.IsReadOnly = false;
                        btn._listPanel.WarningMessage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    foreach (StrategyButton btn in App.CurrentMainWindow.Strategies.Children)
                    {
                        btn.IsEnabled = true;
                    }
                }
                App.CurrentMainWindow.HideButton.IsEnabled = true;
                App.CurrentMainWindow.SettingsButton.IsEnabled = true;
                App.CurrentMainWindow.AddButton.IsEnabled = true;
                foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
                {
                    btn._listPanel.SearchBoxOff();
                }
                AllListButtonsOff();
                App.CurrentMainWindow.ConnectionGrid.Visibility = Visibility.Visible;
                var connectionGridTranslateAnim = new DoubleAnimation()
                {
                    From = -App.CurrentMainWindow.ConnectionGrid.ActualHeight,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
                };
                App.CurrentMainWindow.ConnectionGridTranslate.
                    BeginAnimation(TranslateTransform.YProperty, connectionGridTranslateAnim);
                await Task.Delay(600);
                App.CurrentMainWindow.GridPanel.Children.Remove(_listPanel);
                AllListButtonsOn();
            }
            CloseListButton.Visibility = Visibility.Collapsed;
        }

        private static void AllListButtonsOn()
        {
            foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
            {
                btn.IsHitTestVisible = true;
            }
        }

        private static void AllListButtonsOff()
        {
            foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
            {
                btn.IsHitTestVisible = false;
            }
        }

        private async void CloseListButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
            {
                if (btn.UnsaveIcon.Visibility != Visibility.Visible)
                    continue;
                var answer = await Styles.SavePopupOpenAsync(btn.ListName.Text);
                if (answer == "Cancel")
                {
                    btn.ListButtonBody.IsChecked = true;
                    return;
                }
                else
                {
                    btn.UnsaveIcon.Visibility = Visibility.Collapsed;
                    if (answer == "Save")
                    {
                        btn._listPanel.SaveTextButton.IsChecked = true;
                    }
                    else
                    {
                        btn._listPanel.ListContent.TextChanged -= btn._listPanel.ListContent_OnTextChanged;
                        btn._listPanel.ListContent.Text = btn.ListFileText;
                        btn._listPanel.ListContent.TextChanged += btn._listPanel.ListContent_OnTextChanged;
                    }
                }
            }
            CloseListButton.Visibility = Visibility.Collapsed;
            App.CurrentMainWindow.SelectedList = null;
            foreach (ListButton btn in App.CurrentMainWindow.Lists.Children)
            {
                btn.ListButtonBody.IsChecked = false;
            }
        }
    }
}
