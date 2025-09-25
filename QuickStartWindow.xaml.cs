using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HayChonGiaDung.Wpf
{
    public partial class QuickStartWindow : Window
    {
        private readonly List<QuickQuestion> _questions;
        private int _currentIndex = -1;
        private int? _selectedIndex = null;
        private readonly List<HelpStoreItem> _storeItems;
        private bool _inStorePhase = false;

        public QuickStartWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            SoundManager.StartRound();

            _questions = BuildQuestions().OrderBy(_ => GameState.Rnd.Next()).Take(6).ToList();
            _storeItems = BuildStore();

            StoreList.ItemsSource = _storeItems;

            NextQuestion();
            UpdateInventoryPanel();
        }

        private void NextQuestion()
        {
            _currentIndex++;
            _selectedIndex = null;
            Feedback.Text = string.Empty;
            QuestionHint.Text = string.Empty;

            if (_currentIndex >= _questions.Count)
            {
                EnterStorePhase();
                return;
            }

            var q = _questions[_currentIndex];
            ProgressText.Text = $"Câu hỏi {_currentIndex + 1}/{_questions.Count}";
            QuestionText.Text = q.Text;
            AnswerList.ItemsSource = q.Options
                .Select((text, idx) => new { Text = $"{(char)('A' + idx)}. {text}", Index = idx })
                .ToList();
            QuestionPanel.Visibility = Visibility.Visible;
            StorePanel.Visibility = Visibility.Collapsed;
            ActionButton.Content = "Trả lời";
            SkipButton.Visibility = Visibility.Visible;
        }

        private void EnterStorePhase()
        {
            _inStorePhase = true;
            ProgressText.Text = "Hoàn thành khởi động";
            QuestionPanel.Visibility = Visibility.Collapsed;
            StorePanel.Visibility = Visibility.Visible;
            ActionButton.Content = "Bắt đầu vòng 1";
            SkipButton.Visibility = Visibility.Collapsed;
            Feedback.Text = "Bạn có thể dùng xu để mua thẻ hỗ trợ.";
            UpdateInventoryPanel();
        }

        private static List<QuickQuestion> BuildQuestions() => new()
        {
            new QuickQuestion("Nồi chiên không dầu hoạt động dựa trên nguyên lý chính nào?",
                new[]
                {
                    "Dùng áp suất cao",
                    "Lưu thông khí nóng đối lưu",
                    "Chiên bằng hồng ngoại",
                    "Đun sôi dầu truyền thống"
                }, 1,
                "Máy dùng quạt thổi khí nóng tuần hoàn để làm chín thực phẩm."),
            new QuickQuestion("Chuẩn năng lượng 5 sao trên nhãn năng lượng Việt Nam thể hiện điều gì?",
                new[]
                {
                    "Sản phẩm xuất xứ châu Âu",
                    "Sản phẩm tiết kiệm điện nhất trong nhóm",
                    "Sản phẩm bảo hành 5 năm",
                    "Sản phẩm chống nước chuẩn IPX5"
                }, 1,
                "Nhãn càng nhiều sao càng tiết kiệm điện."),
            new QuickQuestion("Bột giặt và nước giặt khác nhau ở điểm nổi bật nào?",
                new[]
                {
                    "Bột giặt chỉ dùng cho máy cửa trên",
                    "Nước giặt dễ hòa tan, ít để lại cặn",
                    "Bột giặt thơm hơn nước giặt",
                    "Nước giặt không dùng được cho máy giặt"
                }, 1,
                "Nước giặt tan nhanh nên phù hợp giặt lạnh, ít để lại cặn."),
            new QuickQuestion("Khi sử dụng tủ lạnh mới mua về, nên làm gì trước khi cắm điện?",
                new[]
                {
                    "Cắm điện và cho thực phẩm ngay",
                    "Để yên tối thiểu 4-6 tiếng cho gas ổn định",
                    "Vệ sinh bằng nước nóng",
                    "Lật ngược tủ lạnh 1 phút"
                }, 1,
                "Để gas hồi vị trí ban đầu sau quá trình vận chuyển."),
            new QuickQuestion("Tiêu chí quan trọng khi chọn máy lọc không khí cho phòng ngủ là gì?",
                new[]
                {
                    "Công suất bơm nước",
                    "Độ ồn thấp, có chế độ ban đêm",
                    "Có nhiều đèn LED nhiều màu",
                    "Trọng lượng càng nặng càng tốt"
                }, 1,
                "Phòng ngủ cần yên tĩnh để không ảnh hưởng giấc ngủ."),
            new QuickQuestion("Chức năng Inverter trên điều hòa giúp ích điều gì?",
                new[]
                {
                    "Thổi gió xa hơn",
                    "Tiết kiệm điện, vận hành ổn định",
                    "Tự phát Wifi",
                    "Tăng nhiệt độ tối đa"
                }, 1,
                "Biến tần giúp máy hoạt động êm và tiết kiệm điện."),
            new QuickQuestion("Khi mua nồi cơm điện, thông số nào quyết định nấu được bao nhiêu gạo?",
                new[]
                {
                    "Điện áp",
                    "Dung tích nồi (lít)",
                    "Công suất",
                    "Số lớp chống dính"
                }, 1,
                "Dung tích càng lớn thì nấu được nhiều gạo."),
            new QuickQuestion("Chuẩn IP của máy rửa xe gia đình thể hiện điều gì?",
                new[]
                {
                    "Tốc độ vòng quay động cơ",
                    "Khả năng chống bụi nước của thiết bị",
                    "Nguồn gốc xuất xứ",
                    "Mức độ tiết kiệm điện"
                }, 1,
                "IP cao bảo vệ tốt khỏi bụi và nước."),
        };

        private static List<HelpStoreItem> BuildStore() => new()
        {
            new HelpStoreItem(HelpCardType.Hint, "Xem gợi ý", 2,
                "Nhận gợi ý sâu hơn về đáp án hoặc giới hạn giá."),
            new HelpStoreItem(HelpCardType.SwapProduct, "Đổi sản phẩm", 3,
                "Đổi sang một sản phẩm khác nếu câu hỏi quá khó."),
            new HelpStoreItem(HelpCardType.DoubleReward, "Nhân đôi phần thưởng", 4,
                "Dùng trước khi trả lời để nhân đôi tiền thưởng vòng đó."),
        };

        private void Answer_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb) return;
            _selectedIndex = (int)rb.Tag;
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inStorePhase)
            {
                DialogResult = true;
                Close();
                return;
            }

            if (_selectedIndex == null)
            {
                Feedback.Text = "Hãy chọn một đáp án.";
                return;
            }

            var question = _questions[_currentIndex];
            if (_selectedIndex.Value == question.CorrectIndex)
            {
                GameState.AddCoins(2);
                Feedback.Text = "✅ Chính xác! +2 xu";
                QuestionHint.Text = question.Explanation;
                SoundManager.Correct();
            }
            else
            {
                Feedback.Text = $"❌ Sai rồi! Đáp án đúng là {(char)('A' + question.CorrectIndex)}.";
                QuestionHint.Text = question.Explanation;
                SoundManager.Wrong();
            }

            UpdateInventoryPanel();
            ActionButton.Content = "Câu tiếp";
            SkipButton.Visibility = Visibility.Collapsed;
            ActionButton.Click -= ActionButton_Click;
            ActionButton.Click += NextQuestion_Click;
        }

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            ActionButton.Click -= NextQuestion_Click;
            ActionButton.Click += ActionButton_Click;
            NextQuestion();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedIndex = null;
            Feedback.Text = "Bạn đã bỏ qua câu này.";
            SoundManager.Wrong();
            ActionButton.Content = "Câu tiếp";
            SkipButton.Visibility = Visibility.Collapsed;
            ActionButton.Click -= ActionButton_Click;
            ActionButton.Click += NextQuestion_Click;
        }

        private void BuyCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not HelpCardType type) return;
            var item = _storeItems.First(s => s.Type == type);
            if (!GameState.SpendCoins(item.Cost))
            {
                Feedback.Text = "Không đủ xu để mua thẻ này.";
                return;
            }

            GameState.AddHelpCard(type);
            Feedback.Text = $"Đã mua thẻ {item.Name}.";
            SoundManager.Correct();
            UpdateInventoryPanel();
        }

        private void UpdateInventoryPanel()
        {
            CoinText.Text = GameState.QuickCoins.ToString();
            CardText.Text = string.Join(" \u2022 ", GameState.GetInventory()
                .Where(kv => kv.Value > 0)
                .Select(kv => $"{GetCardName(kv.Key)} x{kv.Value}"));
            if (string.IsNullOrWhiteSpace(CardText.Text))
            {
                CardText.Text = "Chưa có thẻ";
            }
        }

        private static string GetCardName(HelpCardType type) => type switch
        {
            HelpCardType.Hint => "Gợi ý",
            HelpCardType.SwapProduct => "Đổi sản phẩm",
            HelpCardType.DoubleReward => "Nhân đôi",
            _ => type.ToString()
        };
    }

    public record QuickQuestion(string Text, IReadOnlyList<string> Options, int CorrectIndex, string Explanation);

    public record HelpStoreItem(HelpCardType Type, string Name, int Cost, string Description)
    {
        public string CostText => $"{Cost} xu";
    }
}
