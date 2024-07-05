using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ProyectLauncher.Classes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using CmlLib.Core;
using CmlLib.Core.Downloader;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Supabase.Gotrue.Exceptions;
using Client = Supabase.Client;
using Settings = ProyectLauncher.Classes.Settings;

namespace ProyectLauncher.Views.Beta
{
    public partial class MainBeta : UserControl
    {
        Client _supabase;
        private Settings settings;

        public MainBeta()
        {
            InitializeComponent();
            //Activar controles
            Launcher.CompleteDownload += FinishDownload;
            Launcher.CloseMc += CloseMc;
            //Iniciar lanzador
            btn_launch.Click += Launch_btn;
            //Actualizar Mods
            btn_mods.Click += UpdateMods;
            //Barra de progreso
            Launcher.McLauncher.ProgressChanged += (sender, e) =>
            {
                pgr_descarga.Value = e.ProgressPercentage;
            };
            //Muestreo de archivos
            Launcher.McLauncher.FileChanged += (sender) =>
            {
                lbl_files.IsEnabled = true;
                lbl_files.Content = $"{sender.ProgressedFileCount}/{sender.TotalFileCount} '{sender.FileName}'";
            };
            //Cargar Nombre
            LoadUser();
            //Cambio en el usuario
            txt_user.TextChanged += Change_User;
        }

        //Carga para usuario y conexión con Supabase
        private async void LoadUser()
        {
            settings = Settings.Instance();
            txt_user.Text = settings.UserName;

            try
            {
                _supabase = new Client(SecureKeys.Url, SecureKeys.Key);
                await _supabase.InitializeAsync();
            }
            catch (GotrueException ex)
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", ex.Message).ShowAsPopupAsync(this);
            }

            if (await ModsController.New_Mods(_supabase))
            {
                lbl_mods.Content = "Es necesario actualizar los mods:";
                lbl_mods.Foreground = Brushes.Red;
            }
        }

        //Funcion que lanza minecraft
        private async void Launch_btn(object? sender, RoutedEventArgs e)
        {
            string userName = txt_user.Text;
            if (!string.IsNullOrEmpty(userName))
            {
                userName = userName.Trim();
            }
            else
            {
                await MessageBoxManager.GetMessageBoxStandard("Error","Error debes escribir un nombre de usuario",ButtonEnum.Ok,Icon.Error).ShowAsPopupAsync(this);
                return;
            }
            EnableControls(false);
            Launcher.LaunchVersion("fabric-loader-0.15.11-1.19.2",userName);
        }

        private void UpdateMods(object? sender, RoutedEventArgs e)
        {
            EnableControls(false);
            ModsController.Update_mods(_supabase, pgr_mods, lbl_mods);
            ModsController.FinishDownload += (o, ev) =>
            {
                EnableControls();
            };
        }

        private void FinishDownload(object sender, Task e)
        {
            Dispatcher.UIThread.Invoke(new Action(async () =>
            {
                EnableControls();
                if (!e.IsCompletedSuccessfully)
                {
                    await MessageBoxManager.GetMessageBoxStandard("Error", "Error al descargar los archivos de la version seleccionada", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsPopupAsync(this);
                }
            }));
        }

        private void Change_User(object? senderm, EventArgs e)
        {
            string userName = txt_user.Text;
            if (!string.IsNullOrEmpty(userName))
            {
                userName = userName.Trim();
            }
            else
            {
                userName = "";
            }
            SaveUser(userName);
        }

        private void CloseMc(object? sender, MLaunchOption e)
        {
            EnableControls();
        }

        private void EnableControls(bool enable = true)
        {
            btn_launch.IsEnabled = txt_user.IsEnabled = enable;
            btn_mods.IsEnabled = enable;
            BtnOptions.IsEnabled = enable;
        }

        private void SaveUser(string userName)
        {
            settings.ChangeUserName(userName);
            Settings.SaveChanges(settings);
        }
    }
}
