using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HayChonGiaDung.Wpf
{
    public partial class Round3Window : Window
    {
        private readonly List<TextBox> _boxes;
        private readonly HashSet<int> _lockedPositions = new();
        private DispatcherTimer? _timer;
        private int _timeLeft = 60;
        private Product _current = null!;
        private string _targetDigits = string.Empty;
        private bool _roundActive;
        private bool _isClosing;

        public Round3Window()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            SoundManager.StartRound();

            _boxes = new List<TextBox> { D1, D2, D3, D4 };
            foreach (var box in _boxes)
            {
                box.PreviewTextInput += OnlyDigits;
                box.TextChanged += AutoAdvance;
                box.PreviewKeyDown += HandleBackspace;
            }

            BeginRound();
        }

        private void BeginRound()
        {
            _roundActive = true;
            Feedback.Text = string.Empty;
            Hint.Text = string.Empty;
            _lockedPositions.Clear();
            foreach (var box in _boxes)
            {
                box.IsEnabled = true;
                box.Text = string.Empty;
            }

            PickProduct();
            RestartTimer();
            RefreshHud();
            D1.Focus();
        }

        private void PickProduct()
        {
            _current = GameState.Catalog.Count > 0
                ? GameState.Catalog[GameState.Rnd.Next(GameState.Catalog.Count)]
                : new Product { Name = "Sản phẩm demo", Price = 2_890_000, Image = null };

            var price = Math.Max(1_000, _current.Price);
            _targetDigits = (price / 1_000).ToString("0000");

            ProductName.Text = $"Đoán 4 chữ số giá (x.000 ₫): {_current.Name}";
            ProductDesc.Text = string.IsNullOrWhiteSpace(_current.Description)
                ? "Chưa có mô tả cho sản phẩm này."
                : _current.Description;

            ProductImage.Source = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(_current.ImageUrl) && Uri.IsWellFormedUriString(_current.ImageUrl, UriKind.Absolute))
                {
                    ProductImage.Source = new BitmapImage(new Uri(_current.ImageUrl));
                }
                else if (!string.IsNullOrWhiteSpace(_current.Image))
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", _current.Image);
                    if (File.Exists(path))
                    {
                        ProductImage.Source = new BitmapImage(new Uri(path));
                    }
                }
            }
            catch
            {
                ProductImage.Source = null;
            }
        }

        private void RestartTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            _timeLeft = 60;
            TimerText.Text = _timeLeft.ToString();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_roundActive) return;

            _timeLeft--;
            TimerText.Text = _timeLeft.ToString();

            if (_timeLeft <= 0)
            {
                _timer?.Stop();
                _roundActive = false;
                SoundManager.Wrong();
                MessageBox.Show("⏰ Hết giờ! Bạn đã thua vòng Đếm Ngược 4 Số", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Information);
                FinishRound(false);
            }
        }

        private void OnlyDigits(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void AutoAdvance(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box && box.Text.Length == 1)
            {
                int index = _boxes.IndexOf(box);
                if (index < _boxes.Count - 1)
                {
                    _boxes[index + 1].Focus();
                    _boxes[index + 1].SelectAll();
                }
            }
        }

        private void HandleBackspace(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back) return;
            if (sender is not TextBox box) return;
            int index = _boxes.IndexOf(box);
            if (index <= 0 || box.SelectionStart != 0 || box.SelectionLength != 0) return;

            _boxes[index - 1].Focus();
            _boxes[index - 1].SelectAll();
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            if (!_roundActive) return;

            string guess = string.Concat(_boxes.Select(b => string.IsNullOrEmpty(b.Text) ? "_" : b.Text));
            if (guess.Contains('_'))
            {
                Feedback.Text = "⚠️ Nhập đủ 4 chữ số (0-9).";
                return;
            }

            if (!int.TryParse(guess, out var guessValue))
            {
                Feedback.Text = "⚠️ Chỉ được nhập chữ số.";
                return;
            }

            int target = int.Parse(_targetDigits);
            if (guessValue == target)
            {
                _roundActive = false;
                _timer?.Stop();
                GameState.AddPrize(1_000_000);
                Feedback.Text = $"✅ Chính xác! Giá: {target:N0}.000 ₫";
                SoundManager.Correct();
                RefreshHud();
                await Task.Delay(1000);
                if (_isClosing)
                {
                    return;
                }

                FinishRound(true);
                return;
            }

            SoundManager.Wrong();
            var hint = BuildHintString(guess);
            Hint.Text = $"Gợi ý: {hint}";
            Feedback.Text = "Sai rồi, thử lại nhé!";
            RefreshHud();
            D1.Focus();
            D1.SelectAll();
        }

        private string BuildHintString(string guess)
        {
            char[] hints = new char[4];
            for (int i = 0; i < 4; i++)
            {
                int guessDigit = guess[i] - '0';
                int targetDigit = _targetDigits[i] - '0';
                hints[i] = guessDigit == targetDigit ? '=' : guessDigit < targetDigit ? '<' : '>';
            }
            return $"[{string.Join(' ', hints)}]";
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_roundActive) return;

            if (!EnsureCardAvailable(PowerCardType.Hint, 5, "Gợi ý"))
                return;

            var available = Enumerable.Range(0, 4).Where(i => !_lockedPositions.Contains(i)).ToList();
            if (available.Count == 0)
            {
                Feedback.Text = "Đã tiết lộ toàn bộ chữ số.";
                return;
            }

            int revealIndex = available[GameState.Rnd.Next(available.Count)];
            _lockedPositions.Add(revealIndex);

            var targetDigit = _targetDigits[revealIndex].ToString();
            var box = _boxes[revealIndex];
            box.Text = targetDigit;
            box.IsEnabled = false;
            Feedback.Text = $"Đã mở khóa chữ số thứ {revealIndex + 1}: {targetDigit}.";
            RefreshHud();
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_roundActive) return;

            if (!EnsureCardAvailable(PowerCardType.SwapProduct, 8, "Đổi sản phẩm"))
                return;

            BeginRound();
            Feedback.Text = "Đã đổi sang sản phẩm mới.";
            RefreshHud();
        }

        private void DoubleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureCardAvailable(PowerCardType.DoubleReward, 10, "Nhân đôi"))
                return;

            GameState.QueueDoubleReward();
            Feedback.Text = "⭐ Phần thưởng vòng này sẽ được nhân đôi nếu thắng!";
            RefreshHud();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _roundActive = false;
            _timer?.Stop();
            FinishRound(false);
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

        protected override void OnClosing(CancelEventArgs e)
        {
            _isClosing = true;
            _roundActive = false;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        private void FinishRound(bool success)
        {
            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            _roundActive = false;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            if (!IsLoaded)
            {
                return;
            }

            try
            {
                DialogResult = success;
            }
            catch (InvalidOperationException)
            {
                // Window was not shown as dialog or is already closing – ignore.
            }

            if (IsVisible)
            {
                try
                {
                    Close();
                }
                catch (InvalidOperationException)
                {
                    // Ignore if window is already closing.
                }
            }
        }
    }
}
