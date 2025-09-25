using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round1Window : Window
    {
        private int questionIndex = 0;
        private int correct = 0;
        private Product current = null!;
        private int qty = 1;
        private int hiddenPrice = 0;
        private int correctPrice = 0;
        private bool rangeMode = true;
        private bool hintUsedThisQuestion = false;

        public Round1Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();
            NextQuestion();
        }

        private void NextQuestion()
        {
            questionIndex++;
            if (questionIndex > 10) { OpenPunchBoard(); return; }
            RoundProgText.Text = $"Câu {questionIndex}/10";

            // pick product
            current = GameState.Catalog.Count > 0
                ? GameState.Catalog[GameState.Rnd.Next(GameState.Catalog.Count)]
                : new Product { Name = "Sản phẩm", Price = 1_000_000 };

            qty = GameState.Rnd.Next(1, 5);
            correctPrice = current.Price * qty;

            // hidden price around correct ±20%
            var delta = (int)(correctPrice * 0.2);
            hiddenPrice = Math.Max(1000, correctPrice + GameState.Rnd.Next(-delta, delta + 1));

            rangeMode = questionIndex <= 5;
            hintUsedThisQuestion = false;

            // UI text
            ProductName.Text = $"{current.Name} x{qty}";
            ModeText.Text = rangeMode ? "±10%" : "Cao/Thấp";
            Question.Text = rangeMode
                ? $"Bạn đoán giá bao nhiêu? Sai số cho phép ±10% so với giá thật."
                : $"{hiddenPrice:N0} ₫ — Giá đúng CAO HƠN hay THẤP HƠN?";

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

            Feedback.Text = "";
            CorrectCount.Text = $"{correct}/4";

            HigherLowerPanel.Visibility = rangeMode ? Visibility.Collapsed : Visibility.Visible;
            RangePanel.Visibility = rangeMode ? Visibility.Visible : Visibility.Collapsed;
            RangeInput.Text = string.Empty;
            UpdateHelpButtons();
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

            bool isHigher = correctPrice > hiddenPrice;
            if (guessHigher == isHigher)
            {
                correct++;
                Feedback.Text = $"✅ Chuẩn! Giá đúng: {correctPrice:N0} ₫";
                SoundManager.Correct();
            }
            else
            {
                Feedback.Text = $"❌ Sai! Giá đúng: {correctPrice:N0} ₫";
                SoundManager.Wrong();
            }
            CorrectCount.Text = $"{correct}/4";

            await Task.Delay(1000);

            Feedback.Text = string.Empty;
            HigherButton.IsEnabled = true;
            LowerButton.IsEnabled = true;

            NextQuestion();
        }

        private async void Higher_Click(object sender, RoutedEventArgs e) => await EvaluateAsync(true);
        private async void Lower_Click(object sender, RoutedEventArgs e) => await EvaluateAsync(false);

        private async void RangeSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(RangeInput.Text.Replace(".", "").Replace(",", "").Trim(), out var guess) || guess <= 0)
            {
                Feedback.Text = "⚠️ Nhập giá hợp lệ (số nguyên).";
                return;
            }

            var button = (System.Windows.Controls.Button)sender;
            RangeInput.IsEnabled = false;
            button.IsEnabled = false;

            var tolerance = (int)(correctPrice * 0.1);
            if (Math.Abs(guess - correctPrice) <= tolerance)
            {
                correct++;
                Feedback.Text = $"✅ Chuẩn! Giá đúng: {correctPrice:N0} ₫";
                SoundManager.Correct();
            }
            else
            {
                Feedback.Text = $"❌ Lệch rồi! Giá đúng: {correctPrice:N0} ₫";
                SoundManager.Wrong();
            }
            CorrectCount.Text = $"{correct}/4";

            await Task.Delay(1000);

            RangeInput.IsEnabled = true;
            button.IsEnabled = true;

            NextQuestion();
        }

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            if (hintUsedThisQuestion)
            {
                Feedback.Text = "Bạn đã dùng gợi ý cho câu này.";
                return;
            }

            if (!GameState.UseHelpCard(HelpCardType.Hint))
            {
                Feedback.Text = "Bạn không còn thẻ gợi ý.";
                return;
            }

            hintUsedThisQuestion = true;

            if (rangeMode)
            {
                int tolerance = (int)(correctPrice * 0.08);
                Feedback.Text = $"🔍 Gợi ý: Giá nằm trong khoảng {correctPrice - tolerance:N0} ₫ - {correctPrice + tolerance:N0} ₫";
            }
            else
            {
                string relation = correctPrice > hiddenPrice ? "cao hơn" : "thấp hơn";
                Feedback.Text = $"🔍 Gợi ý: Giá thật {relation} số hiển thị từ {Math.Abs(correctPrice - hiddenPrice):N0} ₫";
            }

            UpdateHelpButtons();
        }

        private void Swap_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.SwapProduct))
            {
                Feedback.Text = "Bạn không còn thẻ đổi sản phẩm.";
                return;
            }

            Feedback.Text = "🔄 Đã đổi sang sản phẩm khác.";
            questionIndex--;
            NextQuestion();
        }

        private void UpdateHelpButtons()
        {
            HintButton.IsEnabled = GameState.GetHelpCount(HelpCardType.Hint) > 0 && !hintUsedThisQuestion;
            SwapButton.IsEnabled = GameState.GetHelpCount(HelpCardType.SwapProduct) > 0;
        }

        private void Finish_Click(object sender, RoutedEventArgs e) => OpenPunchBoard();

        private void OpenPunchBoard()
        {
            var pb = new PunchBoardWindow(correct) { Owner = this };
            pb.ShowDialog();
            DialogResult = true;
            Close();
        }
    }
}
