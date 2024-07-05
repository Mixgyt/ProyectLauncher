using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProyectLauncher.Classes
{
    public class ModVersion : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("version")]
        public string Version { get; set; }
    }
}
