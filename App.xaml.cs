using System.Windows;

namespace GTAVInjector
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Cargar configuración
            Core.SettingsManager.LoadSettings();
            
            // Cargar idioma guardado
            Core.LocalizationManager.SetLanguage(Core.SettingsManager.Settings.Language);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Guardar configuración al salir
            Core.SettingsManager.SaveSettings();
            base.OnExit(e);
        }
    }
}
