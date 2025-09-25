using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round5Window : Window
    {
        private readonly List<FinalCard> _cards = new();
        private bool _doubleActive = false;
        private const int BaseReward = 700_000;
        private const int ProtectedConsolation = 300_000;

        public Round5Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            BuildCards();
        }

        private void RefreshHud()
        {
            CoinText.Text = GameState.QuickCoins.ToString();
            CardText.Text = string.Join(", ", GameState.GetInventory().Where(kv => kv.Value > 0)
                .Select(kv => $"{GetCardName(kv.Key)} x{kv.Value}"));
            if (string.IsNullOrWhiteSpace(CardText.Text))
            {
                CardText.Text = "Chưa có";
            }
            HintButton.IsEnabled = GameState.GetHelpCount(HelpCardType.Hint) > 0;
            SwapButton.IsEnabled = GameState.GetHelpCount(HelpCardType.SwapProduct) > 0;
            DoubleButton.IsEnabled = !_doubleActive && GameState.GetHelpCount(HelpCardType.DoubleReward) > 0;
        }

        private static string GetCardName(HelpCardType type) => type switch
        {
            HelpCardType.Hint => "Gợi ý",
            HelpCardType.SwapProduct => "Đổi",
            HelpCardType.DoubleReward => "Nhân đôi",
            _ => type.ToString()
        };

        private void BuildCards()
        {
            _cards.Clear();
            ProductsPanel.Children.Clear();

            var picks = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(5).ToList();
            if (picks.Count < 3)
            {
                picks = new List<Product>
                {
                    new Product { Name = "Máy lọc nước", Price = 5_200_000 },
                    new Product { Name = "Bếp từ đôi", Price = 7_800_000 },
                    new Product { Name = "Lò nướng", Price = 3_400_000 },
                    new Product { Name = "Máy hút mùi", Price = 4_900_000 }
                };
            }

            int count = Math.Min(5, Math.Max(3, picks.Count));
            picks = picks.Take(count).ToList();

            for (int i = 0; i < picks.Count; i++)
            {
                var product = picks[i];
                var card = CreateCard(product, i, count);
                _cards.Add(card);
                ProductsPanel.Children.Add(card.Container);
            }
        }

        private FinalCard CreateCard(Product product, int index, int total)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                Margin = new Thickness(12),
                Padding = new Thickness(12),
                Width = 240
            };

            var stack = new StackPanel();
            var img = new Image { Height = 160, Stretch = System.Windows.Media.Stretch.UniformToFill, Margin = new Thickness(0, 0, 0, 8) };
            img.Source = TryLoadImage(product);
            var name = new TextBlock { Text = product.Name, FontWeight = FontWeights.Bold, FontSize = 18, TextWrapping = TextWrapping.Wrap };
            var orderBox = new ComboBox { Margin = new Thickness(0, 10, 0, 0), FontSize = 16 };
            for (int i = 1; i <= total; i++) orderBox.Items.Add(i);
            orderBox.SelectedIndex = -1;
            var protect = new RadioButton { Content = "Bảo toàn", GroupName = "ProtectGroup", Margin = new Thickness(0, 10, 0, 0) };
            var status = new TextBlock { Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap };

            stack.Children.Add(img);
            stack.Children.Add(name);
            stack.Children.Add(orderBox);
            stack.Children.Add(protect);
            stack.Children.Add(status);

            border.Child = stack;

            return new FinalCard(product, orderBox, protect, status, border);
        }

        private static BitmapImage? TryLoadImage(Product p)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(p.ImageUrl) && Uri.IsWellFormedUriString(p.ImageUrl, UriKind.Absolute))
                {
                    return new BitmapImage(new Uri(p.ImageUrl));
                }
                if (!string.IsNullOrWhiteSpace(p.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", p.Image);
                    if (File.Exists(path))
                        return new BitmapImage(new Uri(path));
                }
            }
            catch { }
            return null;
        }

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.Hint))
            {
                Feedback.Text = "Bạn không còn thẻ gợi ý.";
                return;
            }

            var product = _cards[GameState.Rnd.Next(_cards.Count)].Product;
            int span = (int)(product.Price * 0.12);
            Feedback.Text = $"🔍 Gợi ý: {product.Name} có giá khoảng {product.Price - span:N0} ₫ - {product.Price + span:N0} ₫.";
            RefreshHud();
        }

        private void Swap_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.SwapProduct))
            {
                Feedback.Text = "Bạn không còn thẻ đổi sản phẩm.";
                return;
            }

            Feedback.Text = "🔄 Đã đổi danh sách sản phẩm.";
            BuildCards();
            RefreshHud();
        }

        private void Double_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.DoubleReward))
            {
                Feedback.Text = "Bạn không còn thẻ nhân đôi.";
                return;
            }

            _doubleActive = true;
            Feedback.Text = "✨ Toàn bộ tiền thưởng vòng này sẽ được nhân đôi nếu xếp đúng.";
            DoubleButton.IsEnabled = GameState.GetHelpCount(HelpCardType.DoubleReward) > 0;
            RefreshHud();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var usedOrders = new HashSet<int>();
            foreach (var card in _cards)
            {
                if (card.Order.SelectedItem is not int order)
                {
                    Feedback.Text = "Hãy chọn đầy đủ thứ tự cho từng sản phẩm.";
                    return;
                }
                if (!usedOrders.Add(order))
                {
                    Feedback.Text = "Mỗi thứ tự chỉ được dùng một lần.";
                    return;
                }
            }

            var sorted = _cards.OrderBy(c => c.Product.Price).ToList();
            int reward = 0;

            foreach (var card in _cards)
            {
                int chosen = (int)card.Order.SelectedItem!;
                int actualIndex = sorted.IndexOf(sorted.First(c => c.Product == card.Product)) + 1;
                if (chosen == actualIndex)
                {
                    int add = BaseReward;
                    if (_doubleActive) add *= 2;
                    reward += add;
                    card.Status.Text = $"✅ Đúng vị trí! +{add:N0} ₫";
                }
                else if (card.Protect.IsChecked == true)
                {
                    int add = ProtectedConsolation;
                    reward += add;
                    card.Status.Text = $"🛡️ Bảo toàn: +{add:N0} ₫ (vị trí đúng là {actualIndex})";
                }
                else
                {
                    card.Status.Text = $"❌ Sai. Vị trí đúng: {actualIndex}";
                }
            }

            GameState.TotalPrize += reward;
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            Feedback.Text = reward > 0
                ? $"Bạn nhận thêm {reward:N0} ₫ ở vòng cuối!"
                : "Bạn chưa nhận thêm tiền thưởng ở vòng này.";
            SoundManager.Correct();
            LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
            this.DialogResult = reward > 0;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private record FinalCard(Product Product, ComboBox Order, RadioButton Protect, TextBlock Status, Border Container);
    }
}
