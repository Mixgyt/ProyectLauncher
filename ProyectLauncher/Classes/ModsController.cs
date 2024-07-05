using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using MsBox.Avalonia;
using Supabase;
using Supabase.Gotrue.Exceptions;

namespace ProyectLauncher.Classes
{
    public static class ModsController
    {
        public static event EventHandler FinishDownload;

        public static async void Update_mods(Client supabase, ProgressBar modBar, Label lbl)
        {
            Settings settings = Settings.Instance();
            lbl.Foreground = Brushes.White;
            string modPath = settings.ModsPath;

            //Obtiene los mods alojados en la base de datos
            var remote_files = await supabase.Storage.From("MODS").List();

            //Si la carpeta de mods no esta creada entonces la crea
            if (!Directory.Exists(modPath))
            {
                Directory.CreateDirectory(modPath);
            }

            //Obtiene los mods guardados en la carpeta mods
            List<string> local_mods = Directory.GetFiles(modPath).ToList();

            //Si la funcion de eliminar mods viejos esta habilitada
            if (settings.DeleteOldMods)
            {
                //Revisa los mods locales y remotos para que si uno no coincide se elimina de la carpeta mods
                foreach (var lfile in local_mods)
                {
                    if (remote_files.Any(r => r.Name == Path.GetFileName(lfile)))
                    {
                        continue;
                    }
                    File.Delete(lfile);
                }
                //Se vuelve a tomar la lista mods en la carpeta mods luego de la eliminacion
                local_mods = Directory.GetFiles(modPath).ToList();
            }

            //Se toma revisan los mods no descargados
            foreach (var rfile in remote_files)
            {
                //Si hay un mod que no esta descargado este procede a descargarse
                if(local_mods.All(m => Path.GetFileName(m) != rfile.Name))
                {
                    //Se muestra la informacion en un label
                    lbl.Content = $"Descargando: {rfile.Name}";


                    _ = await supabase.Storage.From("MODS").Download(rfile.Name, $"{modPath}/{rfile.Name}",
                            (sender, prog) => { modBar.Value = prog; });
                }
            }
            //Al terminar el proceso se muestra que ya estan instalados y se habilita el boton
            lbl.Content = "Mods Instalados:";
            FinishDownload?.Invoke(null,EventArgs.Empty);
        }

        public static async Task<bool> New_Mods(Client supabase)
        {
            string modPath = Settings.Instance().ModsPath;
            var local_mods = Directory.GetFiles(modPath).ToList();
            var remote_files = await supabase.Storage.From("MODS").List();

            foreach (var rfile in remote_files)
            {
                if (local_mods.All(m => Path.GetFileName(m) != rfile.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
