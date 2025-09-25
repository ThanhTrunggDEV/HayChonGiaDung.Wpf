using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HayChonGiaDung.Wpf
{
    public partial class PunchBoardWindow : Window
    {
        private int picksLeft;
        private int[] prizes;
        public PunchBoardWindow(int correctCount)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();
            picksLeft = Math.Max(0, correctCount);
            LeftText.Text = picksLeft.ToString();
            // prize distribution (in VND)
            var basePrizes = new int[] {0,0,0,0,50000,50000,100000,100000,200000,200000,500000,500000,1000000,1000000,2000000,5000000};
            prizes = new int[50];
            for (int i=0;i<50;i++) prizes[i] = basePrizes[GameState.Rnd.Next(basePrizes.Length)];
            for (int i=0;i<50;i++)
            {
                var b = new Button { Content=(i+1).ToString(), Margin=new Thickness(4), Tag=i };
                b.Click += Pick;
                GridBoard.Children.Add(b);
            }
        }

        private void Pick(object sender, RoutedEventArgs e)
        {
            if (picksLeft<=0) return;
            var btn = (Button)sender;
            int idx = (int)btn.Tag;
            int prize = prizes[idx];
            //btn.IsEnabled = false;
            btn.Foreground = System.Windows.Media.Brushes.Black;
            btn.Background = prize>0 ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightCoral;
            btn.Content = $"{prize:N0} ₫";
            GameState.AddPrize(prize);
            picksLeft--;
            LeftText.Text = picksLeft.ToString();
            if (prize>0) SoundManager.Correct(); else SoundManager.Wrong();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }
    }
}
