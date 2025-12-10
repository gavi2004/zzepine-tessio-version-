using System;
using System.Diagnostics;

namespace GTAVInjector.Core
{
    public static class VersionChecker
    {
        // URLs para verificación de versiones - GitHub Raw como fuente principal
        
        private const string TESSIO_DISCORD_URL_PATREONS = "https://discord.com/channels/1037927157822918666/1319801942846996480";
        private const string CHANGELOG_URL = "https://github.com/Tessio/TessioScript-Launcher/releases";
       

        public static void OpenDiscordUpdate()
        {

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TESSIO_DISCORD_URL_PATREONS,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Discord: {ex.Message}");
            }
        }

      

        public static void OpenChangelog()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = CHANGELOG_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open changelog: {ex.Message}");
            }
        }

    }


}
