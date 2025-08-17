using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ZapretDesktop;

public partial class Styles
{
    public Styles()
    {
        InitializeComponent();
    }
    
    private void ContextMenu_OnHandler(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu)
            return;
        var scaleTransform = (ScaleTransform)menu.Template.FindName("PART_BorderScale", menu);
        menu.HorizontalOffset = 0;
        menu.VerticalOffset = 0;
        var pos = Mouse.GetPosition(menu);
        if (pos.X <= menu.ActualWidth / 2)
        {
            menu.HorizontalOffset = -12;
            scaleTransform.CenterX = 0;
        }
        else
        {
            menu.HorizontalOffset = 12;
            scaleTransform.CenterX = menu.ActualWidth;
        }
        if (pos.Y <= menu.ActualHeight / 2)
        {
            menu.VerticalOffset = -8;
            scaleTransform.CenterY = 0;
        }
        else
        {
            menu.VerticalOffset = 16;
            scaleTransform.CenterY = menu.ActualHeight;
        }
        if (menu.PlacementTarget is FrameworkElement fe && menu.HorizontalAlignment == HorizontalAlignment.Left)
        {
            menu.HorizontalOffset = -menu.HorizontalOffset - (menu.ActualWidth - fe.ActualWidth);
            scaleTransform.CenterX = menu.ActualWidth;
        }
    }
    
    private void ToolTip_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ToolTip toolTip ||
            toolTip.PlacementTarget is not FrameworkElement target ||
            toolTip.Template.FindName("PART_Grid", toolTip) is not Grid grid ||
            toolTip.Template.FindName("SizingColumn1", toolTip) is not ColumnDefinition sizingColumn1 ||
            toolTip.Template.FindName("SizingColumn2", toolTip) is not ColumnDefinition sizingColumn2)
            return;
        var gridMargin = grid.Margin;
        double toolTipWidth = toolTip.ActualWidth - gridMargin.Left - gridMargin.Right;
        double toolTipHeight = toolTip.ActualHeight - gridMargin.Top - gridMargin.Bottom;
        if (toolTip.Placement == PlacementMode.Bottom || toolTip.Placement == PlacementMode.Top)
        {
            if (toolTip.HorizontalAlignment == HorizontalAlignment.Center)
            {
                toolTip.HorizontalOffset = (target.ActualWidth - toolTipWidth) / 2 - gridMargin.Left;
            }
            else if (toolTip.HorizontalAlignment == HorizontalAlignment.Left || toolTip.HorizontalAlignment == HorizontalAlignment.Right)
            {
                ColumnDefinition sizingColumn;
                if (toolTip.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    toolTip.HorizontalOffset = -(toolTipWidth - target.ActualWidth) - gridMargin.Left;
                    sizingColumn = sizingColumn2;
                }
                else
                {
                    sizingColumn = sizingColumn1;
                }
                if (target.ActualWidth < toolTipWidth)
                {
                    sizingColumn.MinWidth = (target.ActualWidth - 8) / 2;
                    sizingColumn.MaxWidth = sizingColumn.MinWidth;
                }
            }
        }
        else if ((toolTip.Placement == PlacementMode.Right || toolTip.Placement == PlacementMode.Left) && toolTip.VerticalAlignment == VerticalAlignment.Center)
        {
            toolTip.VerticalOffset = (target.ActualHeight - toolTipHeight) / 2 - gridMargin.Top;
        }
    }
    
    private void RoundButton_OnInitialized(object? sender, EventArgs e)
    {
        if (sender is not Border border)
            return;
        border.CornerRadius = new CornerRadius(border.Height / 2);
    }

    #region CustomPopupRegion
    
    private static TaskCompletionSource<string>? _popupResultTcs;
    
    public static async Task<string> SavePopupOpenAsync(string fileName)
    {
        _popupResultTcs = new TaskCompletionSource<string>();
        var savePopup = App.CurrentMainWindow.TryFindResource("SavePopup") as CustomPopup;
        var text = $"{Application.Current.TryFindResource("SavePopupMessageText")} {fileName}.txt?";
        if (savePopup?.PopupContent is TextBlock textBlock)
        {
            textBlock.Text = text;
            savePopup.Show(App.CurrentMainWindow.PopupGrid);
        }
        else
        {
            MessageBox.Show(text);
        }
        return await _popupResultTcs.Task; 
    }
    
    private void NoSaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        (Application.Current.Resources["SavePopup"] as CustomPopup)!.Close();
        _popupResultTcs?.TrySetResult("NoSave");
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        (Application.Current.Resources["SavePopup"] as CustomPopup)!.Close();
        _popupResultTcs?.TrySetResult("Cancel");
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        (Application.Current.Resources["SavePopup"] as CustomPopup)!.Close();
        _popupResultTcs?.TrySetResult("Save");
    }

    private void ConfirmDeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.CurrentMainWindow.SelectedStrategy!.DeleteStrategy();
        (Application.Current.Resources["DeletePopup"] as CustomPopup)!.Close();
    }

    private void NoDeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        (Application.Current.Resources["DeletePopup"] as CustomPopup)!.Close();
    }
    
    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        (Application.Current.Resources["ErrorPopup"] as CustomPopup)!.Close();
    }
    
    #endregion
}

