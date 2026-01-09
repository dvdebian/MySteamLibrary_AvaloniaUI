using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Linq;

namespace MySteamLibrary.Views
{
    public partial class CoverView : UserControl
    {
        public CoverView()
        {
            InitializeComponent();

            // Start with the first item selected
            var listBox = this.FindControl<ListBox>("CoverList");
            if (listBox != null)
            {
                listBox.SelectedIndex = 0;
            }
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var listBox = sender as ListBox;
            var scrollViewer = listBox?.GetValue(ListBox.ScrollProperty) as ScrollViewer;

            if (scrollViewer != null)
            {
                // Redirect vertical wheel to horizontal offset
                double scrollStep = 150;
                if (e.Delta.Y > 0)
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X - scrollStep, 0);
                else
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X + scrollStep, 0);

                e.Handled = true;
            }
        }
    }
}