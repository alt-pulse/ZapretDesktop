using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZapretDesktop
{
    /// <summary>
    /// Логика взаимодействия для CustomPopup.xaml
    /// </summary>
    public partial class CustomPopup
    {
        public CustomPopup()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CustomPopup));

        public string Title
        {
            get => (string)GetValue(TitleProperty); 
            set => SetValue(TitleProperty, value); 
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Image), typeof(CustomPopup));

        public Image Icon
        {
            get => (Image)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ButtonStackProperty =
            DependencyProperty.Register(nameof(ButtonStack), typeof(StackPanel), typeof(CustomPopup));

        public StackPanel ButtonStack
        {
            get => (StackPanel)GetValue(ButtonStackProperty); 
            set => SetValue(ButtonStackProperty, value);
        }

        public static readonly DependencyProperty PopupBackgroundProperty =
            DependencyProperty.Register(nameof(PopupBackground), typeof(Brush), typeof(CustomPopup));

        public Brush PopupBackground
        {
            get => (Brush)GetValue(PopupBackgroundProperty);
            set => SetValue(PopupBackgroundProperty, value);
        }
        
        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register(nameof(PopupContent), typeof(object), typeof(CustomPopup));

        public object PopupContent
        {
            get => GetValue(PopupContentProperty);
            set => SetValue(PopupContentProperty, value);
        }
        
        public static readonly DependencyProperty CloseButtonVisibilityProperty =
            DependencyProperty.Register(nameof(CloseButtonVisibility), typeof(Visibility), typeof(CustomPopup));

        public Visibility CloseButtonVisibility
        {
            get => (Visibility)GetValue(CloseButtonVisibilityProperty);
            set => SetValue(CloseButtonVisibilityProperty, value);
        }

        public void Show(Panel parent)
        {
            parent.Children.Add(this);
            App.CurrentMainWindow.BackgroundBlurOn();
        }

        public void Close()
        {
            if (Parent is Panel panel)
            {
                panel.Children.Remove(this);
            }
            App.CurrentMainWindow.BackgroundBlurOff();
        }

        private void ClosePopupButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
