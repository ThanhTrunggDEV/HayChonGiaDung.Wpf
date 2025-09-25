using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round4Window : Window
    {
        private readonly List<Product> _products = new();
        private readonly List<int> _order = new();
        private readonly HashSet<int> _hintHighlights = new();
        private int? _protectedProductIndex;
        private bool _protectPurchased;
        private bool _hintUsed;
        private bool _locked;

        public Round4Window()
        {
            InitializeComponent();
            SoundManager.StartRound();

            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            RefreshHud();
            GenerateRound();
        }

        private void GenerateRound()
        {
            _products.Clear();
            _order.Clear();
            _hintHighlights.Clear();
            _protectedProductIndex = null;
            _protectPurchased = false;
            _hintUsed = false;
            _locked = false;
            Feedback.Text = string.Empty;
            LockButton.IsEnabled = true;
            HintButton.IsEnabled = true;
            SwapButton.IsEnabled = true;
            DoubleButton.IsEnabled = true;

            int count = Math.Min(5, Math.Max(4, GameState.Catalog.Count >= 5 ? GameState.Rnd.Next(4, 6) : 4));
            var selected = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(count).ToList();
            if (selected.Count < count)
            {
                selected = new List<Product>
                {
                    new Product { Name = "Quạt cây", Price = 1_200_000 },
                    new Product { Name = "Máy xay đa năng", Price = 2_400_000 },
                    new Product { Name = "Máy rửa chén", Price = 9_200_000 },
                    new Product { Name = "Tủ lạnh 450L", Price = 14_500_000 }
                };
            }

            _products.AddRange(selected);
            _order.AddRange(Enumerable.Range(0, _products.Count).OrderBy(_ => GameState.Rnd.Next()));

            RenderOrder();
        }
        private void RenderOrder()
        {
            OrderPanel.Children.Clear();
            for (int position = 0; position < _order.Count; position++)
            {
                int productIndex = _order[position];
                var product = _products[productIndex];

                var card = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    BorderBrush = Brushes.DimGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 6, 0, 6),
                    Padding = new Thickness(12),
                    Background = _hintHighlights.Contains(productIndex)
                        ? new SolidColorBrush(Color.FromArgb(40, 0, 191, 255))
                        : Brushes.Transparent
                };

                var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                var image = new Image
                {
                    Height = 80,
                    Width = 120,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                if (!string.IsNullOrWhiteSpace(product.ImageUrl) && Uri.IsWellFormedUriString(product.ImageUrl, UriKind.Absolute))
                {
                    image.Source = new BitmapImage(new Uri(product.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(product.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", product.Image);
                    if (File.Exists(path))
                        image.Source = new BitmapImage(new Uri(path));
                }
                Grid.SetColumn(image, 0);
                grid.Children.Add(image);

                var infoStack = new StackPanel();
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"{position + 1}. {product.Name}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 18
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(product.Description) ? "Sắp xếp từ rẻ đến đắt." : product.Description,
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetColumn(infoStack, 1);
                grid.Children.Add(infoStack);

                var buttonStack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Right };
                var upButton = new Button { Content = "▲", Tag = productIndex, Margin = new Thickness(0, 0, 0, 4), IsEnabled = position > 0 && !_locked };
                upButton.Click += MoveUp_Click;
                var downButton = new Button { Content = "▼", Tag = productIndex, Margin = new Thickness(0, 0, 0, 4), IsEnabled = position < _order.Count - 1 && !_locked };
                downButton.Click += MoveDown_Click;
                var protectButton = new Button
                {
                    Content = _protectedProductIndex == productIndex ? "ĐÃ BẢO TOÀN" : "Bảo toàn (-6 xu)",
                    Tag = productIndex,
                    Margin = new Thickness(0, 4, 0, 0),
                    IsEnabled = !_locked && (_protectedProductIndex == null || _protectedProductIndex == productIndex)
                };
                protectButton.Click += Protect_Click;
                buttonStack.Children.Add(upButton);
                buttonStack.Children.Add(downButton);
                buttonStack.Children.Add(protectButton);
                Grid.SetColumn(buttonStack, 2);
                grid.Children.Add(buttonStack);

                card.Child = grid;
                OrderPanel.Children.Add(card);
            }
        }
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_locked) return;
            int productIndex = (int)((Button)sender).Tag;
            int currentPos = _order.IndexOf(productIndex);
            if (currentPos <= 0) return;
            (_order[currentPos - 1], _order[currentPos]) = (_order[currentPos], _order[currentPos - 1]);
            RenderOrder();
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_locked) return;
            int productIndex = (int)((Button)sender).Tag;
            int currentPos = _order.IndexOf(productIndex);
            if (currentPos >= _order.Count - 1) return;
            (_order[currentPos + 1], _order[currentPos]) = (_order[currentPos], _order[currentPos + 1]);
            RenderOrder();
        }

        private void Protect_Click(object sender, RoutedEventArgs e)
        {
            if (_locked) return;
            int productIndex = (int)((Button)sender).Tag;

            if (_protectedProductIndex == productIndex)
            {
                _protectedProductIndex = null;
                Feedback.Text = "Đã bỏ bảo toàn cho sản phẩm.";
                RenderOrder();
                RefreshHud();
                return;
            }

            if (!_protectPurchased)
            {
                if (GameState.Coins < 6)
                {
                    MessageBox.Show("Bạn không đủ xu để bảo toàn sản phẩm.", "Bảo toàn", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var confirm = MessageBox.Show("Bảo toàn sản phẩm này với 6 xu?", "Bảo toàn", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                if (!GameState.TrySpendCoins(6))
                {
                    MessageBox.Show("Không thể trừ xu lúc này.", "Bảo toàn", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _protectPurchased = true;
            }

            _protectedProductIndex = productIndex;
            Feedback.Text = $"Đã bảo toàn { _products[productIndex].Name }. Nếu sai vị trí vẫn giữ 500.000 ₫.";
            RenderOrder();
            RefreshHud();
        }
        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hintUsed)
            {
                Feedback.Text = "Bạn đã dùng gợi ý.";
                return;
            }

            if (!EnsureCardAvailable(PowerCardType.Hint, 5, "Gợi ý"))
                return;

            _hintUsed = true;
            var sorted = _products.Select((p, i) => new { Index = i, p.Price }).OrderBy(p => p.Price).ToList();
            var chosen = sorted[GameState.Rnd.Next(sorted.Count)];
            int targetPos = sorted.IndexOf(chosen);
            _hintHighlights.Add(chosen.Index);
            Feedback.Text = $"Gợi ý: { _products[chosen.Index].Name } nên đứng ở vị trí số {targetPos + 1}.";
            RenderOrder();
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
            Feedback.Text = "⭐ Thẻ nhân đôi đã kích hoạt.";
            RefreshHud();
        }

        private async void LockButton_Click(object sender, RoutedEventArgs e)
        {
            if (_locked) return;
            _locked = true;

            LockButton.IsEnabled = false;
            HintButton.IsEnabled = false;
            SwapButton.IsEnabled = false;
            DoubleButton.IsEnabled = false;
            RenderOrder();

            var sorted = _products.Select((p, i) => new { Index = i, p.Price })
                                   .OrderBy(p => p.Price)
                                   .Select(p => p.Index)
                                   .ToList();

            int correct = 0;
            int totalReward = 0;
            for (int pos = 0; pos < _order.Count; pos++)
            {
                int productIndex = _order[pos];
                if (sorted[pos] == productIndex)
                {
                    correct++;
                    totalReward += 1_500_000;
                }
                else if (_protectedProductIndex.HasValue && _protectedProductIndex.Value == productIndex)
                {
                    totalReward += 500_000;
                }
            }

            if (totalReward > 0)
            {
                GameState.AddPrize(totalReward);
                PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            }

            var sortedNames = string.Join(" < ", sorted.Select(i => _products[i].Name));
            Feedback.Text = $"Bạn đặt đúng {correct}/{_order.Count} vị trí. Thưởng: {totalReward:N0} ₫. Thứ tự chuẩn: {sortedNames}.";

            if (correct == _order.Count)
            {
                SoundManager.Correct();
            }
            else if (totalReward > 0)
            {
                SoundManager.Correct();
            }
            else
            {
                SoundManager.Wrong();
            }

            HighlightResults(sorted);
            RefreshHud();
            LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
            await Task.Delay(1200);
            DialogResult = true;
            Close();
        }
        private void HighlightResults(IReadOnlyList<int> sorted)
        {
            for (int pos = 0; pos < _order.Count; pos++)
            {
                if (OrderPanel.Children[pos] is Border border)
                {
                    int productIndex = _order[pos];
                    if (sorted[pos] == productIndex)
                    {
                        border.BorderBrush = Brushes.LimeGreen;
                        border.BorderThickness = new Thickness(3);
                    }
                    else if (_protectedProductIndex.HasValue && _protectedProductIndex.Value == productIndex)
                    {
                        border.BorderBrush = Brushes.Goldenrod;
                        border.BorderThickness = new Thickness(3);
                    }
                    else
                    {
                        border.BorderBrush = Brushes.IndianRed;
                        border.BorderThickness = new Thickness(3);
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
            DialogResult = false;
            Close();
        }

        private void RefreshHud()
        {
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
    }
}
