using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round1Window : Window
    {
        private const int TotalQuestions = 6;
        private int questionIndex = 0;
        private int correct = 0;
        private Product current = null!;
        private int qty = 1;
        private int correctPrice = 0;
        private int referencePrice = 0;
        private bool hintUsedThisQuestion;

        public Round1Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();
            NextQuestion();
        }

        private void NextQuestion(bool advance = true)
        {
            if (advance)
            {
                questionIndex++;
            }

            if (questionIndex > TotalQuestions)
            {
                OpenPunchBoard();
                return;
            }

            RoundProgText.Text = $"Câu {questionIndex}/{TotalQuestions}";

            hintUsedThisQuestion = false;
            RangeHint.Text = string.Empty;
            Feedback.Text = string.Empty;
            LowerButton.IsEnabled = true;
            HigherButton.IsEnabled = true;

            // pick product
            current = GameState.Catalog.Count > 0
                ? GameState.Catalog[GameState.Rnd.Next(GameState.Catalog.Count)]
                : new Product { Name = "Sản phẩm", Price = 1_000_000 };

            qty = GameState.Rnd.Next(1, 5);
            correctPrice = current.Price * qty;
            referencePrice = GenerateReferencePrice(correctPrice);

            // UI text
            ProductName.Text = $"{current.Name} x{qty}";
            Question.Text = "Theo bạn giá bán thật đang cao hơn hay thấp hơn mốc gợi ý?";
            ReferencePriceText.Text = $"{referencePrice:N0} ₫";

            // description (nếu có), fallback câu mặc định
            ProductDesc.Text = GetDescriptionOrDefault(current);

            // image
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

            // mô tả
            ProductDesc.Text = string.IsNullOrWhiteSpace(current.Description)
                ? "Chưa có mô tả cho sản phẩm này."
                : current.Description;

            AnimateProduct();
            RefreshHud();
        }

        // Lấy mô tả nếu Product có property "Description" (nullable) hoặc trả về fallback
        private static string GetDescriptionOrDefault(Product p)
        {
            try
            {
                var prop = p.GetType().GetProperty("Description"); // hỗ trợ nếu anh thêm vào model sau này
                var val = prop?.GetValue(p) as string;
                if (!string.IsNullOrWhiteSpace(val)) return val!;
            }
            catch { /* ignore */ }
            return "Chưa có mô tả cho sản phẩm này.";
        }

        private async Task EvaluateAsync(bool guessHigher)
        {
            HigherButton.IsEnabled = false;
            LowerButton.IsEnabled = false;

            var actualHigher = correctPrice > referencePrice;
            if (actualHigher == guessHigher)
            {
                correct++;
                var diff = Math.Abs(correctPrice - referencePrice);
                Feedback.Text = $"✅ Chính xác! Giá thật {correctPrice:N0} ₫ {(actualHigher ? "cao hơn" : "thấp hơn")} mốc {referencePrice:N0} ₫ khoảng {diff:N0} ₫.";
                SoundManager.Correct();
            }
            else
            {
                Feedback.Text = $"❌ Chưa đúng. Giá thật là {correctPrice:N0} ₫ {(actualHigher ? "cao hơn" : "thấp hơn")} mốc gợi ý.";
                SoundManager.Wrong();
            }
            RefreshHud();

            await Task.Delay(1200);
            Feedback.Text = string.Empty;
            NextQuestion();
        }

        private async void HigherButton_Click(object sender, RoutedEventArgs e) => await EvaluateAsync(true);

        private async void LowerButton_Click(object sender, RoutedEventArgs e) => await EvaluateAsync(false);

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (hintUsedThisQuestion)
            {
                Feedback.Text = "Bạn đã dùng gợi ý cho câu này.";
                return;
            }

            if (!EnsureCardAvailable(PowerCardType.Hint, 5, "Gợi ý"))
                return;

            hintUsedThisQuestion = true;
            var difference = Math.Abs(correctPrice - referencePrice);
            var percent = difference / (double)referencePrice;
            RangeHint.Text = $"👉 Giá thật chênh khoảng {difference:N0} ₫ (~{percent:P0}) so với mốc gợi ý.";
            Feedback.Text = "Đã kích hoạt thẻ gợi ý!";
            RefreshHud();
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.SwapProduct, 8, "Đổi sản phẩm"))
                return;

            NextQuestion(advance: false);
            Feedback.Text = "Đã đổi sang sản phẩm mới.";
            RefreshHud();
        }

        private void DoubleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.DoubleReward, 10, "Nhân đôi"))
                return;

            GameState.QueueDoubleReward();
            Feedback.Text = "⭐ Phần thưởng kế tiếp sẽ được nhân đôi!";
            RefreshHud();
        }

        private void Finish_Click(object sender, RoutedEventArgs e) => OpenPunchBoard();

        private void OpenPunchBoard()
        {
            var pb = new PunchBoardWindow(correct) { Owner = this };
            pb.ShowDialog();
            DialogResult = true;
            Close();
        }

        private void RefreshHud()
        {
            CorrectCount.Text = $"{correct}/{TotalQuestions}";
            CoinText.Text = GameState.Coins.ToString();
            CardText.Text = $"Gợi ý {GameState.GetCardCount(PowerCardType.Hint)} • Đổi {GameState.GetCardCount(PowerCardType.SwapProduct)} • x2 {GameState.GetCardCount(PowerCardType.DoubleReward)}";
        }

        private bool EnsureCardAvailable(PowerCardType type, int coinCost, string cardLabel)
        {
            if (GameState.TryUsePowerCard(type))
            {
                return true;
            }

            if (GameState.Coins < coinCost)
            {
                MessageBox.Show("Bạn không còn thẻ và cũng không đủ xu để mua thêm.", cardLabel, MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var confirm = MessageBox.Show($"Mua thẻ {cardLabel} với {coinCost} xu?", "Mua thẻ", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return false;
            }

            if (!GameState.TrySpendCoins(coinCost))
            {
                MessageBox.Show("Xu hiện có không đủ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            GameState.AddPowerCard(type);
            GameState.TryUsePowerCard(type);
            return true;
        }

        private static int GenerateReferencePrice(int actualPrice)
        {
            if (actualPrice <= 0)
            {
                return 100_000;
            }

            var percent = GameState.Rnd.Next(12, 28) / 100.0; // 12% - 27%
            var pushHigher = GameState.Rnd.Next(2) == 0;
            var candidate = (int)Math.Round(actualPrice * (1 + (pushHigher ? percent : -percent)));
            if (candidate <= 0)
            {
                candidate = Math.Max(1000, actualPrice - 50_000);
            }

            if (candidate == actualPrice)
            {
                candidate += pushHigher ? (int)Math.Round(actualPrice * 0.05) : -(int)Math.Round(actualPrice * 0.05);
            }

            return Math.Max(1000, candidate);
        }

        private void AnimateProduct()
        {
            try
            {
                if (FindResource("RevealStoryboard") is Storyboard storyboard)
                {
                    var sb = storyboard.Clone();
                    foreach (var anim in sb.Children)
                    {
                        Storyboard.SetTarget(anim, ProductImage);
                    }
                    SoundManager.Reveal();
                    sb.Begin();
                }
            }
            catch
            {
                // ignore animation errors
            }
        }
    }
}
