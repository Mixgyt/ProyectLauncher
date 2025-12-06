using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PinoLauncher.Views;

public partial class ModLoaderConfigWindow : Window
{
    public ModLoaderConfigWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}