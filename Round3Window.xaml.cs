using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round3Window : Window
    {
        private int fakeIndex;
        private Product[] group = new Product[6];
        private int[] displayPrices = new int[6];
        private bool picked = false;

        public Round3Window()
        {
            InitializeComponent();
            SoundManager.StartRound();

            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";

            // Lấy 6 sản phẩm ngẫu nhiên (unique)
            var pool = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(6).ToArray();
            if (pool.Length < 6)
            {
                // fallback nếu ít data
                pool = new[]
                {
                    new Product{ Name="SP A", Price=8500000 },
                    new Product{ Name="SP B", Price=9200000 },
                    new Product{ Name="SP C", Price=7800000 },
                    new Product{ Name="SP D", Price=8100000 },
                    new Product{ Name="SP E", Price=7900000 },
                    new Product{ Name="SP F", Price=8000000 }
                };
            }
            for (int i = 0; i < 6; i++) group[i] = pool[i];

            // Chọn 1 index để "làm sai giá"
            fakeIndex = GameState.Rnd.Next(6);

            // Tính giá hiển thị: đúng cho 5 cái, "lệch" cho 1 cái
            for (int i = 0; i < 6; i++)
            {
                if (i == fakeIndex)
                {
                    // lệch ~ ±30%–60% cho lộ liễu hơn
                    var basePrice = group[i].Price;
                    var sign = GameState.Rnd.Next(2) == 0 ? -1 : 1;
                    var pct = GameState.Rnd.Next(30, 61) / 100.0; // 30%..60%
                    int altered = (int)Math.Max(1000, basePrice + sign * basePrice * pct);
                    if (altered == basePrice) altered += 10000;
                    displayPrices[i] = altered;
                }
                else
                {
                    displayPrices[i] = group[i].Price;
                }
            }

            // Render cards
            RenderCards();
        }

        private void RenderCards()
        {
            GridProducts.Children.Clear();
            for (int i = 0; i < 6; i++)
            {
                var sp = group[i];
                int shownPrice = displayPrices[i];

                var card = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.DimGray,
                    Margin = new Thickness(10),
                    Padding = new Thickness(12)
                };

                var stack = new StackPanel();

                // Ảnh sản phẩm
                var img = new Image { Height = 180, Stretch = System.Windows.Media.Stretch.UniformToFill };
                if (!string.IsNullOrWhiteSpace(sp.ImageUrl) &&
                    Uri.IsWellFormedUriString(sp.ImageUrl, UriKind.Absolute))
                {
                    img.Source = new BitmapImage(new Uri(sp.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(sp.Image))
                {
                    string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", sp.Image);
                    if (System.IO.File.Exists(path))
                        img.Source = new BitmapImage(new Uri(path));
                }

              

                var name = new TextBlock
                {
                    Text = sp.Name,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.Bold,
                    FontSize = 18,
                    Margin = new Thickness(0, 8, 0, 4)
                };

                var price = new TextBlock
                {
                    Text = $"{shownPrice:N0} ₫",
                    Opacity = 0.9,
                    FontSize = 16
                };

                var btn = new Button
                {
                    Content = "Chọn sản phẩm này",
                    Tag = i,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                btn.Click += Pick;

                stack.Children.Add(img);
                stack.Children.Add(name);
                stack.Children.Add(price);
                stack.Children.Add(btn);

                card.Child = stack;
                GridProducts.Children.Add(card);
            }
        }

        private async void Pick(object sender, RoutedEventArgs e)
        {
            if (picked) return; // chặn double click
            picked = true;

            int idx = (int)((Button)sender).Tag;
            bool correct = idx == fakeIndex;

            if (correct)
            {
                GameState.TotalPrize += 1_500_000;
                PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
                SoundManager.Correct();
                Feedback.Text = "✅ Chuẩn bài! +1.500.000 ₫";
                await Task.Delay(1000);
                this.DialogResult = true;
                Close();
            }
            else
            {
                SoundManager.Wrong();
                Feedback.Text = "❌ Sai mất rồi! Vòng này 0 ₫.";
                await Task.Delay(1000);
                this.DialogResult = false;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Nếu người chơi thoát giữa chừng coi như bỏ vòng này
            this.DialogResult = false;
            Close();
        }
    }
}
