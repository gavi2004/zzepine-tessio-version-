using System.Collections.Generic;

namespace GTAVInjector.Core
{
    public static class LocalizationManager
    {
        private static string _currentLanguage = "es";
        
        public static string CurrentLanguage => _currentLanguage;
        
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["en"] = new Dictionary<string, string>
            {
                ["DllList"] = "DLL List",
                ["AddDll"] = "+ Add DLL",
                ["AutoInject"] = "Auto-Inject on game start",
                ["LaunchGame"] = "Launch Game",
                ["InjectDlls"] = "Inject DLLs",
                ["KillGame"] = "Kill Game",
                ["GameType"] = "Game Type",
                ["Launcher"] = "Launcher",
                ["Requirements"] = "Requirements",
                ["Devs"] = "Devs",
                ["VersionStatus"] = "Version Status",
                ["UpdateAvailable"] = "Update Available",
                ["UpToDate"] = "Up to date",
                ["CheckingUpdates"] = "Checking for updates...",
                ["UpdateCheckFailed"] = "Update check failed",
                ["GameRunning"] = "Game: Running",
                ["GameNotRunning"] = "Game: Not Running",
                ["Injected"] = "Injected",
                ["NotInjected"] = "Not Injected",
                ["SelectDlls"] = "Select DLL files",
                ["LaunchingGame"] = "Launching game...",
                ["GameLaunched"] = "Game launched",
                ["LaunchFailed"] = "Failed to launch game",
                ["Injecting"] = "Injecting DLLs...",
                ["InjectionComplete"] = "Injection complete",
                ["InjectionSuccess"] = "DLLs injected successfully",
                ["InjectionFailed"] = "Injection failed",
                ["GameKilled"] = "Game process terminated",
                ["NoDllsEnabled"] = "No DLLs enabled for injection",
                ["AutoInjecting"] = "Auto-injecting DLLs...",
                ["UpdateConfirm"] = "Do you want to download and install the update?",
                ["ViewChangelog"] = "View Changelog",
                ["CheckUpdates"] = "🔄 Check Updates",
                ["VcRequirement"] = "• VC++ Redist x86 & x64",
                ["GtaRequirement"] = "• GTA V installed",
                ["AdminRequirement"] = "• Administrator rights",
                ["Language"] = "Language",
                ["GameKilled"] = "Game terminated"
            },
            ["es"] = new Dictionary<string, string>
            {
                ["DllList"] = "Lista de DLLs",
                ["AddDll"] = "+ Agregar DLL",
                ["AutoInject"] = "Auto-inyectar al iniciar el juego",
                ["LaunchGame"] = "Iniciar Juego",
                ["InjectDlls"] = "Inyectar DLLs",
                ["KillGame"] = "Cerrar Juego",
                ["GameType"] = "Tipo de Juego",
                ["Launcher"] = "Lanzador",
                ["Requirements"] = "Requisitos",
                ["Devs"] = "Desarrolladores",
                ["VersionStatus"] = "Estado de Versión",
                ["UpdateAvailable"] = "Actualización Disponible",
                ["UpToDate"] = "Actualizado",
                ["CheckingUpdates"] = "Verificando actualizaciones...",
                ["UpdateCheckFailed"] = "Error al verificar actualizaciones",
                ["GameRunning"] = "Juego: En Ejecución",
                ["GameNotRunning"] = "Juego: No Ejecutándose",
                ["Injected"] = "Inyectado",
                ["NotInjected"] = "No Inyectado",
                ["SelectDlls"] = "Seleccionar archivos DLL",
                ["LaunchingGame"] = "Iniciando juego...",
                ["GameLaunched"] = "Juego iniciado",
                ["LaunchFailed"] = "Error al iniciar el juego",
                ["Injecting"] = "Inyectando DLLs...",
                ["InjectionComplete"] = "Inyección completada",
                ["InjectionSuccess"] = "DLLs inyectadas exitosamente",
                ["InjectionFailed"] = "Error en la inyección",
                ["GameKilled"] = "Proceso del juego terminado",
                ["NoDllsEnabled"] = "No hay DLLs habilitadas para inyectar",
                ["AutoInjecting"] = "Auto-inyectando DLLs...",
                ["UpdateConfirm"] = "¿Deseas descargar e instalar la actualización?",
                ["ViewChangelog"] = "Ver Changelog",
                ["CheckUpdates"] = "🔄 Verificar Actualizaciones",
                ["VcRequirement"] = "• VC++ Redistribuible x86 y x64",
                ["GtaRequirement"] = "• GTA V instalado",
                ["AdminRequirement"] = "• Permisos de administrador",
                ["Language"] = "Idioma",
                ["GameKilled"] = "Juego terminado"
            }
        };

        public static void SetLanguage(string languageCode)
        {
            if (Translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
            }
        }

        public static string GetString(string key)
        {
            if (Translations.TryGetValue(_currentLanguage, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            return key;
        }
    }
}
