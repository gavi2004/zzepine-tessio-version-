using System.Collections.Generic;
using System.IO;
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
    }
}
