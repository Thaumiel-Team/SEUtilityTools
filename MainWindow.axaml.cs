using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using SEUtilityTools.API.Interface;
using SEUtilityTools.Pages;
using ic = Projektanker.Icons.Avalonia;
using Avalonia.Threading;

namespace SEUtilityTools
{
    public partial class MainWindow : Window
    {
        private Rectangle? _activeIndicator;
        private Page? _activePage;
        private bool _sidebarVisible = true;
        private bool _isAnimating = false;

        private readonly Dictionary<string, bool> _groupOpen = new()
        {
            ["GPS"] = false
        };

        private readonly HashSet<string> _groupAnimating = [];

        private static double GroupExpandedHeight(int itemCount) =>
            itemCount * 44.0 + Math.Max(0, itemCount - 1) * 4.0 + 8.0;

        public MainWindow()
        {
            InitializeComponent();
            NavigateTo(new Home(), HomeIndicator);
        }

        private async void HamburgerClick(object? sender, RoutedEventArgs e)
        {
            if (_isAnimating)
                return;

            _isAnimating = true;
            _sidebarVisible = !_sidebarVisible;

            double targetWidth = _sidebarVisible ? 240 : 0;

            Animation animation = new()
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseInOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(WidthProperty, sideBar.Width)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(WidthProperty, targetWidth)
                        }
                    }
                }
            };

            await animation.RunAsync(sideBar);
            sideBar.Width = targetWidth;
            _isAnimating = false;
        }

        private async void GPSGroupClick(object? sender, RoutedEventArgs e) =>
            await ToggleGroupAsync("GPS", GPSContent, GPSChevron, itemCount: 1);

        private async Task ToggleGroupAsync(string key, Border content, ic::Icon chevron, int itemCount)
        {
            if (_groupAnimating.Contains(key))
                return;

            _groupAnimating.Add(key);
            bool opening = !_groupOpen[key];
            _groupOpen[key] = opening;

            double expandedHeight = GroupExpandedHeight(itemCount);
            double fromHeight = opening ? 0.0 : expandedHeight;
            double toHeight = opening ? expandedHeight : 0.0;
            double fromAngle = opening ? 0.0 : 90.0;
            double toAngle = opening ? 90.0 : 0.0;

            Animation heightAnim = new()
            {
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseInOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(Layoutable.HeightProperty, fromHeight)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(Layoutable.HeightProperty, toHeight)
                        }
                    }
                }
            };

            RotateTransform rotateTransform = (RotateTransform)chevron.RenderTransform!;

            DateTime start = DateTime.UtcNow;
            TimeSpan duration = TimeSpan.FromMilliseconds(220);
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(8)
            };

            timer.Tick += (s, _) =>
            {
                double t = Math.Clamp((DateTime.UtcNow - start).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
                rotateTransform.Angle = fromAngle + (toAngle - fromAngle) * (1 - Math.Pow(1 - t, 3));
                if (t >= 1)
                {
                    rotateTransform.Angle = toAngle;
                    ((DispatcherTimer)s!).Stop();
                }
            };

            timer.Start();
            await heightAnim.RunAsync(content);
            content.Height = toHeight;
            _groupAnimating.Remove(key);
        }

        private void NavigateTo(Page page, Rectangle indicator)
        {
            _activePage?.OnClosed();
            _activeIndicator?.IsVisible = false;
            indicator.IsVisible = true;
            _activeIndicator = indicator;
            _activePage = page;
            _activePage.OnClicked();
            _activePage.FillContent(contentArea);
        }

        private void HomeButtonClick(object? sender, RoutedEventArgs e) =>
            NavigateTo(new Home(), HomeIndicator);

        private void SettingsButtonClick(object? sender, RoutedEventArgs e) =>
            NavigateTo(new Settings(), SettingsIndicator);

        private void BlueprintCalculatorButtonClick(object? sender, RoutedEventArgs e) =>
            NavigateTo(new BlueprintCalculator(), BlueprintIndicator);

        private void ServerQueryButtonClick(object? sender, RoutedEventArgs e) =>
            NavigateTo(new ServerQuery(), ServerIndicator);

        private void GPSTriangulatorButtonClick(object? sender, RoutedEventArgs e) =>
            NavigateTo(new GPSTriangulator(), GpsIndicator);
    }
}