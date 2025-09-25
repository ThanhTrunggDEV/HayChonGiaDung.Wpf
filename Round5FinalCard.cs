using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace HayChonGiaDung.Wpf
{
    public enum Round5EvaluationState
    {
        Pending,
        Win,
        Protected,
        Lose
    }

    public class Round5FinalCard : INotifyPropertyChanged
    {
        public Round5FinalCard(Product product, ImageSource? image)
        {
            Product = product;
            Image = image;
        }

        public Product Product { get; }
        public ImageSource? Image { get; }

        private Round5EvaluationState _evaluationState = Round5EvaluationState.Pending;
        public Round5EvaluationState EvaluationState
        {
            get => _evaluationState;
            set
            {
                if (_evaluationState != value)
                {
                    _evaluationState = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isProtected;
        public bool IsProtected
        {
            get => _isProtected;
            set
            {
                if (_isProtected != value)
                {
                    _isProtected = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _displayOrder;
        public int DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayOrderText));
                }
            }
        }

        public string DisplayOrderText => $"Vị trí #{DisplayOrder}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
