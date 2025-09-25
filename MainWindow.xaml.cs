using System.Windows;

namespace HayChonGiaDung.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            CoinText.Text = GameState.Coins.ToString();
            //SoundManager.Background();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerNameBox.Text))
            {
                StatusText.Text = "Vui lòng nhập tên!";
                return;
            }
            GameState.PlayerName = PlayerNameBox.Text.Trim();
            GameState.Reset();
            UpdateEconomyTexts();

            var warmup = new QuickStartWindow { Owner = this };
            if (warmup.ShowDialog() != true)
            {
                StatusText.Text = "Bạn đã dừng ở vòng khởi động.";
                return;
            }

            StatusText.Text = string.Empty;
            UpdateEconomyTexts();

            var r1 = new Round1Window { Owner = this };
            r1.ShowDialog();
            UpdateEconomyTexts();
            if (HandleGameOver()) { return; }

            var r2 = new Round2Window { Owner = this };
            r2.ShowDialog();
            UpdateEconomyTexts();
            if (HandleGameOver()) { return; }

            var r3 = new Round3Window { Owner = this };
            r3.ShowDialog();
            UpdateEconomyTexts();
            if (HandleGameOver()) { return; }

            var r4 = new Round4Window { Owner = this };
            r4.ShowDialog();
            UpdateEconomyTexts();
            if (HandleGameOver()) { return; }

            SoundManager.Win();
            MessageBox.Show($"Chúc mừng {GameState.PlayerName}! Bạn đã hoàn thành tất cả vòng chơi.\nTổng thưởng: {GameState.TotalPrize:N0} ₫","Hoàn thành");
            ReturnToStart();
        }

        private bool HandleGameOver()
        {
            if (GameState.TotalPrize > 0)
            {
                return false;
            }

            var endGameWindow = new EndGameWindow { Owner = this };
            endGameWindow.ShowDialog();
            ReturnToStart();
            return true;
        }

        private void ReturnToStart()
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            Application.Current.MainWindow = startWindow;
            Close();
        }

        private void UpdateEconomyTexts()
        {
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            CoinText.Text = GameState.Coins.ToString();
        }
    }
}
