using Avalonia.Controls;
using PinoLauncher.ViewModels;
using System;
using System.ComponentModel;

namespace PinoLauncher.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.LogText))
        {
            // Auto-scroll al final del log cuando se actualice
            if (LogScrollViewer != null)
            {
                LogScrollViewer.ScrollToEnd();
            }
        }
    }
}