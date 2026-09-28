using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MiyagiStrap
{
    public static class PetalRain
    {
        private static readonly Geometry PetalShape = Geometry.Parse(
            "M0,0 C-40,-30 -55,-85 -22,-105 L0,-92 L22,-105 C55,-85 40,-30 0,0 Z");

        public static void Start(Canvas canvas, int count = 16)
        {
            var rnd = new Random();
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;

            for (int i = 0; i < count; i++)
            {
                double size = rnd.Next(10, 22);
                var rotate = new RotateTransform();

                var petal = new Path
                {
                    Data = PetalShape,
                    Stretch = Stretch.Fill,
                    Width = size * 0.8,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xC2, 0xDD)),
                    Opacity = 0.35 + rnd.NextDouble() * 0.35,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = rotate,
                    IsHitTestVisible = false
                };

                double startX = rnd.NextDouble() * w;
                Canvas.SetLeft(petal, startX);
                Canvas.SetTop(petal, -30);
                canvas.Children.Add(petal);

                double fallSeconds = 8 + rnd.NextDouble() * 8;
                var begin = TimeSpan.FromSeconds(rnd.NextDouble() * fallSeconds);

                var fall = new DoubleAnimation(-30, h + 30, TimeSpan.FromSeconds(fallSeconds))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = begin
                };

                var sway = new DoubleAnimation(startX - 40, startX + 40,
                    TimeSpan.FromSeconds(3 + rnd.NextDouble() * 3))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = begin,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                var spin = new DoubleAnimation(0, rnd.Next(2) == 0 ? 360 : -360,
                    TimeSpan.FromSeconds(4 + rnd.NextDouble() * 4))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = begin
                };

                petal.BeginAnimation(Canvas.TopProperty, fall);
                petal.BeginAnimation(Canvas.LeftProperty, sway);
                rotate.BeginAnimation(RotateTransform.AngleProperty, spin);
            }
        }
    }
}
