using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round3Window : Window
    {
        private readonly List<Border> _cards = new();
        private int fakeIndex;
        private Product[] group = new Product[6];
        private int[] displayPrices = new int[6];
        private bool picked;
        private bool hintUsed;

        public Round3Window()
        {
            InitializeComponent();
            SoundManager.StartRound();

            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            GenerateRound();
        }

        private void GenerateRound()
        {
            picked = false;
            hintUsed = false;
            Feedback.Text = string.Empty;

            var pool = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(6).ToArray();
            if (pool.Length < 6)
            {
                pool = new[]
                {
                    new Product{ Name="SP A", Price=8_500_000 },
                    new Product{ Name="SP B", Price=9_200_000 },
                    new Product{ Name="SP C", Price=7_800_000 },
                    new Product{ Name="SP D", Price=8_100_000 },
                    new Product{ Name="SP E", Price=7_900_000 },
                    new Product{ Name="SP F", Price=8_000_000 }
                };
            }

            for (int i = 0; i < 6; i++) group[i] = pool[i];

            fakeIndex = GameState.Rnd.Next(6);
            for (int i = 0; i < 6; i++)
            {
                if (i == fakeIndex)
                {
                    var basePrice = group[i].Price;
                    var sign = GameState.Rnd.Next(2) == 0 ? -1 : 1;
                    var pct = GameState.Rnd.Next(30, 61) / 100.0;
                    int altered = (int)Math.Max(1000, basePrice + sign * basePrice * pct);
                    if (altered == basePrice) altered += 10000;
                    displayPrices[i] = altered;
                }
                else
                {
                    displayPrices[i] = group[i].Price;
                }
            }

            RenderCards();
        }

        private void RenderCards()
        {
            GridProducts.Children.Clear();
            _cards.Clear();

            for (int i = 0; i < 6; i++)
            {
                var sp = group[i];
                int shownPrice = displayPrices[i];

                var card = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.DimGray,
                    Margin = new Thickness(10),
                    Padding = new Thickness(12),
                    Opacity = 0
                };

                var stack = new StackPanel();

                var img = new Image { Height = 180, Stretch = Stretch.UniformToFill };
                if (!string.IsNullOrWhiteSpace(sp.ImageUrl) && Uri.IsWellFormedUriString(sp.ImageUrl, UriKind.Absolute))
                {
                    img.Source = new BitmapImage(new Uri(sp.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(sp.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", sp.Image);
                    if (File.Exists(path))
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
                _cards.Add(card);

                AnimateCard(card);
            }

            SoundManager.Reveal();
        }

        private async void Pick(object sender, RoutedEventArgs e)
        {
            if (picked) return;
            picked = true;

            int idx = (int)((Button)sender).Tag;
            bool correct = idx == fakeIndex;

            HighlightSelection(idx, correct);

            foreach (var button in GridProducts.Children.OfType<Border>().SelectMany(b => b.Child is StackPanel sp ? sp.Children.OfType<Button>() : Array.Empty<Button>()))
            {
                button.IsEnabled = false;
            }

            if (correct)
            {
                GameState.AddPrize(1_500_000);
                PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
                SoundManager.Correct();
                Feedback.Text = "✅ Chuẩn bài! +1.500.000 ₫";
                RefreshHud();
                await Task.Delay(1000);
                DialogResult = true;
                Close();
            }
            else
            {
                SoundManager.Wrong();
                Feedback.Text = $"❌ Sai mất rồi! Giá đúng của {group[fakeIndex].Name} là {group[fakeIndex].Price:N0} ₫.";
                RefreshHud();
                await Task.Delay(1000);
                DialogResult = false;
                Close();
            }
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (hintUsed)
            {
                Feedback.Text = "Bạn đã dùng gợi ý.";
                return;
            }

            if (!EnsureCardAvailable(PowerCardType.Hint, 5, "Gợi ý"))
                return;

            hintUsed = true;
            var highlightCount = 3;
            var highlight = new HashSet<int> { fakeIndex };
            while (highlight.Count < highlightCount)
            {
                highlight.Add(GameState.Rnd.Next(group.Length));
            }

            foreach (var index in highlight)
            {
                _cards[index].Background = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255));
            }

            Feedback.Text = "Một trong các thẻ được tô xanh là đáp án sai giá.";
            RefreshHud();
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.SwapProduct, 8, "Đổi bộ"))
                return;

            Feedback.Text = "Đã đổi sang bộ sản phẩm mới.";
            RefreshHud();
            GenerateRound();
        }

        private void DoubleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.DoubleReward, 10, "Nhân đôi"))
                return;

            GameState.QueueDoubleReward();
            Feedback.Text = "⭐ Thẻ nhân đôi đã sẵn sàng.";
            RefreshHud();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void HighlightSelection(int pickedIndex, bool correct)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i == fakeIndex)
                {
                    _cards[i].BorderBrush = Brushes.LimeGreen;
                    _cards[i].BorderThickness = new Thickness(3);
                }
                else if (i == pickedIndex && !correct)
                {
                    _cards[i].BorderBrush = Brushes.IndianRed;
                    _cards[i].BorderThickness = new Thickness(3);
                }
                else
                {
                    _cards[i].BorderBrush = Brushes.DimGray;
                    _cards[i].BorderThickness = new Thickness(1);
                }
            }
        }

        private void RefreshHud()
        {
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
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
            RefreshHud();
            return true;
        }

        private void AnimateCard(Border card)
        {
            try
            {
                var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
                card.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch
            {
                // ignore
            }
        }
    }
}
