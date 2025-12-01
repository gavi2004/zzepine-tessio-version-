using System.Collections.Generic;
<<<<<<< HEAD
=======
using System.IO;
>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568
using GTAVInjector.Models;

namespace GTAVInjector.Models
{
    public class AppSettings
    {
        public GameType GameType { get; set; } = GameType.Enhanced;
        public LauncherType LauncherType { get; set; } = LauncherType.Rockstar;
        public List<DllEntry> DllEntries { get; set; } = new();
        public bool AutoInject { get; set; } = false;
        public string Language { get; set; } = "es";
<<<<<<< HEAD
        public string Version { get; set; } = "2.0.0";
=======
        public string Version { get; set; } = GetVersionFromFile();
        
        private static string GetVersionFromFile()
        {
            try
            {
                return File.ReadAllText("version.txt").Trim();
            }
            catch
            {
                return "2.0.0"; // Fallback
            }
        }
>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568
    }
}
