using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Avalonia.Media.Imaging;
using System.Text;
using Avalonia.Media;

namespace ProyectLauncher.Views.Beta
{
    public partial class ServerStatus : UserControl
    {
        public ServerStatus()
        {
            InitializeComponent();
            this.Loaded += LoadServerStatus;
        }

        //Al cargar el control se buscan la informacion respectiva
        private async void LoadServerStatus(object? sender, RoutedEventArgs e)
        {
            string url = "https://bridge-mc.netlify.app/mc/server.txt";
            string api = "https://api.mcsrvstat.us/3/";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;
            string title, serverIp, subtitle, players;
            string playersList = "";

            if (response.IsSuccessStatusCode)
            {
                string rootStr = response.Content.ReadAsStringAsync().Result.Trim();
                title = rootStr.Substring(0,rootStr.IndexOf('-'));
                serverIp = rootStr.Substring(rootStr.IndexOf('-')+1, rootStr.Length - rootStr.IndexOf('-')-1);

                var serverInfo = await client.GetFromJsonAsync<Dictionary<string,object>>(api+serverIp);

                bool active = JsonSerializer.Deserialize<bool>(((JsonElement)serverInfo["online"]).GetRawText());
                if (!active)
                {
                    LblSubtitle.Text = "Servidor fuera de linea";
                    LblPlayers.Text = "N/A";
                    Background = Brushes.Brown;
                    return;
                }

                var motd = JsonSerializer.Deserialize<Dictionary<string,object>>(((JsonElement)serverInfo["motd"]).GetRawText());
                subtitle = ((JsonElement)motd["clean"]).Deserialize<string[]>()[0];

                var playerProp = JsonSerializer.Deserialize<Dictionary<string, object>>(((JsonElement)serverInfo["players"]).GetRawText());
                var max = ((JsonElement)playerProp["max"]).Deserialize<int>();
                var online = ((JsonElement)playerProp["online"]).Deserialize<int>();
                players = $"{online}/{max}";

                if (online > 0)
                {
                    var pList = ((JsonElement)playerProp["list"]).Deserialize<object[]>();
                    foreach (var player in pList)
                    {
                        var p = ((JsonElement)player).Deserialize<Dictionary<string, string>>();
                        if (!string.IsNullOrEmpty(playersList))
                        {
                            playersList += ",";
                        }
                        playersList += p["name"];
                    }
                }
                
                var icon_base64 = JsonSerializer.Deserialize<string>(((JsonElement)serverInfo["icon"]).GetRawText()).Replace("data:image/png;base64,","");
                var icon_bytes = Convert.FromBase64String(icon_base64);
                var icon_stream = new MemoryStream(icon_bytes);
                PicIcon.Source = new Bitmap(icon_stream);

                LblTitleServer.Text = title;
                LblSubtitle.Text = subtitle;
                LblPlayers.Text = players;
                ToolTip.SetTip(LblPlayers,playersList);
            }

        }
    }
}
