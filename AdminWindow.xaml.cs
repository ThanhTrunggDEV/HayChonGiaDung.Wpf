using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HayChonGiaDung.Wpf
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        private static readonly string ImagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");

        private ObservableCollection<Product> _products = new();
        private Product? _selectedProduct;
        private readonly ProductDraft _editor = new();

        private ObservableCollection<QuickQuestion> _quickQuestions = new();
        private QuickQuestion? _selectedQuickQuestion;
        private readonly QuickQuestionDraft _questionEditor = new();

        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged(nameof(Products));
            }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
                _editor.LoadFrom(value);
            }
        }

        public ObservableCollection<QuickQuestion> QuickQuestions
        {
            get => _quickQuestions;
            set
            {
                _quickQuestions = value;
                OnPropertyChanged(nameof(QuickQuestions));
            }
        }

        public QuickQuestion? SelectedQuickQuestion
        {
            get => _selectedQuickQuestion;
            set
            {
                _selectedQuickQuestion = value;
                OnPropertyChanged(nameof(SelectedQuickQuestion));
                _questionEditor.LoadFrom(value);
            }
        }

        public ProductDraft Editor => _editor;
        public QuickQuestionDraft QuestionEditor => _questionEditor;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AdminWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadProducts();
            LoadQuickQuestions();
        }

        private void LoadProducts()
        {
            Products = new ObservableCollection<Product>(GameState.Catalog.Select(Clone));
            SelectedProduct = Products.FirstOrDefault();
        }

        private void LoadQuickQuestions()
        {
            var loaded = QuickStartQuestionRepository.LoadQuestions();
            QuickQuestions = new ObservableCollection<QuickQuestion>(loaded.Select(CloneQuestion));
            SelectedQuickQuestion = QuickQuestions.FirstOrDefault();
        }

        private static Product Clone(Product product)
        {
            return new Product
            {
                Name = product.Name,
                Price = product.Price,
                Image = product.Image,
                ImageUrl = product.ImageUrl,
                Description = product.Description
            };
        }

        private static QuickQuestion CloneQuestion(QuickQuestion question)
        {
            return question.Clone();
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Ảnh (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|Tất cả|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                UploadButton.IsEnabled = false;
                var fileName = await SaveImageLocallyAsync(dialog.FileName);
                Editor.SetLocalImage(fileName);
                MessageBox.Show(this, "Đã lưu ảnh thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UploadButton.IsEnabled = true;
            }
        }

        private static async Task<string> SaveImageLocallyAsync(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Không tìm thấy file nguồn.", sourcePath);
            }

            Directory.CreateDirectory(ImagesDirectory);

            var extension = Path.GetExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".png";
            }

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var destination = Path.Combine(ImagesDirectory, fileName);

            await using var sourceStream = File.OpenRead(sourcePath);
            await using var destinationStream = File.Create(destination);
            await sourceStream.CopyToAsync(destinationStream);

            return fileName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Editor.TryBuildProduct(out var product, out var error))
            {
                MessageBox.Show(this, error, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Products.Any(p => string.Equals(p.Name, product.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var result = MessageBox.Show(this, "Tên sản phẩm đã tồn tại. Bạn có muốn tiếp tục thêm một mục trùng tên?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            Products.Add(product);
            PersistChanges();
            SelectedProduct = product;
            MessageBox.Show(this, "Đã thêm sản phẩm mới.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần cập nhật trong danh sách.", "Chưa chọn sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Editor.TryBuildProduct(out var updated, out var error))
            {
                MessageBox.Show(this, error, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var index = Products.IndexOf(SelectedProduct);
            if (index >= 0)
            {
                Products[index] = updated;
                PersistChanges();
                SelectedProduct = updated;
                MessageBox.Show(this, "Đã cập nhật sản phẩm.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần xóa.", "Chưa chọn sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(this, $"Bạn có chắc muốn xóa sản phẩm '{SelectedProduct.Name}'?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var toRemove = SelectedProduct;
            Products.Remove(toRemove);
            PersistChanges();
            SelectedProduct = Products.FirstOrDefault();
            if (SelectedProduct == null)
            {
                Editor.LoadFrom(null);
            }

            MessageBox.Show(this, "Đã xóa sản phẩm.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedProduct = null;
            Editor.LoadFrom(null);
        }

        private void AddQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!QuestionEditor.TryBuildQuestion(out var question, out var error))
            {
                MessageBox.Show(this, error, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (QuickQuestions.Any(q => string.Equals(q.Text, question.Text, StringComparison.OrdinalIgnoreCase)))
            {
                var confirm = MessageBox.Show(this,
                    "Nội dung câu hỏi đã tồn tại. Bạn có muốn tiếp tục thêm câu hỏi trùng nội dung?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            QuickQuestions.Add(question);
            PersistQuestionChanges();
            SelectedQuickQuestion = question;
            MessageBox.Show(this, "Đã thêm câu hỏi mới.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedQuickQuestion == null)
            {
                MessageBox.Show(this, "Vui lòng chọn câu hỏi cần cập nhật.", "Chưa chọn câu hỏi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!QuestionEditor.TryBuildQuestion(out var updated, out var error))
            {
                MessageBox.Show(this, error, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var index = QuickQuestions.IndexOf(SelectedQuickQuestion);
            if (index >= 0)
            {
                QuickQuestions[index] = updated;
                PersistQuestionChanges();
                SelectedQuickQuestion = updated;
                MessageBox.Show(this, "Đã cập nhật câu hỏi.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedQuickQuestion == null)
            {
                MessageBox.Show(this, "Vui lòng chọn câu hỏi cần xóa.", "Chưa chọn câu hỏi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(this,
                "Bạn có chắc muốn xóa câu hỏi này?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var toRemove = SelectedQuickQuestion;
            QuickQuestions.Remove(toRemove);
            PersistQuestionChanges();
            SelectedQuickQuestion = QuickQuestions.FirstOrDefault();
            if (SelectedQuickQuestion == null)
            {
                QuestionEditor.LoadFrom(null);
            }

            MessageBox.Show(this, "Đã xóa câu hỏi.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedQuickQuestion = null;
            QuestionEditor.LoadFrom(null);
        }

        private void PersistChanges()
        {
            ProductRepository.SaveProducts(Products.Select(Clone));
            GameState.ReloadCatalog();
        }

        private void PersistQuestionChanges()
        {
            QuickStartQuestionRepository.SaveQuestions(QuickQuestions.Select(CloneQuestion));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProductDraft : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _priceText = string.Empty;
        private string? _imageUrl;
        private string? _imageFileName;
        private ImageSource? _imagePreview;
        private string? _description;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string PriceText
        {
            get => _priceText;
            set
            {
                if (_priceText != value)
                {
                    _priceText = value;
                    OnPropertyChanged(nameof(PriceText));
                }
            }
        }

        public string? ImageUrl
        {
            get => _imageUrl;
            private set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    OnPropertyChanged(nameof(ImageUrl));
                    UpdatePreview();
                }
            }
        }

        public string? ImageFileName
        {
            get => _imageFileName;
            private set
            {
                if (_imageFileName != value)
                {
                    _imageFileName = value;
                    OnPropertyChanged(nameof(ImageFileName));
                    UpdatePreview();
                }
            }
        }

        public ImageSource? ImagePreview
        {
            get => _imagePreview;
            private set
            {
                if (!Equals(_imagePreview, value))
                {
                    _imagePreview = value;
                    OnPropertyChanged(nameof(ImagePreview));
                }
            }
        }

        public string? Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadFrom(Product? product)
        {
            if (product == null)
            {
                Name = string.Empty;
                PriceText = string.Empty;
                ImageUrl = null;
                ImageFileName = null;
                Description = null;
                return;
            }

            Name = product.Name;
            PriceText = product.Price.ToString();
            ImageFileName = string.IsNullOrWhiteSpace(product.Image) ? null : product.Image;
            ImageUrl = string.IsNullOrWhiteSpace(product.Image) ? product.ImageUrl : null;
            Description = product.Description;
            UpdatePreview();
        }

        public bool TryBuildProduct(out Product product, out string error)
        {
            product = new Product();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "Tên sản phẩm không được để trống.";
                return false;
            }

            if (!int.TryParse(PriceText, out var price) || price < 0)
            {
                error = "Giá sản phẩm phải là số nguyên không âm.";
                return false;
            }

            product.Name = Name.Trim();
            product.Price = price;

            if (!string.IsNullOrWhiteSpace(ImageFileName))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", ImageFileName);
                if (!File.Exists(path))
                {
                    error = "Không tìm thấy ảnh trong thư mục Assets/Images. Vui lòng chọn lại.";
                    return false;
                }

                product.Image = ImageFileName;
                product.ImageUrl = null;
            }
            else
            {
                product.Image = null;
                product.ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim();
            }

            product.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            return true;
        }

        public void SetLocalImage(string? fileName)
        {
            ImageFileName = fileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                ImageUrl = null;
            }
        }

        private void UpdatePreview()
        {
            ImagePreview = CreateImageSource(ImageFileName, ImageUrl);
        }

        private static ImageSource? CreateImageSource(string? fileName, string? imageUrl)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", fileName);
                if (File.Exists(path))
                {
                    return LoadBitmap(new Uri(path));
                }
            }

            if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                try
                {
                    return LoadBitmap(new Uri(imageUrl));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static ImageSource? LoadBitmap(Uri uri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class QuickQuestionDraft : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private string _optionA = string.Empty;
        private string _optionB = string.Empty;
        private string _optionC = string.Empty;
        private string _optionD = string.Empty;
        private int _correctIndex;
        private string _explanation = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        public string OptionA
        {
            get => _optionA;
            set
            {
                if (_optionA != value)
                {
                    _optionA = value;
                    OnPropertyChanged(nameof(OptionA));
                }
            }
        }

        public string OptionB
        {
            get => _optionB;
            set
            {
                if (_optionB != value)
                {
                    _optionB = value;
                    OnPropertyChanged(nameof(OptionB));
                }
            }
        }

        public string OptionC
        {
            get => _optionC;
            set
            {
                if (_optionC != value)
                {
                    _optionC = value;
                    OnPropertyChanged(nameof(OptionC));
                }
            }
        }

        public string OptionD
        {
            get => _optionD;
            set
            {
                if (_optionD != value)
                {
                    _optionD = value;
                    OnPropertyChanged(nameof(OptionD));
                }
            }
        }

        public int CorrectIndex
        {
            get => _correctIndex;
            set
            {
                var clamped = Math.Clamp(value, 0, 3);
                if (_correctIndex != clamped)
                {
                    _correctIndex = clamped;
                    OnPropertyChanged(nameof(CorrectIndex));
                }
            }
        }

        public string Explanation
        {
            get => _explanation;
            set
            {
                if (_explanation != value)
                {
                    _explanation = value;
                    OnPropertyChanged(nameof(Explanation));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadFrom(QuickQuestion? question)
        {
            if (question == null)
            {
                Text = string.Empty;
                OptionA = string.Empty;
                OptionB = string.Empty;
                OptionC = string.Empty;
                OptionD = string.Empty;
                CorrectIndex = 0;
                Explanation = string.Empty;
                return;
            }

            Text = question.Text;
            OptionA = question.Options.Count > 0 ? question.Options[0] : string.Empty;
            OptionB = question.Options.Count > 1 ? question.Options[1] : string.Empty;
            OptionC = question.Options.Count > 2 ? question.Options[2] : string.Empty;
            OptionD = question.Options.Count > 3 ? question.Options[3] : string.Empty;
            CorrectIndex = Math.Clamp(question.CorrectIndex, 0, 3);
            Explanation = question.Explanation;
        }

        public bool TryBuildQuestion(out QuickQuestion question, out string error)
        {
            question = new QuickQuestion();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(Text))
            {
                error = "Nội dung câu hỏi không được để trống.";
                return false;
            }

            var options = new List<string> { OptionA, OptionB, OptionC, OptionD }
                .Select(o => (o ?? string.Empty).Trim())
                .ToList();

            if (options.Any(string.IsNullOrWhiteSpace))
            {
                error = "Các đáp án không được để trống.";
                return false;
            }

            if (CorrectIndex < 0 || CorrectIndex >= options.Count)
            {
                error = "Vị trí đáp án đúng không hợp lệ.";
                return false;
            }

            question.Text = Text.Trim();
            question.Options = options;
            question.CorrectIndex = CorrectIndex;
            question.Explanation = string.IsNullOrWhiteSpace(Explanation) ? string.Empty : Explanation.Trim();
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
