using System.Linq;
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
            //SoundManager.Background();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerNameBox.Text))
            {
                StatusText.Text = "Vui lòng nhập tên!";
                return;
            }
            GameState.Reset();
            GameState.PlayerName = PlayerNameBox.Text.Trim();
            PrizeText.Text = "0 ₫";
            RefreshHud();

            var quick = new QuickStartWindow { Owner = this };
            quick.ShowDialog();
            RefreshHud();
            if (quick.DialogResult != true)
            {
                ReturnToStart();
                return;
            }

            var r1 = new Round1Window();
            r1.Owner = this;
            r1.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            if (HandleGameOver()) { return; }

            var r2 = new Round2Window(); r2.Owner = this; r2.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            if (HandleGameOver()) { return; }

            var r3 = new Round3Window(); r3.Owner = this; r3.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            if (HandleGameOver()) { return; }

            var r4 = new Round4Window(); r4.Owner = this; r4.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            if (HandleGameOver()) { return; }

            var r5 = new Round5Window(); r5.Owner = this; r5.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            if (HandleGameOver()) { return; }

            SoundManager.Win();
            MessageBox.Show($"Chúc mừng {GameState.PlayerName}! Bạn đã hoàn thành tất cả vòng chơi.\nTổng thưởng: {GameState.TotalPrize:N0} ₫","Hoàn thành");
            ReturnToStart();
        }

        private void RefreshHud()
        {
            CoinText.Text = GameState.QuickCoins.ToString();
            CardText.Text = string.Join(", ", GameState.GetInventory()
                .Where(kv => kv.Value > 0)
                .Select(kv => $"{GetCardName(kv.Key)} x{kv.Value}"));
            if (string.IsNullOrWhiteSpace(CardText.Text))
            {
                CardText.Text = "Chưa có";
            }
        }

        private static string GetCardName(HelpCardType type) => type switch
        {
            HelpCardType.Hint => "Gợi ý",
            HelpCardType.SwapProduct => "Đổi sản phẩm",
            HelpCardType.DoubleReward => "Nhân đôi",
            _ => type.ToString()
        };

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
    }
}
