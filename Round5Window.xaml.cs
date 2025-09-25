using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class Round5Window : Window
    {
        private ObservableCollection<Round5FinalCard> _cards = new();
        private bool _doubleActive = false;
        private Point? _dragStartPoint;
        private ListBoxItem? _draggedContainer;

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

            _cards = new ObservableCollection<Round5FinalCard>(
                picks.Select(p => new Round5FinalCard(p, TryLoadImage(p))));
            ProductsList.ItemsSource = _cards;
            RefreshDisplayOrders();
            foreach (var card in _cards)
            {
                card.Status = string.Empty;
                card.IsProtected = false;
                card.EvaluationState = Round5EvaluationState.Pending;
            }
            Feedback.Text = string.Empty;
        }

        private void RefreshDisplayOrders()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].DisplayOrder = i + 1;
            }
        }

        private static ImageSource? TryLoadImage(Product p)
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
                    {
                        return new BitmapImage(new Uri(path));
                    }
                }
            }
            catch { }
            return null;
        }

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            if (!GameState.UseHelpCard(HelpCardType.Hint))
            {
                MessageBox.Show("Bạn không đủ thẻ gợi ý.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_cards.Count == 0)
            {
                Feedback.Text = string.Empty;
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
                MessageBox.Show("Bạn không đủ thẻ đổi sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Feedback.Text = "🔄 Đã đổi danh sách sản phẩm.";
            BuildCards();
            RefreshHud();
        }

        private void Double_Click(object sender, RoutedEventArgs e)
        {
            if (_doubleActive)
            {
                MessageBox.Show("Bạn đã kích hoạt nhân đôi thưởng rồi!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!GameState.UseHelpCard(HelpCardType.DoubleReward))
            {
                MessageBox.Show("Bạn không đủ thẻ nhân đôi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _doubleActive = true;
            Feedback.Text = "✨ Toàn bộ tiền thưởng vòng này sẽ được nhân đôi nếu xếp đúng.";
            RefreshHud();
        }

        private void ProductsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _draggedContainer = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        }

        private void ProductsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedContainer == null || _dragStartPoint == null)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _draggedContainer = null;
                _dragStartPoint = null;
                return;
            }

            Point position = e.GetPosition(null);
            if (Math.Abs(position.X - _dragStartPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(position.Y - _dragStartPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (_draggedContainer.DataContext is Round5FinalCard card)
            {
                DragDrop.DoDragDrop(_draggedContainer, card, DragDropEffects.Move);
            }
            _draggedContainer = null;
            _dragStartPoint = null;
        }

        private void ProductsList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Round5FinalCard)) is not Round5FinalCard card)
            {
                return;
            }

            int sourceIndex = _cards.IndexOf(card);
            if (sourceIndex < 0)
            {
                return;
            }

            var targetContainer = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            int targetIndex = targetContainer != null
                ? ProductsList.ItemContainerGenerator.IndexFromContainer(targetContainer)
                : _cards.Count - 1;

            if (targetIndex < 0)
            {
                targetIndex = _cards.Count - 1;
            }

            if (targetIndex == sourceIndex)
            {
                return;
            }

            _cards.Move(sourceIndex, targetIndex);
            RefreshDisplayOrders();
            _draggedContainer = null;
            _dragStartPoint = null;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void Protect_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is Round5FinalCard selected)
            {
                foreach (var card in _cards)
                {
                    if (!ReferenceEquals(card, selected) && card.IsProtected)
                    {
                        card.IsProtected = false;
                    }
                }
                selected.IsProtected = true;
            }
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (_cards.Count == 0)
            {
                Feedback.Text = "Không có sản phẩm để xếp hạng.";
                return;
            }

            var sorted = _cards.OrderBy(c => c.Product.Price).ToList();
            int reward = 0;

            foreach (var card in _cards)
            {
                int actualIndex = sorted.IndexOf(card) + 1;
                if (card.DisplayOrder == actualIndex)
                {
                    int add = BaseReward;
                    if (_doubleActive) add *= 2;
                    reward += add;
                    card.Status = $"✅ Đúng vị trí! +{add:N0} ₫";
                    card.EvaluationState = Round5EvaluationState.Win;
                }
                else if (card.IsProtected)
                {
                    int add = ProtectedConsolation;
                    reward += add;
                    card.Status = $"🛡️ Bảo toàn: +{add:N0} ₫ (vị trí đúng là {actualIndex})";
                    card.EvaluationState = Round5EvaluationState.Protected;
                }
                else
                {
                    card.Status = $"❌ Sai. Vị trí đúng: {actualIndex}";
                    card.EvaluationState = Round5EvaluationState.Lose;
                }
            }

            GameState.TotalPrize += reward;
            PrizeText.Text = $"{GameState.TotalPrize:N0} ₫";
            Feedback.Text = reward > 0
                ? $"Bạn nhận thêm {reward:N0} ₫ ở vòng cuối!"
                : "Bạn chưa nhận thêm tiền thưởng ở vòng này.";
            PlayFeedbackAnimation(reward > 0);
            if (reward > 0)
            {
                SoundManager.Correct();
            }
            else
            {
                SoundManager.Wrong();
            }
            LeaderboardService.AddScore(GameState.PlayerName, GameState.TotalPrize);
            if (reward > 0)
            {
                await RoundCelebrationHelper.ShowWinAsync(this,
                    $"Bạn đã hoàn thành vòng 5 và nhận thêm {reward:N0} ₫!");
            }
            else
            {
                RoundCelebrationHelper.ShowLose(this,
                    "Bạn chưa nhận thêm tiền thưởng ở vòng này.");
            }
            this.DialogResult = reward > 0;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void PlayFeedbackAnimation(bool isWin)
        {
            if (Feedback.RenderTransform is not ScaleTransform scale)
            {
                scale = new ScaleTransform(1, 1);
                Feedback.RenderTransform = scale;
            }

            double target = isWin ? 1.15 : 0.9;
            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = target,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            SolidColorBrush? brush = Feedback.Background as SolidColorBrush;
            if (brush == null || brush.IsFrozen)
            {
                brush = new SolidColorBrush(Colors.Transparent);
                Feedback.Background = brush;
            }

            var colorAnimation = new ColorAnimation
            {
                To = isWin ? Color.FromRgb(76, 175, 80) : Color.FromRgb(239, 83, 80),
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

    }
}
