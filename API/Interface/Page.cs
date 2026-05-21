using Avalonia.Controls;

namespace SEUtilityTools.API.Interface
{
    public abstract class Page : Window
    {
        /// <summary>
        /// Gets the name of the page.
        /// </summary>
        public abstract string PageName { get; }

        /// <summary>
        /// Gets the description of the page.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Returns the Avalonia control tree for this page.
        /// </summary>
        public abstract Control CreateContent();

        /// <summary>
        /// Called whenever the page is clicked on.
        /// </summary>
        public virtual void OnClicked() { }

        /// <summary>
        /// Called whenever the page is closed.
        /// </summary>
        public virtual void OnClosed() { }

        /// <summary>
        /// Clears the content panel and fills it with this page's content.
        /// </summary>
        public virtual void FillContent(Panel contentArea)
        {
            contentArea.Children.Clear();
            contentArea.Children.Add(CreateContent());
        }
    }
}
