using Avalonia.Controls;
using Avalonia.Interactivity;
using PinoLauncher.ViewModels;
using System.Linq;

namespace PinoLauncher.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(MainViewModel mainViewModel) : this()
    {
        var settingsViewModel = new SettingsViewModel
        {
            ParentViewModel = mainViewModel,
            RequestClose = () => Close()
        };

        // Sincronizar todas las configuraciones actuales del MainViewModel
        settingsViewModel.SyncFromMainViewModel(mainViewModel);

        DataContext = settingsViewModel;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}