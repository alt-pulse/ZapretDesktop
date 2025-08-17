using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Threading;
using ZapretDesktop.Properties;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для StrategyButton.xaml
    /// </summary>
    public partial class StrategyButton
    {
        private string _strategyFileName = null!;

        public required string StrategyFileName
        {
            get => _strategyFileName;
            set => _strategyFileName = value;
        }
        
        public StrategyButton()
        {
            InitializeComponent();

            StrategyName.TextArea.SelectionBorder = null;
            StrategyName.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215));
            StrategyName.TextArea.SelectionForeground = SystemColors.HighlightTextBrush;
            StrategyName.TextArea.SelectionCornerRadius = 3;
        }

        private void StrategyOptionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu()
            {
                Placement = PlacementMode.Right,
                PlacementTarget = this
            };
            var renameItem = new MenuItem()
            {
                Header = $"{TryFindResource("RenameMenuItemText")}",
                Icon = new Image() { Height = 14, Source = Application.Current.Resources["RenameIcon"] as DrawingImage }
            };
            renameItem.Click += RenameButton_Click;
            var deleteItem = new MenuItem()
            {
                Header = $"{TryFindResource("DeleteMenuItemText")}",
                Foreground = Brushes.Red,
                Icon = new Image() { Height = 16, Source = Application.Current.Resources["DeleteIcon"] as DrawingImage }
            };
            deleteItem.Click += DeleteButton_Click;
            contextMenu.Items.Add(renameItem);
            contextMenu.Items.Add(deleteItem);
            contextMenu.IsOpen = true;
        }

        private void StrategyButtonBody_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (StrategyName.Visibility == Visibility.Collapsed && 
                App.CurrentMainWindow.ConnectButton.IsChecked == false)
            {
                StrategyOptionButton.Visibility = Visibility.Visible;
            }
        }

        private void StrategyButtonBody_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (StrategyName.Visibility == Visibility.Collapsed && 
                App.CurrentMainWindow.ConnectButton.IsChecked == false)
            {
                StrategyOptionButton.Visibility = Visibility.Collapsed;
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            StrategyNameMask.Visibility = Visibility.Collapsed;
            StrategyName.Visibility = Visibility.Visible;
            StrategyButtonBody.IsChecked = true;
            StrategyName.Focus();
            StrategyName.SelectAll();
        }

        private void StrategyName_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Escape)
                return;
            Keyboard.ClearFocus();
            App.CurrentMainWindow.MainGrid.Focus();
        }
        
        private void StrategyName_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var prohibitedSymbols = "\\/:*?\"<>|";
            if (e.Text.Any(ch => prohibitedSymbols.Contains(ch)))
            {
                e.Handled = true;
            }
        }

        private void StrategyName_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (StrategyName.IsKeyboardFocusWithin || StrategyName.IsFocused)
                    return;
                StrategyNameMask.Visibility = Visibility.Visible;
                StrategyName.Visibility = Visibility.Collapsed;
                var errorPopup = Application.Current.Resources["ErrorPopup"] as CustomPopup;
                bool wasCaught = false;
                try
                {
                    File.Move($@"{Settings.Default.StrategyPath}\{StrategyFileName}",
                        $@"{Settings.Default.StrategyPath}\{StrategyName.Text}.bat");
                    StrategyFileName = $"{StrategyName.Text}.bat";
                }
                catch (DirectoryNotFoundException)
                {
                    wasCaught = true;
                    var text = $"{TryFindResource("ErrorPopupWrongSymbolsMessageText")}";
                    if (errorPopup?.PopupContent is TextBlock textBlock)
                    {
                        textBlock.Text = text;
                        errorPopup.Show(App.CurrentMainWindow.PopupGrid);
                    }
                    else
                    {
                        MessageBox.Show(text);
                    }
                }
                catch (IOException ex)
                {
                    wasCaught = true;
                    const int errorInvalidName = 123;
                    const int errorFileExists = 183;
                    int errorCode = ex.HResult & 0xFFFF;
                    string text = string.Empty;
                    switch (errorCode)
                    {
                        case errorInvalidName:
                            text = $"{TryFindResource("ErrorPopupWrongSymbolsMessageText")}";
                            if (errorPopup?.PopupContent is TextBlock invalidNameTextBlock)
                            {
                                invalidNameTextBlock.Text = text;
                                errorPopup.Show(App.CurrentMainWindow.PopupGrid);
                            }
                            else
                            {
                                MessageBox.Show(text);
                            }
                            break;
                        case errorFileExists:
                            text = $"{TryFindResource("ErrorPopupFileNameMessageBeginningText")} " +
                                   $"{StrategyName.Text}.bat " +
                                   $"{TryFindResource("ErrorPopupFileAlreadyExistsMessageText")}";
                            if (errorPopup?.PopupContent is TextBlock errorFileExistsTextBlock)
                            {
                                errorFileExistsTextBlock.Text = text;
                                errorPopup.Show(App.CurrentMainWindow.PopupGrid);
                            }
                            else
                            {
                                MessageBox.Show(text);
                            }
                            break;
                        default:
                            text = $"{TryFindResource("ErrorPopupRenameFailedMessageText")}";
                            if (errorPopup?.PopupContent is TextBlock otherErrorTextBlock)
                            {
                                otherErrorTextBlock.Text = text;
                                errorPopup.Show(App.CurrentMainWindow.PopupGrid);
                            }
                            else
                            {
                                MessageBox.Show(text);
                            }
                            break;
                    }
                }
                if (wasCaught)
                {
                    StrategyName.Text = StrategyFileName[..^4];
                }
            }), DispatcherPriority.Background);
        }

        private void StrategyName_OnTextChanged(object sender, EventArgs e)
        {
            StrategyNameMask.Text = StrategyName.Text;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            StrategyButtonBody.IsChecked = true;
            var deletePopup = TryFindResource("DeletePopup") as CustomPopup;
            var text = $"{TryFindResource("DeletePopupMessageText")} {_strategyFileName}?";
            if (deletePopup?.PopupContent is TextBlock textBlock)
            {
                textBlock.Text = text;
                deletePopup.Show(App.CurrentMainWindow.PopupGrid);
            }
            else
            {
                MessageBox.Show(text);
            }
        }

        private void StrategyButtonBody_OnChecked(object sender, RoutedEventArgs e)
        {
            if (App.CurrentMainWindow.SelectedStrategy != null)
            {
                App.CurrentMainWindow.SelectedStrategy.StrategyButtonBody.IsChecked = false;
            }
            App.CurrentMainWindow.SelectedStrategy = this;
        }
        
        public void DeleteStrategy()
        {
            File.Delete($"{Settings.Default.StrategyPath}\\{StrategyFileName}");
            App.CurrentMainWindow.Strategies.Children.Remove(this);
            if (App.CurrentMainWindow.Strategies.Children.Count > 0 && 
                App.CurrentMainWindow.Strategies.Children[0] is StrategyButton btn)
            {
                btn.StrategyButtonBody.IsChecked = true;
            }
            else
            {
                App.CurrentMainWindow.ConnectButton.IsChecked = null;
            }
        }
    }
}
