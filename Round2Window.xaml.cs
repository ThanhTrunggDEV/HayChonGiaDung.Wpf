using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private bool selectionNeedsMostExpensive;
        private Product[] selectionPool = Array.Empty<Product>();
        private Product selectionAnswer = null!;
        private bool doubleRewardActive = false;

        public Round2Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();

            SetupSelectionPhase();

            // Khởi tạo timer
            TimerText.Text = timeLeft.ToString();
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            timeLeft--;
            TimerText.Text = timeLeft.ToString();

            if (timeLeft <= 0)
            {
                timer.Stop();
                SoundManager.Wrong();
                RoundCelebrationHelper.ShowLose(this, "⏰ Hết giờ! Bạn đã thua vòng Đếm Ngược.");
                this.DialogResult = false;
                Close();
            }
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
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
                int reward = doubleRewardActive ? 2_000_000 : 1_000_000;
                Hint.Text = $"✅ Chính xác! Giá: {target:N0}.000 ₫ (+{reward:N0} ₫)";
                GameState.TotalPrize += reward;
                SoundManager.Correct();
                await RoundCelebrationHelper.ShowWinAsync(this,
                    $"Bạn đã thắng vòng Đếm Ngược và nhận {reward:N0} ₫!\nGiá chính xác: {target:N0}.000 ₫.");
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

        private void SetupSelectionPhase()
        {
            selectionNeedsMostExpensive = GameState.Rnd.Next(2) == 0;
            var pool = new List<Product>();
            if (GameState.Catalog.Count >= 4)
            {
                pool = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(4).ToList();
            }
            else
            {
                pool = new List<Product>
                {
                    new Product{ Name="Máy xay sinh tố", Price=950_000 },
                    new Product{ Name="Bình đun siêu tốc", Price=650_000 },
                    new Product{ Name="Robot hút bụi", Price=6_500_000 },
                    new Product{ Name="Tủ lạnh mini", Price=3_200_000 }
                };
            }
            selectionPool = pool.ToArray();

            selectionAnswer = selectionNeedsMostExpensive
                ? selectionPool.OrderByDescending(p => p.Price).First()
                : selectionPool.OrderBy(p => p.Price).First();

            SelectionInstruction.Text = selectionNeedsMostExpensive
                ? "Hãy chọn sản phẩm ĐẮT NHẤT để nhận thưởng thêm thời gian"
                : "Hãy chọn sản phẩm RẺ NHẤT để nhận thưởng thêm thời gian";

            SelectionList.ItemsSource = selectionPool
                .Select((p, idx) => new SelectionDisplay(idx, p.Name, TryLoadImage(p)))
                .ToList();

            SelectionPanel.Visibility = Visibility.Visible;
            SelectionHintButton.IsEnabled = true;
            SelectionFeedback.Text = "";
            DigitsPanel.Visibility = Visibility.Collapsed;
        }

        private void SelectionPick_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            int index = (int)btn.Tag;
            var chosen = selectionPool[index];
            bool correct = chosen == selectionAnswer;
            if (correct)
            {
                SelectionFeedback.Text = "✅ Chính xác! +10 giây cho phần đoán giá.";
                timeLeft += 10;
                TimerText.Text = timeLeft.ToString();
                SoundManager.Correct();
            }
            else
            {
                SelectionFeedback.Text = $"❌ Sai! Đáp án là {selectionAnswer.Name}. Bạn không nhận thêm thời gian.";
                SoundManager.Wrong();
            }

            SelectionPanel.Visibility = Visibility.Collapsed;
            SelectionHintButton.IsEnabled = false;

            InitializeDigitsPhase();
        }

        private void SelectionHint_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.Hint))
            {
                MessageBox.Show("Bạn không đủ thẻ gợi ý.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ordered = selectionNeedsMostExpensive
                ? selectionPool.OrderByDescending(p => p.Price).Take(2).Select(p => p.Name)
                : selectionPool.OrderBy(p => p.Price).Take(2).Select(p => p.Name);
            SelectionFeedback.Text = "🔍 Gợi ý: Đáp án nằm trong " + string.Join(" hoặc ", ordered);
        }

        private void InitializeDigitsPhase()
        {
            DigitsPanel.Visibility = Visibility.Visible;
            SelectionFeedback.Text = string.Empty;

            if (!timer.IsEnabled)
            {
                timer.Start();
            }

            current = GameState.Catalog.Count > 0
                ? GameState.Catalog[GameState.Rnd.Next(GameState.Catalog.Count)]
                : new Product { Name = "Sản phẩm demo", Price = 2_890_000, Image = null };

            ProductName.Text = $"Đoán 4 chữ số cho giá (x.000 ₫): {current.Name}";
            price4 = Math.Max(1000, current.Price) / 1000;

            ProductImage.Source = TryLoadImage(current);

            D1.Text = D2.Text = D3.Text = D4.Text = string.Empty;
            Hint.Text = string.Empty;
            doubleRewardActive = false;
            DigitDoubleButton.IsEnabled = true;
            DigitHintButton.IsEnabled = true;
            DigitSwapButton.IsEnabled = true;
            D1.Focus();
        }

        private BitmapImage? TryLoadImage(Product product)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(product.ImageUrl) &&
                    Uri.IsWellFormedUriString(product.ImageUrl, UriKind.Absolute))
                {
                    return new BitmapImage(new Uri(product.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(product.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", product.Image);
                    if (File.Exists(path))
                        return new BitmapImage(new Uri(path));
                }
            }
            catch { }
            return null;
        }

        private void DigitHint_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.Hint))
            {
                MessageBox.Show("Bạn không đủ thẻ gợi ý.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string target = price4.ToString("0000");
            int revealIndex = GameState.Rnd.Next(4);
            char digit = target[revealIndex];
            Hint.Text = $"🔍 Gợi ý: Chữ số thứ {revealIndex + 1} là {digit}.";
            switch (revealIndex)
            {
                case 0: D1.Text = digit.ToString(); break;
                case 1: D2.Text = digit.ToString(); break;
                case 2: D3.Text = digit.ToString(); break;
                case 3: D4.Text = digit.ToString(); break;
            }
        }

        private void DigitSwap_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.SwapProduct))
            {
                MessageBox.Show("Bạn không đủ thẻ đổi sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Hint.Text = "🔄 Đã đổi sang sản phẩm khác.";
            InitializeDigitsPhase();
        }

        private void DigitDouble_Click(object sender, RoutedEventArgs e)
        {
            if (doubleRewardActive)
            {
                MessageBox.Show("Bạn đã kích hoạt nhân đôi thưởng rồi!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!GameState.UseHelpCard(HelpCardType.DoubleReward))
            {
                MessageBox.Show("Bạn không đủ thẻ nhân đôi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            doubleRewardActive = true;
            Hint.Text = "✨ Nếu trả lời đúng bạn sẽ được nhân đôi thưởng.";
        }

        private record SelectionDisplay(int Index, string Name, BitmapImage? Image);
    }
}
