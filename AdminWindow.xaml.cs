using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HayChonGiaDung.Wpf
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new();
        private ObservableCollection<Product> _products = new();
        private Product? _selectedProduct;
        private readonly ProductDraft _editor = new();

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

        public ProductDraft Editor => _editor;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AdminWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadProducts();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _httpClient.Dispose();
        }

        private void LoadProducts()
        {
            Products = new ObservableCollection<Product>(GameState.Catalog.Select(Clone));
            SelectedProduct = Products.FirstOrDefault();
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

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyBox.Password?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show(this, "Vui lòng nhập API key Imgbb trước khi tải ảnh.", "Thiếu API key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                ApiKeyBox.IsEnabled = false;
                var url = await UploadImageAsync(dialog.FileName, apiKey);
                if (!string.IsNullOrEmpty(url))
                {
                    Editor.ImageUrl = url;
                    MessageBox.Show(this, "Đã tải ảnh lên Imgbb thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Không thể lấy được URL ảnh từ Imgbb.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi khi tải ảnh lên Imgbb: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UploadButton.IsEnabled = true;
                ApiKeyBox.IsEnabled = true;
            }
        }

        private async Task<string?> UploadImageAsync(string filePath, string apiKey)
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            var base64 = Convert.ToBase64String(bytes);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(base64), "image");
            content.Add(new StringContent(Path.GetFileName(filePath)), "name");

            var response = await _httpClient.PostAsync($"https://api.imgbb.com/1/upload?key={Uri.EscapeDataString(apiKey)}", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Imgbb trả về lỗi {(int)response.StatusCode}: {error}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("url", out var url))
            {
                return url.GetString();
            }

            return null;
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

        private void PersistChanges()
        {
            ProductRepository.SaveProducts(Products.Select(Clone));
            GameState.ReloadCatalog();
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
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    OnPropertyChanged(nameof(ImageUrl));
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
                Description = null;
                return;
            }

            Name = product.Name;
            PriceText = product.Price.ToString();
            ImageUrl = product.ImageUrl ?? product.Image;
            Description = product.Description;
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
            product.ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim();
            product.Image = null;
            product.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
