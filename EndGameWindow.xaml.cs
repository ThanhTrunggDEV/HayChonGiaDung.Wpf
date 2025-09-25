using System.Windows;
namespace HayChonGiaDung.Wpf
{
    public partial class EndGameWindow : Window
    {
        public EndGameWindow()
        {
            InitializeComponent();
            SoundManager.End();
            Msg.Text = $"{GameState.PlayerName}, bạn đã thua ở vòng này.\nTổng thưởng hiện tại: {GameState.TotalPrize:N0} ₫";
        }
        private void Close_Click(object sender, RoutedEventArgs e){ this.Close(); }
    }
}
