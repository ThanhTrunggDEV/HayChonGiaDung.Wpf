using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round4Window : Window
    {
        private int correctDisplayIndex;
        private Product[] trio = new Product[3];

        public Round4Window()
        {
            InitializeComponent();
            SoundManager.StartRound();

            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";

            // Lấy 3 sản phẩm CẬN KỀ GIÁ: sort theo giá rồi pick 3 món liên tiếp
            var all = GameState.Catalog.OrderBy(p => p.Price).ToList();
            if (all.Count >= 3)
            {
                int start = GameState.Rnd.Next(0, Math.Max(1, all.Count - 2));
                trio[0] = all[start];
                trio[1] = all[start + 1];
                trio[2] = all[start + 2];
            }
            else
            {
                // fallback nếu thiếu data
                trio = new[]
                {
                    new Product { Name = "SP A", Price = 1200000 },
                    new Product { Name = "SP B", Price = 1800000 },
                    new Product { Name = "SP C", Price = 2400000 }
                };
            }

            // Tính median (giữa) theo giá
            var median = trio.OrderBy(p => p.Price).ElementAt(1);

            // Xáo trộn vị trí hiển thị để tránh đoán theo vị trí
            int[] order = new[] { 0, 1, 2 }.OrderBy(_ => GameState.Rnd.Next()).ToArray();
            correctDisplayIndex = Array.FindIndex(order, i => trio[i] == median);

            // Render 3 card
            GridTrio.Children.Clear();
            for (int col = 0; col < 3; col++)
            {
                var sp = trio[order[col]];
                GridTrio.Children.Add(BuildCard(sp, col));
            }
        }

        private UIElement BuildCard(Product sp, int displayIndex)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(16),
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
                Text = $"{sp.Price:N0} ₫",
                Opacity = 0.9,
                FontSize = 16
            };

            var btn = new Button
            {
                Content = "Chọn sản phẩm này",
                Tag = displayIndex,
                Margin = new Thickness(0, 10, 0, 0)
            };
            btn.Click += Pick;

            stack.Children.Add(img);
            stack.Children.Add(name);
            stack.Children.Add(price);
            stack.Children.Add(btn);

            card.Child = stack;
            return card;
        }

        private void Pick(object sender, RoutedEventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            if (idx == correctDisplayIndex)
            {
                GameState.TotalPrize += 2_000_000;
                PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
                SoundManager.Correct();
                MessageBox.Show("Quá thông minh! +2.000.000 ₫", "Lựa Chọn Thông Minh");
                LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
                this.DialogResult = true;
                Close();
            }
            else
            {
                SoundManager.Wrong();
                MessageBox.Show("Sai nước đi! Vòng này 0 ₫.", "Lựa Chọn Thông Minh");
                LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
                this.DialogResult = false;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}
