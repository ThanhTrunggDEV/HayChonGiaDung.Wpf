using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HayChonGiaDung.Wpf
{
    public partial class FireworksWindow : Window
    {
        private readonly Random _random = new();
        private readonly TaskCompletionSource<bool> _completion = new();

        public Task Completion => _completion.Task;

        public FireworksWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            if (owner != null)
            {
                Left = owner.Left;
                Top = owner.Top;
                Width = owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width;
                Height = owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SoundManager.Fireworks();

            if (Owner != null)
            {
                Left = Owner.Left;
                Top = Owner.Top;
                Width = Owner.ActualWidth > 0 ? Owner.ActualWidth : Owner.Width;
                Height = Owner.ActualHeight > 0 ? Owner.ActualHeight : Owner.Height;
            }

            for (int i = 0; i < 6; i++)
            {
                SpawnBurst();
                await Task.Delay(180);
            }

            await Task.Delay(1200);
            Close();
        }

        private void SpawnBurst()
        {
            double width = ActualWidth > 0 ? ActualWidth : Width;
            double height = ActualHeight > 0 ? ActualHeight : Height;
            double left = _random.NextDouble() * Math.Max(200, width - 200) + 50;
            double top = _random.NextDouble() * Math.Max(200, height - 200) + 50;

            var outer = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = CreateGradientBrush(),
                Stroke = Brushes.White,
                StrokeThickness = 1.2,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.1, 0.1)
            };

            Canvas.SetLeft(outer, left);
            Canvas.SetTop(outer, top);
            FireworksCanvas.Children.Add(outer);

            var scaleAnimation = new DoubleAnimationUsingKeyFrames();
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.1, TimeSpan.Zero));
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1.8, TimeSpan.FromMilliseconds(350)));
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.7, TimeSpan.FromMilliseconds(900)));

            var opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, TimeSpan.Zero));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(120)));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(900)));

            if (outer.RenderTransform is ScaleTransform scale)
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            outer.BeginAnimation(OpacityProperty, opacityAnimation);

            CreateSparks(left + outer.Width / 2, top + outer.Height / 2);
        }

        private void CreateSparks(double centerX, double centerY)
        {
            int sparkCount = _random.Next(6, 11);
            double radius = _random.Next(40, 90);

            for (int i = 0; i < sparkCount; i++)
            {
                double angle = 2 * Math.PI * i / sparkCount;
                double targetX = centerX + radius * Math.Cos(angle);
                double targetY = centerY + radius * Math.Sin(angle);

                var spark = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(RandomColor()),
                    Opacity = 0,
                    RenderTransform = new TranslateTransform(0, 0)
                };

                Canvas.SetLeft(spark, centerX);
                Canvas.SetTop(spark, centerY);
                FireworksCanvas.Children.Add(spark);

                var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, TimeSpan.Zero));
                opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(150)));
                opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(900)));
                spark.BeginAnimation(OpacityProperty, opacityAnimation);

                if (spark.RenderTransform is TranslateTransform translate)
                {
                    var animX = new DoubleAnimationUsingKeyFrames();
                    animX.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
                    animX.KeyFrames.Add(new LinearDoubleKeyFrame(targetX - centerX, TimeSpan.FromMilliseconds(550)));
                    animX.KeyFrames.Add(new LinearDoubleKeyFrame(targetX - centerX, TimeSpan.FromMilliseconds(900)));

                    var animY = new DoubleAnimationUsingKeyFrames();
                    animY.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
                    animY.KeyFrames.Add(new LinearDoubleKeyFrame(targetY - centerY, TimeSpan.FromMilliseconds(550)));
                    animY.KeyFrames.Add(new LinearDoubleKeyFrame(targetY - centerY + 30, TimeSpan.FromMilliseconds(1000)));

                    translate.BeginAnimation(TranslateTransform.XProperty, animX);
                    translate.BeginAnimation(TranslateTransform.YProperty, animY);
                }
            }
        }

        private Brush CreateGradientBrush()
        {
            var gradient = new RadialGradientBrush(RandomColor(), RandomColor())
            {
                RadiusX = 0.8,
                RadiusY = 0.8,
                GradientOrigin = new Point(0.5, 0.5)
            };
            gradient.GradientStops.Insert(0, new GradientStop(Colors.White, 0));
            return gradient;
        }

        private Color RandomColor()
        {
            byte r = (byte)_random.Next(100, 256);
            byte g = (byte)_random.Next(100, 256);
            byte b = (byte)_random.Next(100, 256);
            return Color.FromRgb(r, g, b);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _completion.TrySetResult(true);
        }
    }
}
