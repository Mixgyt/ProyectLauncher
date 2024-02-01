using System;

namespace ProyectLauncher.Classes
{
    class LastSesion
    {
        public DateTime Date { get; set; }
        public string PlayedVersion { get; set; }
        public string UserName { get; set; }

        public LastSesion(DateTime Date, string PlayedVersion, string UserName){ 
            this.Date = Date;
            this.PlayedVersion = PlayedVersion;
            this.UserName = UserName;
        }

        public bool Save()
        {

            return false;
        }
    }
}
