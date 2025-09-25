using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HayChonGiaDung.Wpf
{
    public partial class Round2Window : Window
    {
        private Product current = null!;
        private int price4;            // giá tính theo nghìn
        private int timeLeft = 60;     // 60 giây
        private DispatcherTimer timer;

        public Round2Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();

            // Random sản phẩm
            current = GameState.Catalog.Count > 0
                ? GameState.Catalog[GameState.Rnd.Next(GameState.Catalog.Count)]
                : new Product { Name = "Sản phẩm demo", Price = 2_890_000, Image = null };

            ProductName.Text = $"Đoán 4 chữ số cho giá (x.000 ₫): {current.Name}";
            price4 = Math.Max(1000, current.Price) / 1000;

            ProductImage.Source = null;
            if (!string.IsNullOrWhiteSpace(current.ImageUrl) &&
                Uri.IsWellFormedUriString(current.ImageUrl, UriKind.Absolute))
            {
                ProductImage.Source = new BitmapImage(new Uri(current.ImageUrl));
            }
            else if (!string.IsNullOrWhiteSpace(current.Image))
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", current.Image);
                if (System.IO.File.Exists(path))
                    ProductImage.Source = new BitmapImage(new Uri(path));
            }

            // Khởi tạo timer
            TimerText.Text = timeLeft.ToString();
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            timeLeft--;
            TimerText.Text = timeLeft.ToString();

            if (timeLeft <= 0)
            {
                timer.Stop();
                SoundManager.Wrong();
                MessageBox.Show("⏰ Hết giờ! Bạn đã thua vòng Đếm Ngược");
                this.DialogResult = false;
                Close();
            }
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            string g = $"{D1.Text}{D2.Text}{D3.Text}{D4.Text}";
            if (g.Length != 4 || !int.TryParse(g, out var guess))
            {
                Hint.Text = "⚠️ Nhập đủ 4 chữ số (0-9).";
                return;
            }

            int target = price4;
            if (guess == target)
            {
                timer.Stop();
                Hint.Text = $"✅ Chính xác! Giá: {target:N0}.000 ₫ (+1,000,000 ₫)";
                GameState.TotalPrize += 1_000_000;
                SoundManager.Correct();
                this.DialogResult = true;
                Close();
                return;
            }

            // Gợi ý từng digit để đoán tiếp trong 60s (so sánh theo int)
            var gt = target.ToString("0000");     // giữ leading zero
            char[] hint = new char[4];

            for (int i = 0; i < 4; i++)
            {
                int gd = g[i] - '0';             // char -> int (0..9)
                int td = gt[i] - '0';             // char -> int (0..9)

                hint[i] = (gd == td) ? '='
                       : (gd < td) ? '<'
                                     : '>';
            }

            Hint.Text = $"Gợi ý: [{hint[0]} {hint[1]} {hint[2]} {hint[3]}]";

            SoundManager.Wrong();

            // (optional) Auto-focus lại ô đầu để gõ nhanh vòng sau:
            D1.Focus();
            D1.SelectAll();
        }
    }
}
