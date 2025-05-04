using System.Windows;
using System.Windows.Input;

namespace RemoteDeskopControlPannel
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        public bool IsOpened;
        private readonly int TopMenuOffsetX = 200;
        private readonly int TopMenuOffsetY = 170;
        public ClientWindow()
        {
            InitializeComponent();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                TopMenu.Visibility = Visibility.Collapsed;
                return;
            }
            var relPos = e.GetPosition(TopMenu);
            if ((-TopMenuOffsetX < relPos.X && relPos.X - TopMenu.ActualWidth < TopMenuOffsetX)
                && (-TopMenuOffsetY < relPos.Y && relPos.Y - TopMenu.ActualHeight < TopMenuOffsetY))
                TopMenu.Visibility = Visibility.Visible;
            else
                TopMenu.Visibility = Visibility.Collapsed;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Maximized && WindowStyle != WindowStyle.None)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }
}
