using System;
using System.IO;
using System.Windows;

namespace HayChonGiaDung.Wpf
{
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
            SoundManager.StartBackground();

            // nếu không có file logo thì hiện fallback text
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "logo_hcgd.png");
                if (!File.Exists(path))
                {
                    LogoImage.Visibility = Visibility.Collapsed;
                    LogoFallback.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                LogoImage.Visibility = Visibility.Collapsed;
                LogoFallback.Visibility = Visibility.Visible;
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            // mở MainWindow hiện tại (có nhập tên + chạy flow 4 vòng)
            var w = new MainWindow();
            w.Show();
            Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow { Owner = this }.ShowDialog();
        }

        private void Leaderboard_Click(object sender, RoutedEventArgs e)
        {
            new LeaderboardWindow { Owner = this }.ShowDialog();
        }

        private void Guide_Click(object sender, RoutedEventArgs e)
        {
           new GuideWindow { Owner = this }.ShowDialog();
        }

        private void Admin_Click(object sender, RoutedEventArgs e)
        {
            new AdminWindow { Owner = this }.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
