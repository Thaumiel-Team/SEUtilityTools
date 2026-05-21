using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Interface;
using SEUtilityTools.API.NET;
using SEUtilityTools.Pages;
using ic = Projektanker.Icons.Avalonia;

namespace SEUtilityTools
{
    public partial class MainWindow : Window
    {
        private Rectangle? _activeIndicator;
        private Page? _activePage;
        private bool _sidebarVisible = true;
        private bool _isAnimating = false;
        private bool _isUpdating = false;

        private readonly Dictionary<string, bool> _groupOpen = new()
        {
            ["GPS"] = false
        };

        private readonly HashSet<string> _groupAnimating = [];

        private static double GroupExpandedHeight(int itemCount)
            => itemCount * 44.0 + Math.Max(0, itemCount - 1) * 4.0 + 8.0;

        public MainWindow()
        {
            InitializeComponent();
            NavigateTo(new Home(), HomeIndicator);
            Opened += async (_, _) => await RefreshUpdateButtonAsync();
        }

        private async Task RefreshUpdateButtonAsync()
        {
            try
            {
                if (VersionManager.Versions.Count == 0)
                    await VersionManager.Init();

                updateBtn.IsVisible = VersionManager.IsUpdateAvailable();
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Could not check for updates: {ex.Message}");
            }
        }

        private async void UpdateButtonClick(object? sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;

            VersionInfo? latest = VersionManager.GetLatestVersion();
            if (latest?.Version == null)
                return;

            ButtonResult confirm = await MessageBoxManager.GetMessageBoxStandard("Update Available", $"Version {latest.Version} is available.\n\nThe application will restart automatically after the update.\n\nProceed?", ButtonEnum.YesNo).ShowAsync();
            if (confirm != ButtonResult.Yes)
                return;

            _isUpdating = true;
            updateBtn.IsEnabled = false;

            try
            {
                Progress<double> progress = new(p => LogManager.Info($"Update progress: {p:P0}"));
                await Updater.ApplyUpdateAsync(latest, progress);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Update failed: {ex}");

                await MessageBoxManager.GetMessageBoxStandard("Update Failed", $"Could not apply update:\n\n{ex.Message}", ButtonEnum.Ok).ShowAsync();
                _isUpdating = false;
                updateBtn.IsEnabled = true;
            }
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
            _isAnimating= false;
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
                            new Setter(HeightProperty, fromHeight)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(HeightProperty, toHeight)
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

        private void HomeButtonClick(object? sender, RoutedEventArgs e) => NavigateTo(new Home(), HomeIndicator);
        private void SettingsButtonClick(object? sender, RoutedEventArgs e) => NavigateTo(new Settings(), SettingsIndicator);
        private void BlueprintCalculatorButtonClick(object? sender, RoutedEventArgs e) => NavigateTo(new BlueprintCalculator(), BlueprintIndicator);
        private void ServerQueryButtonClick(object? sender, RoutedEventArgs e) => NavigateTo(new ServerQuery(), ServerIndicator);
        private void GPSTriangulatorButtonClick(object? sender, RoutedEventArgs e) => NavigateTo(new GPSTriangulator(), GpsIndicator);
    }
}