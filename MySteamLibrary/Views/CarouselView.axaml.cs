using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MySteamLibrary.Views
{
    public partial class CarouselView : UserControl
    {
        public CarouselView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Converts vertical mouse wheel movement into horizontal scrolling for the carousel.
        /// </summary>
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            // Reference the ScrollViewer by the name defined in XAML
            var scrollViewer = this.FindControl<ScrollViewer>("CarouselScroll");

            if (scrollViewer != null)
            {
                // Define the speed of the scroll
                double scrollStep = 150;

                // If wheel moves up (Delta.Y > 0), scroll left. Otherwise scroll right.
                if (e.Delta.Y > 0)
                {
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X - scrollStep, 0);
                }
                else
                {
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X + scrollStep, 0);
                }

                // Mark the event as handled so it doesn't trigger parent scrolling
                e.Handled = true;
            }
        }
    }
}