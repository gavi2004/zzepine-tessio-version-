using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using GTAVInjector.Core;

namespace GTAVInjector
{
    public class VersionValidator
    {
        private readonly string serverBaseUrl = "http://localhost:3000/api";
        private readonly string currentVersion = "1.0.8"; // ← TU VERSIÓN ACTUAL DEL INYECTOR
        private bool _hasShownOutdatedError = false; // ← Para mostrar el error solo una vez
        
        // URLs de los endpoints
        private string ValidateUrl => $"{serverBaseUrl}/validate";
        private string VersionUrl => $"{serverBaseUrl}/version";
        
        /// <summary>
        /// Valida la versión del inyector contra el servidor (con diálogos)
        /// </summary>
        public async Task<bool> ValidateVersionAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Timeout de 10 segundos para mejor conexión
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // Primero obtener la versión actual del servidor
                    var serverVersion = await GetServerVersionAsync(client);
                    if (serverVersion == null)
                    {
                        ShowConnectionError();
                        return false;
                    }
                    
                    // Luego validar nuestra versión contra el servidor
                    var validationResult = await ValidateAgainstServerAsync(client);
                    if (validationResult == null)
                    {
                        ShowConnectionError();
                        return false;
                    }
                    
                    // Procesar resultado de validación
                    return ProcessValidationResult(validationResult, serverVersion);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión HTTP: {ex.Message}");
                ShowConnectionError();
                return false;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"⏰ Timeout en validación: {ex.Message}");
                ShowConnectionError();
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado en validación: {ex.Message}");
                ShowConnectionError();
                return false;
            }
        }
        
        /// <summary>
        /// Valida la versión del inyector de forma silenciosa (sin diálogos)
        /// </summary>
        public async Task<VersionValidationInfo> ValidateVersionSilentAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    var serverVersion = await GetServerVersionAsync(client);
                    if (serverVersion == null)
                    {
                        return new VersionValidationInfo
                        {
                            IsValid = false,
                            ErrorType = ValidationErrorType.ConnectionError,
                            Message = "No se pudo conectar con el servidor",
                            ClientVersion = currentVersion
                        };
                    }
                    
                    var validationResult = await ValidateAgainstServerAsync(client);
                    if (validationResult == null)
                    {
                        return new VersionValidationInfo
                        {
                            IsValid = false,
                            ErrorType = ValidationErrorType.ServerError,
                            Message = "Error en la respuesta del servidor",
                            ClientVersion = currentVersion,
                            ServerVersion = serverVersion.version
                        };
                    }
                    
                    return new VersionValidationInfo
                    {
                        IsValid = validationResult.allowed,
                        ErrorType = validationResult.allowed ? ValidationErrorType.None : ValidationErrorType.VersionMismatch,
                        Message = validationResult.message,
                        ClientVersion = validationResult.clientVersion,
                        ServerVersion = validationResult.serverVersion,
                        ServerTimestamp = serverVersion.timestamp,
                        ComparisonResult = CompareVersions(validationResult.clientVersion, validationResult.serverVersion)
                    };
                }
            }
            catch (HttpRequestException)
            {
                return new VersionValidationInfo
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.ConnectionError,
                    Message = "Error de conexión HTTP",
                    ClientVersion = currentVersion
                };
            }
            catch (TaskCanceledException)
            {
                return new VersionValidationInfo
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.Timeout,
                    Message = "Tiempo de espera agotado",
                    ClientVersion = currentVersion
                };
            }
            catch (Exception ex)
            {
                return new VersionValidationInfo
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.UnknownError,
                    Message = $"Error inesperado: {ex.Message}",
                    ClientVersion = currentVersion
                };
            }
        }
        
        /// <summary>
        /// Obtiene información del servidor (público)
        /// </summary>
        public async Task<ServerVersionResponse> GetServerInfoAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    return await GetServerVersionAsync(client);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo info del servidor: {ex.Message}");
                return null!;
            }
        }
        
        /// <summary>
        /// Obtener información de versión del servidor
        /// </summary>
        private async Task<ServerVersionResponse> GetServerVersionAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync(VersionUrl);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Servidor respondió con estado: {response.StatusCode}");
                    return null!;
                }
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerVersionResponse>(jsonContent) ?? new ServerVersionResponse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en GetServerVersionAsync: {ex.Message}");
                return null!;
            }
        }
        
        /// <summary>
        /// Validar versión contra el servidor
        /// </summary>
        private async Task<ValidationResult> ValidateAgainstServerAsync(HttpClient client)
        {
            try
            {
                var payload = new { version = currentVersion };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(ValidateUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Validación falló con estado: {response.StatusCode}");
                    return null!;
                }
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ValidationResult>(jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ValidateAgainstServerAsync: {ex.Message}");
                return null!;
            }
        }
        
        /// <summary>
        /// Procesar resultado de validación y mostrar diálogos correspondientes
        /// </summary>
        private bool ProcessValidationResult(ValidationResult validation, ServerVersionResponse serverInfo)
        {
            if (!validation.allowed)
            {
                if (!_hasShownOutdatedError)
                {
                    _hasShownOutdatedError = true;
                    ShowVersionMismatchDialog(validation);
                }
                return false;
            }
            
            ShowSuccessMessage(validation, serverInfo);
            return true;
        }
        
        /// <summary>
        /// Mostrar diálogo de versión incorrecta
        /// </summary>
        private void ShowVersionMismatchDialog(ValidationResult validation)
        {
            var comparison = CompareVersions(validation.clientVersion, validation.serverVersion);
            
            string logMessage;
            if (comparison < 0)
            {
                logMessage = $"Cliente desactualizado: {validation.clientVersion} < {validation.serverVersion}";
            }
            else if (comparison > 0)
            {
                logMessage = $"Cliente más nuevo: {validation.clientVersion} > {validation.serverVersion}";
            }
            else
            {
                logMessage = "Versiones iguales pero acceso denegado";
            }
            
            System.Diagnostics.Debug.WriteLine($"ShowVersionMismatchDialog: {logMessage}");
            
            // Mostrar mensaje simple de version desactualizada con localización
            var isSpanish = LocalizationManager.CurrentLanguage.ToLower() == "es";
            var localizedMessage = isSpanish ? "Version obsoleta. Por favor actualice el inyector." : "Outdated version. Please update the injector.";
            var localizedTitle = isSpanish ? "Version Desactualizada" : "Version Outdated";
            
            MessageBox.Show(
                localizedMessage,
                localizedTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
        
        /// <summary>
        /// Mostrar mensaje de validación exitosa
        /// </summary>
        private void ShowSuccessMessage(ValidationResult validation, ServerVersionResponse serverInfo)
        {
            var message = "✅ VERSIÓN VALIDADA CORRECTAMENTE\n\n" +
                $"🎯 Tu versión: v{validation.clientVersion}\n" +
                $"🖥️ Servidor: v{validation.serverVersion}\n" +
                $"🕒 Última actualización: {DateTime.Parse(serverInfo.timestamp):dd/MM/yyyy HH:mm}\n\n" +
                $"📝 {validation.message}";
                
            System.Diagnostics.Debug.WriteLine(message.Replace("\n", " "));
        }
        
        /// <summary>
        /// Compara dos versiones semánticamente
        /// </summary>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
                var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
                
                for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
                {
                    int v1 = i < v1Parts.Length ? v1Parts[i] : 0;
                    int v2 = i < v2Parts.Length ? v2Parts[i] : 0;
                    
                    if (v1 > v2) return 1;
                    if (v1 < v2) return -1;
                }
                return 0;
            }
            catch (Exception)
            {
                // Fallback a comparación de strings si falla el parsing
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }
        
        /// <summary>
        /// Mostrar error de conexión
        /// </summary>
        private void ShowConnectionError()
        {
            var message = "❌ NO SE PUDO CONECTAR AL SERVIDOR DE VERSIONES\n\n" +
                         "💡 Asegúrate de que 'node version-server.js' esté ejecutándose.";
            
            System.Diagnostics.Debug.WriteLine(message.Replace("\n", " "));
        }
    }
    
    /// <summary>
    /// Resultado de validación del servidor
    /// </summary>
    public class ValidationResult
    {
        public bool success { get; set; }
        public bool allowed { get; set; }
        public string message { get; set; } = string.Empty;
        public string clientVersion { get; set; } = string.Empty;
        public string serverVersion { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Respuesta de versión del servidor
    /// </summary>
    public class ServerVersionResponse
    {
        public bool success { get; set; }
        public string version { get; set; } = string.Empty;
        public string timestamp { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Información detallada de validación de versión
    /// </summary>
    public class VersionValidationInfo
    {
        public bool IsValid { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ClientVersion { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public string ServerTimestamp { get; set; } = string.Empty;
        public int ComparisonResult { get; set; } // -1: cliente<servidor, 0: iguales, 1: cliente>servidor
        
        public bool IsClientOutdated => ComparisonResult < 0;
        public bool IsClientNewer => ComparisonResult > 0;
        public bool AreVersionsEqual => ComparisonResult == 0;
    }
    
    /// <summary>
    /// Tipos de errores de validación
    /// </summary>
    public enum ValidationErrorType
    {
        None,
        ConnectionError,
        Timeout,
        ServerError,
        VersionMismatch,
        UnknownError
    }
}