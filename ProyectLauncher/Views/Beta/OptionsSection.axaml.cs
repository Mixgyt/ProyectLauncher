using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ProyectLauncher.Views.Beta.Options;

namespace ProyectLauncher.Views.Beta
{
    public partial class OptionsSection : UserControl
    {
        public OptionsSection()
        {
            InitializeComponent();
            BtnOptions.Click += OpenOptions_click;
        }

        private void OpenOptions_click(object? sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            var parent = this.GetVisualRoot() as Window;
            options.ShowDialog(parent);
        }
    }
}
