using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CmlLib.Core;
using ProyectLauncher.Classes;

namespace ProyectLauncher.Views.Beta.Options
{
    public partial class OptionsWindow : Window
    {
        private readonly Settings _settings = Settings.Instance();
        public OptionsWindow()
        {
            InitializeComponent();
            Loaded += LoadWindow;
            //Seleccionar Carpetas
            BtnFindMCFolder.Click += FindMCFolder_click;
            BtnFindModsFolder.Click += FindModsFolder_click;
            //Cambiar RAM
            SlRAM.ValueChanged += RAM_change;
            //Abrir Carpetas
            BtnOpenMC.Click += OpenMcFolder;
            BtnOpenMods.Click += OpenModsFolder;
            //Cambiar Checkbox
            CkCloseLauncher.IsCheckedChanged += CloseLauncher_change;
            CkFullScreenMC.IsCheckedChanged += FullScreen_change;
            CkInstallAllMods.IsCheckedChanged += InstallAllMods_change;
            CkDeleteOldMods.IsCheckedChanged += DeleteOldMods_change;
        }

        private void LoadWindow(object? sender, RoutedEventArgs e)
        {
            TxtMcPath.Text = _settings.MCPath.BasePath;
            TxtModsPath.Text = _settings.ModsPath;
            CkCloseLauncher.IsChecked = _settings.CloseLauncher;
            CkInstallAllMods.IsChecked = _settings.InstallAllMods;
            CkFullScreenMC.IsChecked = _settings.Options.FullScreen;
            CkDeleteOldMods.IsChecked = _settings.DeleteOldMods;
            SlRAM.Maximum = (double)Settings.TotalRam()-1000;
            SlRAM.Value = _settings.Options.MaximumRamMb;
            LblRAM.Content = $"RAM Habilitada: {SlRAM.Value}/{SlRAM.Maximum} MB";
        }

        private void RAM_change(object? sender, RangeBaseValueChangedEventArgs e)
        {
            _settings.Options.MaximumRamMb = (int)SlRAM.Value;
            Settings.SaveChanges(_settings);
            LblRAM.Content = $"RAM Habilitada: {(int)SlRAM.Value}/{SlRAM.Maximum} MB";
        }

        private async void FindMCFolder_click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Carpeta donde se guardara minecraft",
                AllowMultiple = false,
            });

            if (folder.Count > 0)
            {
                var owner = Owner as MainWindow;
                _settings.ChangeMcPath(folder[0].Path.LocalPath, owner.Control);
                Settings.SaveChanges(_settings);
            }
        }

        private async void FindModsFolder_click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Carpeta donde se descargaran los mods",
                AllowMultiple = false,
            });

            if (folder.Count > 0)
            {
                _settings.ChangeModsPath(folder[0].Path.LocalPath);
                Settings.SaveChanges(_settings);
            }
        }

        private void OpenMcFolder(object? sender, RoutedEventArgs e)
        {
            Settings.OpenFolder(_settings.MCPath.BasePath);
        }

        private void OpenModsFolder(object? sender, RoutedEventArgs e)
        {
            Settings.OpenFolder(_settings.ModsPath);
        }

        private void CloseLauncher_change(object? sender, RoutedEventArgs e)
        {
            _settings.CloseLauncher = (bool)CkCloseLauncher.IsChecked;
            Settings.SaveChanges(_settings);
        }

        private void FullScreen_change(object? sender, RoutedEventArgs e)
        {
            _settings.Options.FullScreen = (bool)CkFullScreenMC.IsChecked;
            Settings.SaveChanges(_settings);
        }

        private void InstallAllMods_change(object? sender, RoutedEventArgs e)
        {
            _settings.ChangeInstallAllMods((bool)CkInstallAllMods.IsChecked);
            Settings.SaveChanges(_settings);
        }

        private void DeleteOldMods_change(object? sender, RoutedEventArgs e)
        {
            _settings.ChangeOldMods((bool)CkDeleteOldMods.IsChecked);
            Settings.SaveChanges(_settings);
        }
    }
}
