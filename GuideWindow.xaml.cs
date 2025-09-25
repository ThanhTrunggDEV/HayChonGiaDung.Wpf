using System.Windows;
using System.Windows.Input;

namespace HayChonGiaDung.Wpf
{
    public partial class GuideWindow : Window
    {
        public GuideWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.F1) Tabs.SelectedIndex = 0;
        }
    }
}
