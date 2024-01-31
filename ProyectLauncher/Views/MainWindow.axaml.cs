using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProyectLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += LoadWindow;
    }

    private void LoadWindow(object sender, RoutedEventArgs e)
    {
        Title = "Launcher: ";

        #if Windows
            Title += "Windows";
        #elif OSX
            Title += "MacOS";
        #elif Linux
            Title += "Linux";
        #endif
    }
}
