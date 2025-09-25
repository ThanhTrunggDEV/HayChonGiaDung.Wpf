using System;
using System.IO;
using System.Linq;
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

            // UI text
            ProductName.Text = $"{current.Name} x{qty}";
            Question.Text = $"{hiddenPrice:N0} ₫ — Giá đúng CAO HƠN hay THẤP HƠN?";

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

        private void Evaluate(bool guessHigher)
        {
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
            NextQuestion();
        }

        private void Higher_Click(object sender, RoutedEventArgs e) => Evaluate(true);
        private void Lower_Click(object sender, RoutedEventArgs e) => Evaluate(false);

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
