using Avalonia.Controls;
using Avalonia.Media;
using SEUtilityTools.API.Interface;

namespace SEUtilityTools.Pages
{
    public class Home : Page
    {
        public override string PageName => "Home";
        public override string Description => "Landing page";

        public override Control CreateContent()
        {
            return new TextBlock
            {
                Text = "Welcome to SE Utility Tools",
                Foreground = Brushes.White,
                FontSize = 24,
                Margin = new Avalonia.Thickness(32)
            };
        }

        public override void OnClicked()
        {

        }

        public override void OnClosed()
        {

        }
    }
}