using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Styling;
using SEUtilityTools.API.Interface;
using SEUtilityTools.Pages;

namespace SEUtilityTools
{
    public partial class MainWindow : Window
    {
        private Rectangle? _activeIndicator;
        private Page? _activePage;
        private bool _sidebarVisible = true;
        private bool _isAnimating = false;

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

        private void HomeButtonClick(object? sender, RoutedEventArgs e)
            => NavigateTo(new Home(), HomeIndicator);

        private void SettingsButtonClick(object? sender, RoutedEventArgs e)
            => NavigateTo(new Settings(), SettingsIndicator);

        private void BlueprintCalculatorButtonClick(object? sender, RoutedEventArgs e)
            => NavigateTo(new BlueprintCalculator(), BlueprintIndicator);

        private void ServerQueryButtonClick(object? sender, RoutedEventArgs e)
            => NavigateTo(new ServerQuery(), ServerIndicator);
    }
}