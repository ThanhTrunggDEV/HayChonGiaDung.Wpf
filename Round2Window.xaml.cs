using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round2Window : Window
    {
        private readonly List<Border> _cards = new();
        private Product[] _group = Array.Empty<Product>();
        private int _targetIndex;
        private bool _findHighest;
        private bool _hintUsed;
        private bool _locked;

        public Round2Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();
            RefreshHud();
            GenerateRound();
        }

        private void GenerateRound()
        {
            _locked = false;
            _hintUsed = false;
            _findHighest = GameState.Rnd.Next(2) == 0;
            InstructionText.Text = _findHighest
                ? "Chọn sản phẩm có giá CAO NHẤT trong nhóm."
                : "Chọn sản phẩm có giá THẤP NHẤT trong nhóm.";

            _group = GameState.Catalog.OrderBy(_ => GameState.Rnd.Next()).Take(4).ToArray();
            if (_group.Length < 4)
            {
                _group = new[]
                {
                    new Product{ Name="Lò vi sóng", Price=1_600_000, Description="Mẫu tham chiếu."},
                    new Product{ Name="Bàn ủi hơi nước", Price=950_000, Description="Mẫu tham chiếu."},
                    new Product{ Name="Tủ lạnh 300L", Price=9_500_000, Description="Mẫu tham chiếu."},
                    new Product{ Name="Máy hút bụi", Price=3_200_000, Description="Mẫu tham chiếu."}
                };
            }

            _targetIndex = _findHighest
                ? Array.IndexOf(_group, _group.OrderBy(p => p.Price).Last())
                : Array.IndexOf(_group, _group.OrderBy(p => p.Price).First());

            RenderCards();
            Feedback.Text = string.Empty;
        }

        private void RenderCards()
        {
            ProductPanel.Children.Clear();
            _cards.Clear();

            for (int i = 0; i < _group.Length; i++)
            {
                var p = _group[i];
                var card = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Transparent,
                    Margin = new Thickness(12),
                    Padding = new Thickness(12),
                    Background = Brushes.White,
                    Opacity = 0
                };

                var stack = new StackPanel();

                var img = new Image { Height = 160, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 0, 8) };
                if (!string.IsNullOrWhiteSpace(p.ImageUrl) && Uri.IsWellFormedUriString(p.ImageUrl, UriKind.Absolute))
                {
                    img.Source = new BitmapImage(new Uri(p.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(p.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", p.Image);
                    if (File.Exists(path))
                        img.Source = new BitmapImage(new Uri(path));
                }

                var name = new TextBlock
                {
                    Text = p.Name,
                    FontWeight = FontWeights.Bold,
                    FontSize = 18,
                    TextWrapping = TextWrapping.Wrap
                };

                var desc = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(p.Description) ? "Sản phẩm thuộc danh mục gia dụng." : p.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 6),
                    Opacity = 0.75
                };

                var btn = new Button
                {
                    Content = "Chọn sản phẩm này",
                    Margin = new Thickness(0, 8, 0, 0),
                    Tag = i
                };
                btn.Click += Pick;

                stack.Children.Add(img);
                stack.Children.Add(name);
                stack.Children.Add(desc);
                stack.Children.Add(btn);

                card.Child = stack;
                ProductPanel.Children.Add(card);
                _cards.Add(card);

                AnimateCard(card);
            }

            SoundManager.Reveal();
        }

        private async void Pick(object sender, RoutedEventArgs e)
        {
            if (_locked) return;
            _locked = true;

            var btn = (Button)sender;
            int pickIndex = (int)btn.Tag;

            foreach (var button in ProductPanel.Children.OfType<Border>().SelectMany(b => b.Child is StackPanel sp ? sp.Children.OfType<Button>() : Array.Empty<Button>()))
            {
                button.IsEnabled = false;
            }

            var targetProduct = _group[_targetIndex];
            bool correct = pickIndex == _targetIndex;
            HighlightResult(pickIndex, correct);

            if (correct)
            {
                GameState.AddPrize(1_200_000);
                Feedback.Text = $"✅ Chuẩn! {targetProduct.Name} có giá {targetProduct.Price:N0} ₫ (+1.200.000 ₫)";
                SoundManager.Correct();
            }
            else
            {
                var chosen = _group[pickIndex];
                Feedback.Text = $"❌ Chưa đúng. {targetProduct.Name} mới là đáp án ({targetProduct.Price:N0} ₫). Bạn chọn {chosen.Name} ({chosen.Price:N0} ₫).";
                SoundManager.Wrong();
            }

            RefreshHud();
            await Task.Delay(1500);
            DialogResult = correct;
            Close();
        }

        private void HighlightResult(int pickedIndex, bool success)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var brush = Brushes.Transparent;
                if (i == _targetIndex)
                {
                    brush = Brushes.LimeGreen;
                }
                else if (i == pickedIndex && !success)
                {
                    brush = Brushes.IndianRed;
                }

                _cards[i].BorderBrush = brush;
                _cards[i].BorderThickness = new Thickness(3);
            }
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hintUsed)
            {
                Feedback.Text = "Bạn đã dùng gợi ý cho lượt này.";
                return;
            }

            if (!EnsureCardAvailable(PowerCardType.Hint, 5, "Gợi ý"))
                return;

            _hintUsed = true;
            var highlight = new HashSet<int> { _targetIndex };
            while (highlight.Count < 2)
            {
                int idx = GameState.Rnd.Next(_group.Length);
                highlight.Add(idx);
            }

            foreach (var index in highlight)
            {
                _cards[index].Background = new SolidColorBrush(Color.FromArgb(40, 255, 215, 0));
            }

            Feedback.Text = "Một trong các thẻ được tô sáng là đáp án!";
            RefreshHud();
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.SwapProduct, 8, "Đổi nhóm"))
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
            Feedback.Text = "⭐ Thẻ nhân đôi đã sẵn sàng cho phần thưởng tiếp theo.";
            RefreshHud();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
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

        private void AnimateCard(Border card)
        {
            try
            {
                if (FindResource("CardRevealStoryboard") is Storyboard storyboard)
                {
                    var sb = storyboard.Clone();
                    foreach (var anim in sb.Children)
                    {
                        Storyboard.SetTarget(anim, card);
                    }
                    sb.Begin();
                }
            }
            catch { /* ignore animation errors */ }
        }
    }
}
