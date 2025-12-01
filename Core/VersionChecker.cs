using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
<<<<<<< HEAD
using System.Text.Json;

namespace GTAVInjector.Core
{
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DiscordUrl { get; set; } = string.Empty;
    }

    public static class VersionChecker
    {
        private const string VERSION_JSON_URL = "https://raw.githubusercontent.com/zzepine/tessio-version/main/version.json";
        private const string CURRENT_VERSION = "2.0.1";
        private const string TESSIO_DISCORD_URL = "https://discord.gg/tessioScript";
=======

namespace GTAVInjector.Core
{
    public static class VersionChecker
    {
        private const string VERSION_JSON_URL = "https://raw.githubusercontent.com/gavi2004/zzepine-tessio-version-/main/version.txt";
        private const string TESSIO_DISCORD_URL = "https://discord.gg/tessioScript";
        
        private static string? _currentVersion;
>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568

        private static string? _latestVersion;
        private static bool _isOutdated = false;
        private static readonly HttpClient _httpClient = new();

<<<<<<< HEAD
=======
        private static string GetCurrentVersionFromFile()
        {
            if (_currentVersion == null)
            {
                try
                {
                    _currentVersion = File.ReadAllText("version.txt").Trim();
                }
                catch
                {
                    _currentVersion = "2.0.0"; // Fallback version
                }
            }
            return _currentVersion;
        }

>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568
        public static async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(VERSION_JSON_URL);
<<<<<<< HEAD
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(response);

                if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.Version))
                {
                    _latestVersion = versionInfo.Version;
                    
                    // Comparar versiones
                    var current = new Version(CURRENT_VERSION);
=======
                _latestVersion = response.Trim();

                if (!string.IsNullOrEmpty(_latestVersion))
                {
                    
                    // Comparar versiones
                    var current = new Version(GetCurrentVersionFromFile());
>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568
                    var latest = new Version(_latestVersion);

                    _isOutdated = latest > current;
                    return _isOutdated;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void OpenDiscordUpdate()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TESSIO_DISCORD_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Discord: {ex.Message}");
            }
        }

        public static string GetCurrentVersion()
        {
<<<<<<< HEAD
            return CURRENT_VERSION;
=======
            return GetCurrentVersionFromFile();
>>>>>>> 1f394c95213f1ee770ede05d45e7b1433d30f568
        }

        public static string? GetLatestVersion()
        {
            return _latestVersion;
        }

        public static bool IsOutdated()
        {
            return _isOutdated;
        }

        // Timer para verificar constantemente las actualizaciones
        public static async Task StartVersionMonitoring(Action<bool> onVersionChanged)
        {
            var timer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    bool wasOutdated = _isOutdated;
                    await CheckForUpdatesAsync();
                    
                    if (wasOutdated != _isOutdated)
                    {
                        onVersionChanged?.Invoke(_isOutdated);
                    }
                }
                catch
                {
                    // Ignorar errores de red silenciosamente
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // Verificar cada 5 minutos
        }
    }
}
