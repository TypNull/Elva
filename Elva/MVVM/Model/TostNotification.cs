using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Elva.MVVM.Model
{
    public enum ToastType
    {
        Success,
        Info,
        Warning,
        Error
    }

    public class ToastNotification
    {
        private static readonly TimeSpan DefaultDisplayTime = TimeSpan.FromSeconds(4);
        private static Panel? _container;
        private static Dictionary<string, DateTime> _recentNotifications = new();
        private static readonly TimeSpan DuplicateThreshold = TimeSpan.FromSeconds(5);
        private static readonly int MaxVisibleNotifications = 3;
        private static readonly List<Border> _activeToasts = new();

        public static void Initialize(Panel container)
        {
            _container = container;
        }

        public static void Show(string message, ToastType type = ToastType.Info, TimeSpan? displayTime = null)
        {
            if (_container == null)
            {
                MessageBox.Show(message, type.ToString());
                return;
            }

            // Debounce duplicate notifications
            string notificationKey = $"{message}_{type}";
            if (_recentNotifications.TryGetValue(notificationKey, out DateTime lastShown))
            {
                if (DateTime.Now - lastShown < DuplicateThreshold)
                {
                    return; // Skip duplicate notification
                }
            }
            _recentNotifications[notificationKey] = DateTime.Now;

            // Clean up old notifications from tracking
            DateTime now = DateTime.Now;
            List<string> keysToRemove = _recentNotifications.Where(kvp => now - kvp.Value > TimeSpan.FromMinutes(2))
                                                   .Select(kvp => kvp.Key)
                                                   .ToList();
            foreach (string? key in keysToRemove)
            {
                _recentNotifications.Remove(key);
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Limit number of visible notifications
                while (_activeToasts.Count >= MaxVisibleNotifications)
                {
                    if (_activeToasts.Count > 0)
                    {
                        Border oldestToast = _activeToasts[0];
                        FadeOutAndRemove(oldestToast, true);
                    }
                }

                Border toast = CreateToast(message, type);
                _container.Children.Add(toast);
                _activeToasts.Add(toast);

                Storyboard storyboard = new();

                // Slide in animation
                ThicknessAnimation slideInAnimation = new()
                {
                    From = new Thickness(0, -toast.ActualHeight - 20, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideInAnimation, toast);
                Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath("Margin"));
                storyboard.Children.Add(slideInAnimation);

                // Start the animation
                storyboard.Begin();

                // Schedule fade out
                DispatcherTimer timer = new()
                {
                    Interval = displayTime ?? DefaultDisplayTime
                };

                timer.Tick += (sender, e) =>
                {
                    timer.Stop();
                    FadeOutAndRemove(toast);
                };

                timer.Start();
            });
        }

        public static void ShowBatch(IEnumerable<string> messages, ToastType type = ToastType.Info)
        {
            List<string> messagesList = messages.ToList();
            if (messagesList.Count == 0) return;

            if (messagesList.Count == 1)
            {
                Show(messagesList[0], type);
                return;
            }

            Show($"{messagesList.Count} items: {messagesList[0]} and others", type);
        }

        private static Border CreateToast(string message, ToastType type)
        {
            SolidColorBrush background = GetBackgroundColor(type);
            string icon = GetIcon(type);

            Border toast = new()
            {
                Background = background,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 10), // Add margin between toasts
                Padding = new Thickness(15, 10, 15, 10),
                MaxWidth = 350,
                MinWidth = 200,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 3,
                    BlurRadius = 10,
                    Opacity = 0.3
                }
            };

            Grid grid = new();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon
            TextBlock iconTextBlock = new()
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconTextBlock, 0);
            grid.Children.Add(iconTextBlock);

            // Message
            TextBlock messageTextBlock = new()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(messageTextBlock, 1);
            grid.Children.Add(messageTextBlock);

            toast.Child = grid;
            return toast;
        }

        private static void FadeOutAndRemove(Border toast, bool immediate = false)
        {
            if (immediate)
            {
                _activeToasts.Remove(toast);
                if (_container != null && _container.Children.Contains(toast))
                {
                    _container.Children.Remove(toast);
                }
                return;
            }

            Storyboard storyboard = new();

            // Fade out animation
            DoubleAnimation fadeOutAnimation = new()
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOutAnimation, toast);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeOutAnimation);

            // After animation completes, remove the toast from the container
            storyboard.Completed += (sender, e) =>
            {
                _activeToasts.Remove(toast);
                if (_container != null && _container.Children.Contains(toast))
                {
                    _container.Children.Remove(toast);
                }
            };

            // Start the animation
            storyboard.Begin();
        }

        private static SolidColorBrush GetBackgroundColor(ToastType type)
        {
            return type switch
            {
                ToastType.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                ToastType.Info => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                ToastType.Warning => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                ToastType.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };
        }

        private static string GetIcon(ToastType type)
        {
            return type switch
            {
                ToastType.Success => "\uE930", // Checkmark
                ToastType.Info => "\uE946",    // Info
                ToastType.Warning => "\uE7BA", // Warning
                ToastType.Error => "\uE711",   // Error
                _ => "\uE946"                  // Default to Info
            };
        }
    }
}