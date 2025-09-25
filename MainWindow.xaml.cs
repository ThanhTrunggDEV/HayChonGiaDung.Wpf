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
            GameState.PlayerName = PlayerNameBox.Text.Trim();
            GameState.Reset();
            PrizeText.Text = "0 ₫";

            var r1 = new Round1Window();
            r1.Owner = this;
            r1.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            if (GameState.TotalPrize == 0) { new EndGameWindow().ShowDialog(); return; }

            var r2 = new Round2Window(); r2.Owner = this; r2.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            if (GameState.TotalPrize == 0) { new EndGameWindow().ShowDialog(); return; }

            var r3 = new Round3Window(); r3.Owner = this; r3.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            if (GameState.TotalPrize == 0) { new EndGameWindow().ShowDialog(); return; }

            var r4 = new Round4Window(); r4.Owner = this; r4.ShowDialog();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            if (GameState.TotalPrize == 0) { new EndGameWindow().ShowDialog(); return; }

            SoundManager.Win();
            MessageBox.Show($"Chúc mừng {GameState.PlayerName}! Bạn đã hoàn thành tất cả vòng chơi.\nTổng thưởng: {GameState.TotalPrize:N0} ₫","Hoàn thành");
        }
    }
}
