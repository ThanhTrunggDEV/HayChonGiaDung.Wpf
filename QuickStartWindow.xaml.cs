using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HayChonGiaDung.Wpf
{
    public partial class QuickStartWindow : Window
    {
        private readonly List<QuickStartQuestion> _questions;
        private int _index;
        private int _correct;
        private bool _awaitingNext;

        public QuickStartWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            _questions = BuildQuestions().OrderBy(_ => GameState.Rnd.Next()).Take(6).ToList();
            SoundManager.StartRound();
            UpdateCoins();
            LoadQuestion();
        }

        private static IEnumerable<QuickStartQuestion> BuildQuestions()
        {
            yield return new QuickStartQuestion("Thiết bị nào thường được dán nhãn năng lượng để người tiêu dùng nhận biết mức tiết kiệm điện?",
                new[] { "Máy điều hòa", "Máy in", "Bàn ủi hơi nước", "Nồi cơm điện" }, 0);
            yield return new QuickStartQuestion("Chất liệu nào giúp chảo chống dính bền hơn khi sử dụng với nhiệt cao?",
                new[] { "Hợp kim nhôm phủ gốm", "Inox 201", "Gang thô không xử lý", "Nhựa ABS" }, 0);
            yield return new QuickStartQuestion("Khi mua máy lọc không khí cho phòng ngủ 20m², thông số CADR tối thiểu nên khoảng bao nhiêu?",
                new[] { "150 m³/h", "60 m³/h", "320 m³/h", "45 m³/h" }, 0);
            yield return new QuickStartQuestion("Tiêu chuẩn an toàn nào thường thấy trên ổ cắm kéo dài chất lượng?",
                new[] { "Chứng nhận QCVN 4:2009/BKHCN", "Tem hợp quy đồ chơi", "Ký hiệu IP68", "Tem chống hàng giả" }, 0);
            yield return new QuickStartQuestion("Tủ lạnh Inverter mang lại lợi ích chính nào?",
                new[] { "Tiết kiệm điện và vận hành êm", "Giá rẻ hơn tủ thường", "Rã đông nhanh hơn", "Dung tích lớn hơn" }, 0);
            yield return new QuickStartQuestion("Khi chọn robot hút bụi cho nhà nhiều tầng, phụ kiện nào là quan trọng nhất?",
                new[] { "Cảm biến chống rơi", "Đèn UV", "Túi lọc nước", "Chổi cao su" }, 0);
            yield return new QuickStartQuestion("Dung tích bình chứa bụi của máy hút cầm tay nên tối thiểu bao nhiêu để tránh đổ liên tục?",
                new[] { "0.4 lít", "0.05 lít", "1.5 lít", "3 lít" }, 0);
            yield return new QuickStartQuestion("Trên máy lọc nước RO, lõi than hoạt tính có tác dụng gì?",
                new[] { "Khử mùi và cải thiện vị", "Diệt khuẩn bằng tia UV", "Bổ sung khoáng tức thì", "Ổn định áp lực nước" }, 0);
            yield return new QuickStartQuestion("Khi bảo quản nồi chảo chống dính, thao tác nào nên tránh?",
                new[] { "Dùng miếng chà kim loại", "Phơi khô tự nhiên", "Để nguội trước khi rửa", "Sử dụng muỗng silicon" }, 0);
            yield return new QuickStartQuestion("Tiêu chí nào KHÔNG thuộc chương trình Nhãn năng lượng Việt Nam?",
                new[] { "Mức phát thải CO2", "Hiệu suất năng lượng", "Chỉ số tiêu thụ điện", "Số sao hiệu suất" }, 0);
            yield return new QuickStartQuestion("Công nghệ nào giúp máy giặt giảm nhăn áo quần sau khi giặt?",
                new[] { "Giặt hơi nước", "Sấy không khí", "Sấy nóng liên tục", "Tạo bọt khí lạnh" }, 0);
        }

        private void LoadQuestion()
        {
            if (_index >= _questions.Count)
            {
                ShowShop();
                return;
            }

            _awaitingNext = false;
            var q = _questions[_index];
            ProgressText.Text = $"Câu {_index + 1}/{_questions.Count}";
            QuestionText.Text = q.Question;
            FeedbackText.Text = string.Empty;
            NextButton.Visibility = Visibility.Collapsed;

            OptionsPanel.Children.Clear();
            int i = 0;
            foreach (var option in q.Options)
            {
                var btn = new Button
                {
                    Content = option,
                    Tag = i,
                    Height = 54,
                    Margin = new Thickness(0, 6, 0, 6),
                    FontSize = 18,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += Option_Click;
                OptionsPanel.Children.Add(btn);
                i++;
            }

            SoundManager.Reveal();
            AnimatePanel(OptionsPanel);
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            if (_awaitingNext) return;

            _awaitingNext = true;
            var btn = (Button)sender;
            int guess = (int)btn.Tag;
            var q = _questions[_index];

            foreach (Button child in OptionsPanel.Children)
            {
                child.IsEnabled = false;
                child.Background = Brushes.Transparent;
            }

            if (guess == q.CorrectIndex)
            {
                _correct++;
                GameState.AddCoins(5);
                btn.Background = Brushes.LightGreen;
                FeedbackText.Text = "✅ Chính xác! +5 xu";
                SoundManager.Correct();
            }
            else
            {
                btn.Background = Brushes.LightCoral;
                var correctBtn = OptionsPanel.Children.Cast<Button>().First(b => (int)b.Tag == q.CorrectIndex);
                correctBtn.Background = Brushes.LightGreen;
                FeedbackText.Text = "❌ Chưa đúng. Hãy ghi nhớ kiến thức này!";
                SoundManager.Wrong();
            }

            UpdateCoins();
            NextButton.Visibility = Visibility.Visible;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _index++;
            LoadQuestion();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowShop()
        {
            ProgressText.Text = $"Hoàn thành {_correct}/{_questions.Count} câu";
            QuestionText.Text = "Bạn đã sẵn sàng bước vào chương trình chính!";
            OptionsPanel.Children.Clear();
            FeedbackText.Text = "Hãy dùng xu để mua thẻ chiến thuật phù hợp.";
            NextButton.Visibility = Visibility.Collapsed;
            UpdateCoins();
            UpdateShopButtons();
            ShopStatus.Text = $"Đã trả lời đúng {_correct} câu, tích lũy {GameState.Coins} xu.";
            ShopPanel.Visibility = Visibility.Visible;
        }

        private void UpdateShopButtons()
        {
            BuyHintButton.Content = $"Mua thẻ Gợi ý (-5 xu) — hiện có {GameState.GetCardCount(PowerCardType.Hint)}";
            BuySwapButton.Content = $"Mua thẻ Đổi sản phẩm (-8 xu) — hiện có {GameState.GetCardCount(PowerCardType.SwapProduct)}";
            BuyDoubleButton.Content = $"Mua thẻ Nhân đôi phần thưởng (-10 xu) — hiện có {GameState.GetCardCount(PowerCardType.DoubleReward)}";
        }

        private void BuyHint_Click(object sender, RoutedEventArgs e) => BuyCard(PowerCardType.Hint, 5);
        private void BuySwap_Click(object sender, RoutedEventArgs e) => BuyCard(PowerCardType.SwapProduct, 8);
        private void BuyDouble_Click(object sender, RoutedEventArgs e) => BuyCard(PowerCardType.DoubleReward, 10);

        private void BuyCard(PowerCardType type, int cost)
        {
            if (!GameState.TrySpendCoins(cost))
            {
                ShopStatus.Text = "⚠️ Không đủ xu để mua thẻ này.";
                SoundManager.Wrong();
                return;
            }

            GameState.AddPowerCard(type);
            ShopStatus.Text = "Đã mua thẻ thành công!";
            SoundManager.Correct();
            UpdateCoins();
            UpdateShopButtons();
        }

        private void FinishShop_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void UpdateCoins()
        {
            CoinText.Text = GameState.Coins.ToString();
        }

        private static void AnimatePanel(Panel panel)
        {
            var sb = new Storyboard();
            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(400)));
            Storyboard.SetTarget(fade, panel);
            Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fade);
            panel.Opacity = 0;
            sb.Begin();
        }
    }

    internal record QuickStartQuestion(string Question, IReadOnlyList<string> Options, int CorrectIndex);
}
