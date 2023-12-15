using LottieSharp.WPF;
using LottieSharp.WPF.Transforms;
using SkiaSharp;
using SkiaSharp.Skottie;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Elva.MVVM.View.CElement
{
    public class LottieAnimationView : SKElement
    {
        private Animation? animation;

        public AnimationInfo Info
        {
            get { return (AnimationInfo)GetValue(InfoProperty); }
            set { SetValue(InfoProperty, value); }
        }

        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        public string ResourcePath
        {
            get => (string)GetValue(ResourcePathProperty);
            set => SetValue(ResourcePathProperty, value);
        }

        public AnimationTransformBase AnimationScale
        {
            get { return (AnimationTransformBase)GetValue(AnimationScaleProperty); }
            set { SetValue(AnimationScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnimationScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationScaleProperty =
            DependencyProperty.Register("AnimationScale", typeof(AnimationTransformBase), typeof(LottieAnimationView), new PropertyMetadata(default(AnimationTransformBase)));

        public static readonly DependencyProperty InfoProperty =
            DependencyProperty.Register("Info", typeof(AnimationInfo), typeof(LottieAnimationView));

        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(LottieAnimationView), new PropertyMetadata(false));

        public double AnimationTime
        {
            get { return (double)GetValue(AnimationTimeProperty); }
            set { SetValue(AnimationTimeProperty, value); }
        }

        public static readonly DependencyProperty AnimationTimeProperty =
            DependencyProperty.Register("AnimationTime", typeof(double), typeof(LottieAnimationView), new PropertyMetadata(0d));

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(LottieAnimationView), new PropertyMetadata(null, FileNamePropertyChangedCallback));

        public static readonly DependencyProperty ResourcePathProperty =
            DependencyProperty.Register("ResourcePath", typeof(string), typeof(LottieAnimationView), new PropertyMetadata(null, ResourcePathPropertyChangedCallback));

        private static void FileNamePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LottieAnimationView lottieAnimationView && e.NewValue is string assetName)
            {
                lottieAnimationView.SetAnimationFromFile(assetName);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == AnimationTimeProperty)
            {
                InvalidateVisual();
            }
        }

        private static void ResourcePathPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is LottieAnimationView lottieAnimationView && e.NewValue is string assetName)
            {
                lottieAnimationView.SetAnimationFromResource(assetName);
            }
        }

        private void SetAnimationFromFile(string assetName)
        {
            try
            {
                using FileStream stream = File.OpenRead(assetName);
                SetAnimation(stream);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Unexpected error when loading {assetName}");
                throw;
            }
        }

        private void SetAnimationFromResource(string assetUri)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            try
            {
                Uri resourceUri = new(assetUri);
                System.Windows.Resources.StreamResourceInfo resourceInfo = Application.GetResourceStream(resourceUri);

                SetAnimation(resourceInfo.Stream);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Unexpected error when loading resource {assetUri}");
                throw;
            }

        }

        private void SetAnimation(Stream stream)
        {
            using SKManagedStream fileStream = new(stream);

            if (Animation.TryCreate(fileStream, out animation))
            {
                animation.Seek(0);
                Info = new AnimationInfo(animation.Version, animation.Duration, animation.Fps, animation.InPoint,
                    animation.OutPoint);
            }
            else
            {
                Info = new AnimationInfo(string.Empty, TimeSpan.Zero, 0, 0, 0);
                throw new InvalidOperationException("Failed to load animation");
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(SKColor.Empty);
            SKImageInfo info = e.Info;

            if (animation != null)
            {
                animation.SeekFrameTime((float)AnimationTime);

                if (AnimationScale is CenterTransform)
                {
                    canvas.Scale(AnimationScale.ScaleX, AnimationScale.ScaleY, info.Width / 2, info.Height / 2);
                }
                else if (AnimationScale != null)
                {
                    canvas.Scale(AnimationScale.ScaleX, AnimationScale.ScaleY, AnimationScale.CenterX, AnimationScale.CenterY);
                }

                animation.Render(canvas, new SKRect(0, 0, info.Width, info.Height));
            }
        }
    }
}
