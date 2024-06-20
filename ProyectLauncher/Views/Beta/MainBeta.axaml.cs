using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ProyectLauncher.Classes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.IO;
using CmlLib.Core;
using MsBox.Avalonia;

namespace ProyectLauncher.Views.Beta
{
    public partial class MainBeta : UserControl
    {
        private string path_settings = "./settings.json";
        public MainBeta()
        {
            InitializeComponent();
            //Activar controles
            Launcher.CompleteDownload += FinishDownload;
            Launcher.CloseMC += CloseMC;
            //Iniciar lanzador
            btn_launch.Click += Launch_btn;
            //Barra de progreso
            Launcher.MCLauncher.ProgressChanged += (sender, e) =>
            {
                pgr_descarga.Value = e.ProgressPercentage;
            };
            //Cargar Nombre
            //LoadUser();
            //Cambio en el usuario
            txt_user.TextChanged += Change_User;
        }

        private void LoadUser()
        {
            if (!File.Exists(path_settings))
            {
                File.Create(path_settings).Close();

            }

            var json = new StreamReader(path_settings);
            string txt = json.ReadToEnd();
            json.Close();
            Settings settings = JsonConvert.DeserializeObject<Settings>(txt);
            if (settings != null)
            {
                txt_user.Text = settings.UserName;
            }
        }

        private void Launch_btn(object? sender, RoutedEventArgs e)
        {
            string UserName = txt_user.Text;
            if (!string.IsNullOrEmpty(UserName))
            {
                UserName = UserName.Trim();
            }
            else
            {
                UserName = "User";
            }
            EnableControls(false);
            Launcher.LaunchVersion("fabric-loader-0.15.11-1.19.2",UserName);
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
            string UserName = txt_user.Text;
            if (!string.IsNullOrEmpty(UserName))
            {
                UserName = UserName.Trim();
            }
            else
            {
                UserName = "User";
            }
            SaveUser(UserName);
        }

        private void CloseMC(object? sender, MLaunchOption e)
        {
            EnableControls();
        }

        private void EnableControls(bool enable = true)
        {
            btn_launch.IsEnabled = txt_user.IsEnabled = enable;
        }

        private void SaveUser(string UserName)
        {
            string txt = JsonConvert.SerializeObject(new Settings() { UserName = UserName }, Formatting.Indented);
            File.WriteAllText(path_settings,txt);
        }
    }
}
