using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace GTAVInjector.Core
{
    public static class VersionChecker
    {
        // URLs para verificación de versiones - GitHub Raw como fuente principal
        private const string VERSION_JSON_URL = "https://raw.githubusercontent.com/Tessio/Translations/refs/heads/master/version_l.txt";
        private const string TESSIO_DISCORD_URL = "https://gtaggs.wirdland.xyz/discord";
        private const string CHANGELOG_URL = "https://github.com/Tessio/TessioScript-Launcher/releases";
        
        // Estado del verificador de versiones
        private static string? _latestVersion;
        private static bool _isOutdated = false;
        private static bool _isChecking = false;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10) // Timeout de 10 segundos
        };
        private static System.Threading.Timer? _versionTimer;

        // VERSIÓN FIJA DESDE CSPROJ - NO USAR ASSEMBLY
        private static string GetCurrentVersionFromProject()
        {
            // Versión exacta del .csproj (debe actualizarse manualmente aquí)
            return "1.0.7"; // ⚠️ ACTUALIZAR ESTO CUANDO CAMBIES LA VERSIÓN DEL PROYECTO
        }
        public static async Task<bool> CheckForUpdatesAsync(bool forceCheck = false)
        {
            // Prevenir múltiples verificaciones simultáneas
            if (_isChecking && !forceCheck)
            {
                System.Diagnostics.Debug.WriteLine("🔄 Verificación ya en progreso, saltando...");
                return _isOutdated;
            }

            // Cache de 5 minutos para evitar spam al servidor (excepto si es forzado)
            if (!forceCheck && (DateTime.Now - _lastCheckTime).TotalMinutes < 5)
            {
                System.Diagnostics.Debug.WriteLine("⚡ Usando resultado cacheado de verificación");
                return _isOutdated;
            }

            _isChecking = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("🌐 Iniciando verificación de versión desde GitHub...");
                
                // Configurar headers para evitar cache del navegador
                using var request = new HttpRequestMessage(HttpMethod.Get, VERSION_JSON_URL);
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("User-Agent", "TessioScript-Launcher/1.0");
                
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var githubVersion = await response.Content.ReadAsStringAsync();
                _latestVersion = githubVersion.Trim();

                // OBTENER VERSIÓN DEL PROYECTO
                var currentVersion = GetCurrentVersionFromProject();

                System.Diagnostics.Debug.WriteLine($"📱 VERSIÓN DEL PROYECTO: '{currentVersion}'");
                System.Diagnostics.Debug.WriteLine($"🌐 VERSIÓN DE GITHUB: '{_latestVersion}'");

                if (!string.IsNullOrEmpty(_latestVersion))
                {
                    // COMPARACIÓN ROBUSTA DE VERSIONES
                    var current = new Version(currentVersion);
                    var latest = new Version(_latestVersion);

                    _isOutdated = current < latest;
                    _lastCheckTime = DateTime.Now;
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 COMPARACIÓN: {currentVersion} < {_latestVersion} = {_isOutdated}");
                    
                    return _isOutdated;
                }

                return false;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"🌐 ERROR DE CONEXIÓN: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                System.Diagnostics.Debug.WriteLine($"⏰ TIMEOUT en verificación de versión: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR INESPERADO: {ex.Message}");
                return false;
            }
            finally
            {
                _isChecking = false;
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
            return GetCurrentVersionFromProject();
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
        public static void StartVersionMonitoring(Action<bool> onVersionChanged)
        {
            // Detener timer anterior si existe
            _versionTimer?.Dispose();
            
            _versionTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    bool wasOutdated = _isOutdated;
                    await CheckForUpdatesAsync();
                    
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    System.Diagnostics.Debug.WriteLine($"⏱️ [{timestamp}] Timer ejecutado - Estado anterior: {wasOutdated}, Estado actual: {_isOutdated}");
                    
                    // También escribir a un archivo log temporal para verificación
                    try 
                    {
                        System.IO.File.AppendAllText("tmp_rovodev_version_log.txt", 
                            $"[{timestamp}] VersionChecker ejecutado - Versión actual: {GetCurrentVersionFromProject()}, Versión remota: {_latestVersion}, Desactualizada: {_isOutdated}\n");
                    }
                    catch { /* Ignorar errores de escritura */ }
                    
                    if (wasOutdated != _isOutdated)
                    {
                        onVersionChanged?.Invoke(_isOutdated);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en timer: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10)); // Verificar cada 10 segundos
        }
        
        // Método para detener el monitoreo
        public static void StopVersionMonitoring()
        {
            _versionTimer?.Dispose();
            _versionTimer = null;
        }

        // ✨ NUEVOS MÉTODOS MEJORADOS ✨

        /// <summary>
        /// Verifica si actualmente se está ejecutando una verificación
        /// </summary>
        public static bool IsChecking()
        {
            return _isChecking;
        }

        /// <summary>
        /// Obtiene el tiempo transcurrido desde la última verificación
        /// </summary>
        public static TimeSpan GetTimeSinceLastCheck()
        {
            return DateTime.Now - _lastCheckTime;
        }

        /// <summary>
        /// Obtiene información detallada del estado de las versiones
        /// </summary>
        public static VersionInfo GetVersionInfo()
        {
            return new VersionInfo
            {
                CurrentVersion = GetCurrentVersionFromProject(),
                LatestVersion = _latestVersion,
                IsOutdated = _isOutdated,
                IsChecking = _isChecking,
                LastCheckTime = _lastCheckTime,
                TimeSinceLastCheck = GetTimeSinceLastCheck()
            };
        }

        /// <summary>
        /// Abre la página de changelog en el navegador
        /// </summary>
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

        /// <summary>
        /// Fuerza una verificación inmediata (ignora cache)
        /// </summary>
        public static async Task<bool> ForceCheckForUpdatesAsync()
        {
            return await CheckForUpdatesAsync(forceCheck: true);
        }

        /// <summary>
        /// Resetea el estado del verificador de versiones
        /// </summary>
        public static void ResetState()
        {
            _latestVersion = null;
            _isOutdated = false;
            _lastCheckTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Clase que contiene información detallada sobre las versiones
    /// </summary>
    public class VersionInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string? LatestVersion { get; set; }
        public bool IsOutdated { get; set; }
        public bool IsChecking { get; set; }
        public DateTime LastCheckTime { get; set; }
        public TimeSpan TimeSinceLastCheck { get; set; }

        public bool HasBeenChecked => LastCheckTime > DateTime.MinValue;
        public string StatusText => IsChecking ? "Verificando..." : 
                                  !HasBeenChecked ? "No verificado" :
                                  IsOutdated ? $"Desactualizada ({CurrentVersion} → {LatestVersion})" : 
                                  "Actualizada";
    }
}
