using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using ZapretDesktop.Properties;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для EditingPanel.xaml
    /// </summary>
    public partial class EditingPanel
    {
        private ListButton _list = null!;
        private List<Match>? _matches;
        private Match? _currentMatch;
        
        public ListButton List
        {
            get => _list; 
            set => _list = value;
        }
        
        public EditingPanel()
        {
            InitializeComponent();
            var canvasBind = new Binding("ActualHeight")
            {
                Source = App.CurrentMainWindow.ConnectionGrid
            };
            SetBinding(Canvas.TopProperty, canvasBind);
            var yBind = new Binding("Y")
            {
                Source = App.CurrentMainWindow.ConnectionGridTranslate
            };
            BindingOperations.SetBinding(EditingPanelTranslate, TranslateTransform.YProperty, yBind);
            var heightBind = new Binding("ActualHeight")
            {
                Source = App.CurrentMainWindow.MainGrid
            };
            SetBinding(HeightProperty, heightBind);
            var widthBind = new Binding("ActualWidth")
            {
                Source = App.CurrentMainWindow.GridPanel
            };
            SetBinding(WidthProperty, widthBind);
            
            ListContent.TextArea.SelectionBorder = null;
            ListContent.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215));
            ListContent.TextArea.SelectionForeground = SystemColors.HighlightTextBrush;
            ListContent.TextArea.SelectionCornerRadius = 3;
            SearchBox.TextArea.SelectionBorder = null;
            SearchBox.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215));
            SearchBox.TextArea.SelectionForeground = SystemColors.HighlightTextBrush;
            SearchBox.TextArea.SelectionCornerRadius = 3;
        }

        public void ListContent_OnTextChanged(object? sender, EventArgs e)
        { 
            if (App.CurrentMainWindow.SelectedList == null || 
                !App.CurrentMainWindow.SelectedList.IsHitTestVisible ||
                App.CurrentMainWindow.SelectedList.UnsaveIcon.Visibility != Visibility.Collapsed)
                return;
            App.CurrentMainWindow.SelectedList.UnsaveIcon.Visibility = Visibility.Visible;
            SaveTextButton.IsChecked = false;
            SaveTextButton.IsHitTestVisible = true;
        }

        private void SaveTextButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (App.CurrentMainWindow.SelectedList is null)
                return;
            App.CurrentMainWindow.SelectedList.UnsaveIcon.Visibility = Visibility.Collapsed;
            SaveTextButton.IsHitTestVisible = false;
            App.CurrentMainWindow.SelectedList.ListFileText = ListContent.Text;
            File.WriteAllText($"{Settings.Default.ListPath}\\{App.CurrentMainWindow.SelectedList.ListFileName}", ListContent.Text);
        }

        private async void CopyTextButton_OnChecked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ListContent.Text);
            CopyTextButton.IsHitTestVisible = false;
            await Task.Delay(2000);
            CopyTextButton.IsChecked = false;
            CopyTextButton.IsHitTestVisible = true;
        }

        public void SearchBoxOff()
        {
            SearchButton.IsChecked = false;
            SearchBox.Text = string.Empty;
            _matches = null;
        }
        
        private void CloseSearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchBoxOff();
        }

        private void SearchButton_OnChecked(object sender, RoutedEventArgs e)
        {
            SaveTextButton.Visibility = Visibility.Collapsed;
            CopyTextButton.Visibility = Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Visible;
            SearchBoxMask.Text = $"{TryFindResource("SearchBoxText")} {_list.ListFileName}";
            if (SearchButton.Template.FindName("SearchMatches", SearchButton) is TextBlock searchMatches)
            {
                searchMatches.Text = string.Empty;
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SearchBox.Focus();
                SearchBox.TextArea.Caret.Offset = SearchBox.Text.Length;
            }), DispatcherPriority.Input);
        }

        private void SearchButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            SaveTextButton.Visibility = Visibility.Visible;
            CopyTextButton.Visibility = Visibility.Visible;
            SearchBox.Visibility = Visibility.Collapsed;
            SearchBoxMask.SetResourceReference(TextBlock.TextProperty, "SearchButtonText");
        }

        private void SearchBox_OnTextChanged(object sender, EventArgs e)
        {
            var searchMatches = SearchButton.Template.FindName("SearchMatches", SearchButton) as TextBlock;
            if (SearchBox.Text.Length == 0)
            {
                SearchBoxMask.Visibility = Visibility.Visible;
                
                if (searchMatches != null)
                {
                    searchMatches.Text = string.Empty;
                }
                ListContent.Select(0, 0);
                _currentMatch = null;
                _matches = null;
            }
            else
            {
                SearchBoxMask.Visibility = Visibility.Collapsed;
                _matches = Regex.Matches(ListContent.Text, SearchBox.Text).ToList();
                if (_matches.Count > 0)
                {
                    ListContent.Select(_matches[0].Index, _matches[0].Length);
                    _currentMatch = _matches[0];
                    ListContent.TextArea.Caret.Offset = _currentMatch.Index;
                    ListContent.TextArea.Caret.BringCaretToView();
                    if (searchMatches != null)
                    {
                        searchMatches.Text = $"{_matches.IndexOf(_currentMatch) + 1}/{_matches.Count}";
                    }
                }
                else
                {
                    ListContent.Select(0, 0);
                    _currentMatch = null;
                    _matches = null;
                    if (searchMatches != null)
                    {
                        searchMatches.Text = "0";
                    }
                }
            }
        }

        
        private void SearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }
        }

        private void UpSearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentMatch == null || _matches == null || _currentMatch == _matches[0])
                return;
            _currentMatch = _matches[_matches.IndexOf(_currentMatch) - 1];
            ListContent.Select(_currentMatch.Index, _currentMatch.Length);
            ListContent.TextArea.Caret.Offset = _currentMatch.Index;
            ListContent.TextArea.Caret.BringCaretToView();
            if (SearchButton.Template.FindName("SearchMatches", SearchButton) is TextBlock searchMatches)
            {
                searchMatches.Text = $"{_matches.IndexOf(_currentMatch) + 1}/{_matches.Count}";
            }
        }

        private void DownSearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentMatch == null || _matches == null || _currentMatch == _matches.Last())
                return;
            _currentMatch = _matches[_matches.IndexOf(_currentMatch) + 1];
            ListContent.Select(_currentMatch.Index, _currentMatch.Length);
            ListContent.TextArea.Caret.Offset = _currentMatch.Index;
            ListContent.TextArea.Caret.BringCaretToView();
            if (SearchButton.Template.FindName("SearchMatches", SearchButton) is TextBlock searchMatches)
            {
                searchMatches.Text = $"{_matches.IndexOf(_currentMatch) + 1}/{_matches.Count}";
            }
        }

        private void Cut_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ListContent.SelectedText);
            ListContent.SelectedText = string.Empty;
        }

        private void Copy_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ListContent.SelectedText);
        }

        private void Paste_OnClick(object sender, RoutedEventArgs e)
        {
            ListContent.Paste();
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            ListContent.SelectedText = string.Empty;
        }

        private void SelectAll_OnClick(object sender, RoutedEventArgs e)
        {
            ListContent.SelectAll();
        }

        private void ContextMenu_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ListContent.SelectedText.Length > 0)
            {
                if (ListContent.ContextMenu == null)
                    return;
                foreach (MenuItem item in ListContent.ContextMenu.Items)
                {
                    item.IsEnabled = true;
                }
            }
            else
            {
                Cut.IsEnabled = false;
                Copy.IsEnabled = false;
                Delete.IsEnabled = false;
            }
        }

        
    }
}
