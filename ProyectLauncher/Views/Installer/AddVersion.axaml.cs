using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProyectLauncher.Views.Installer
{
    public partial class AddVersion : Window
    {
        public AddVersion()
        {
            InitializeComponent();
            this.Loaded += LoadData;
        }

        private void LoadData(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
