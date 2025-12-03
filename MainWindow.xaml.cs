using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GTAVInjector.Core;
using GTAVInjector.Models;
using Microsoft.Win32;

namespace GTAVInjector
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<DllEntry> DllEntries { get; set; }
        private System.Windows.Threading.DispatcherTimer? _gameCheckTimer;
        private System.Windows.Threading.DispatcherTimer? _autoInjectTimer;

        public MainWindow()
        {
            InitializeComponent();
            DllEntries = new ObservableCollection<DllEntry>();
            DllListView.ItemsSource = DllEntries;
            
            LoadSettings();
            InitializeTimers();
            CheckForUpdates();
            StartVersionMonitoring();
            
            // Mover la llamada a UpdateUI() al evento Loaded para asegurar que los controles estén inicializados
            Loaded += (s, e) => 
            {
                UpdateUI();
                UpdateVersionText();
            };
        }

        private void LoadSettings()
        {
            var settings = SettingsManager.Settings;
            
            // Cargar tipo de juego
            if (settings.GameType == GameType.Legacy)
                LegacyRadio.IsChecked = true;
            else
                EnhancedRadio.IsChecked = true;
            
            // Cargar launcher
            switch (settings.LauncherType)
            {
                case LauncherType.Rockstar:
                    RockstarRadio.IsChecked = true;
                    break;
                case LauncherType.EpicGames:
                    EpicRadio.IsChecked = true;
                    break;
                case LauncherType.Steam:
                    SteamRadio.IsChecked = true;
                    break;
            }
            
            // Cargar DLLs
            foreach (var dll in settings.DllEntries)
            {
                DllEntries.Add(dll);
            }
            
            // Cargar auto-inject
            AutoInjectCheckbox.IsChecked = settings.AutoInject;
            
            // Cargar idioma
            var langTag = settings.Language;
            foreach (System.Windows.Controls.ComboBoxItem item in LanguageSelector.Items)
            {
                if (item.Tag?.ToString() == langTag)
                {
                    LanguageSelector.SelectedItem = item;
                    break;
                }
            }
        }

        private void InitializeTimers()
        {
            // Timer para verificar estado del juego
            _gameCheckTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _gameCheckTimer.Tick += (s, e) => UpdateGameStatus();
            _gameCheckTimer.Start();
            
            // Timer para auto-inyección
            _autoInjectTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _autoInjectTimer.Tick += AutoInjectTimer_Tick;
        }

        private async void CheckForUpdates()
        {
            try
            {
                VersionStatusText.Text = LocalizationManager.GetString("CheckingUpdates");
                var updateAvailable = await VersionChecker.CheckForUpdatesAsync();
                
                UpdateVersionStatus(updateAvailable);
            }
            catch
            {
                VersionStatusText.Text = LocalizationManager.GetString("UpdateCheckFailed");
                VersionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void UpdateVersionText()
        {
            if (VersionText != null)
            {
                VersionText.Text = $"v{VersionChecker.GetCurrentVersion()}";
            }
        }

        private void UpdateVersionStatus(bool isOutdated)
        {
            if (isOutdated)
            {
                VersionStatusText.Text = $"DESACTUALIZADO - v{VersionChecker.GetCurrentVersion()} → v{VersionChecker.GetLatestVersion()}";
                VersionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                UpdateButton.Visibility = Visibility.Visible;
                UpdateButton.Content = "Ir a Discord para Actualizar";
                
                // DESHABILITAR LOS 3 BOTONES PRINCIPALES
                LaunchButton.IsEnabled = false;
                InjectButton.IsEnabled = false;
                KillButton.IsEnabled = false;
            }
            else
            {
                VersionStatusText.Text = $"ACTUALIZADO - v{VersionChecker.GetCurrentVersion()}";
                VersionStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                UpdateButton.Visibility = Visibility.Collapsed;
                
                // Rehabilitar botones según estado del juego
                UpdateGameStatus();
            }
        }

        private async void StartVersionMonitoring()
        {
            await VersionChecker.StartVersionMonitoring(isOutdated =>
            {
                Dispatcher.Invoke(() => UpdateVersionStatus(isOutdated));
            });
        }

        private void UpdateGameStatus()
        {
            bool isRunning = InjectionManager.IsGameRunning();
            bool isOutdated = VersionChecker.IsOutdated();
            
            if (isRunning)
            {
                GameStatusText.Text = LocalizationManager.GetString("GameRunning");
                GameStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                
                // Solo habilitar botones si no está desactualizado
                LaunchButton.IsEnabled = false;
                InjectButton.IsEnabled = !isOutdated;
                KillButton.IsEnabled = !isOutdated;
            }
            else
            {
                GameStatusText.Text = LocalizationManager.GetString("GameNotRunning");
                GameStatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                // Solo habilitar botones si no está desactualizado
                LaunchButton.IsEnabled = !isOutdated;
                InjectButton.IsEnabled = false;
                KillButton.IsEnabled = false;
                
                // Resetear estados de inyección
                foreach (var dll in DllEntries)
                {
                    dll.Status = LocalizationManager.GetString("NotInjected");
                }
            }
        }

        private async void AutoInjectTimer_Tick(object? sender, EventArgs e)
        {
            if (!SettingsManager.Settings.AutoInject)
                return;
            
            if (!InjectionManager.IsGameRunning())
                return;
            
            // Verificar si hay DLLs no inyectadas
            var notInjected = DllEntries.Where(d => d.Enabled && 
                d.Status == LocalizationManager.GetString("NotInjected")).ToList();
            
            if (notInjected.Any())
            {
                StatusText.Text = LocalizationManager.GetString("AutoInjecting");
                await Task.Delay(2000); // Esperar a que el juego cargue completamente
                await InjectDllsAsync();
            }
        }

        private void UpdateUI()
        {
            try
            {
                // Verificar que los controles no sean nulos antes de acceder a ellos
                if (DllListTitle != null) DllListTitle.Text = LocalizationManager.GetString("DllList");
                if (AddDllButton != null) AddDllButton.Content = LocalizationManager.GetString("AddDll");
                if (AutoInjectCheckbox != null) AutoInjectCheckbox.Content = LocalizationManager.GetString("AutoInject");
                if (LaunchButton != null) LaunchButton.Content = LocalizationManager.GetString("LaunchGame");
                if (InjectButton != null) InjectButton.Content = LocalizationManager.GetString("InjectDlls");
                if (KillButton != null) KillButton.Content = LocalizationManager.GetString("KillGame");
                if (GameTypeTitle != null) GameTypeTitle.Text = LocalizationManager.GetString("GameType");
                if (LauncherTitle != null) LauncherTitle.Text = LocalizationManager.GetString("Launcher");
                if (RequirementsTitle != null) RequirementsTitle.Text = LocalizationManager.GetString("Requirements");
                if (VersionStatusTitle != null) VersionStatusTitle.Text = LocalizationManager.GetString("VersionStatus");
                if (UpdateButton != null) UpdateButton.Content = LocalizationManager.GetString("UpdateAvailable");
            }
            catch (Exception ex)
            {
                // Registrar el error para depuración
                System.Diagnostics.Debug.WriteLine($"Error en UpdateUI: {ex.Message}");
            }
        }

        private void AddDll_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*",
                Multiselect = true,
                Title = LocalizationManager.GetString("SelectDlls")
            };
            
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (!DllEntries.Any(d => d.Path == file))
                    {
                        DllEntries.Add(new DllEntry
                        {
                            Path = file,
                            FileName = System.IO.Path.GetFileName(file),
                            Enabled = true,
                            Status = LocalizationManager.GetString("NotInjected")
                        });
                    }
                }
                
                SettingsManager.Settings.DllEntries = DllEntries.ToList();
                SettingsManager.SaveSettings();
            }
        }

        private void RemoveDll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DllEntry dll)
            {
                DllEntries.Remove(dll);
                SettingsManager.Settings.DllEntries = DllEntries.ToList();
                SettingsManager.SaveSettings();
            }
        }

        private async void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = LocalizationManager.GetString("LaunchingGame");
                InjectionManager.LaunchGame();
                await Task.Delay(1000);
                StatusText.Text = LocalizationManager.GetString("GameLaunched");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = LocalizationManager.GetString("LaunchFailed");
            }
        }

        private async void InjectDlls_Click(object sender, RoutedEventArgs e)
        {
            await InjectDllsAsync();
        }

        private async Task InjectDllsAsync()
        {
            try
            {
                var enabledDlls = DllEntries.Where(d => d.Enabled).ToList();
                
                if (!enabledDlls.Any())
                {
                    MessageBox.Show(LocalizationManager.GetString("NoDllsEnabled"), 
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                StatusText.Text = LocalizationManager.GetString("Injecting");
                
                int injected = 0;
                foreach (var dll in enabledDlls)
                {
                    var result = await Task.Run(() => InjectionManager.InjectDll(dll.Path));
                    
                    switch (result)
                    {
                        case InjectionResult.INJECT_OK:
                            MostrarEstado("Estado: TSV2 Cargado exitosamente.", "Inyectado", System.Windows.Media.Brushes.LimeGreen);
                            dll.Status = "Inyectado exitosamente";
                            injected++;
                            break;
                        case InjectionResult.ERROR_OPEN_PROCESS:
                            MostrarEstado("Estado: No se pudo abrir el proceso de GTA5.", "No inyectado.", System.Windows.Media.Brushes.Red);
                            dll.Status = "Error: No se pudo abrir GTA5";
                            break;
                        case InjectionResult.ERROR_DLL_NOTFOUND:
                            MostrarEstado("Estado: No se encontró TessioScriptV2.dll.", "No inyectado.", System.Windows.Media.Brushes.Red);
                            dll.Status = "Error: DLL no encontrada";
                            break;
                        case InjectionResult.ERROR_ALLOC:
                            MostrarEstado("Estado: No se pudo asignar memoria remota (¿Battleye activado?).", "No inyectado.", System.Windows.Media.Brushes.Red);
                            dll.Status = "Error: Memoria no asignada";
                            break;
                        case InjectionResult.ERROR_WRITE:
                            MostrarEstado("Estado: Fallo al escribir en la memoria del juego.", "No inyectado.", System.Windows.Media.Brushes.Red);
                            dll.Status = "Error: Escritura fallida";
                            break;
                        case InjectionResult.ERROR_CREATE_THREAD:
                            MostrarEstado("Estado: No se pudo ejecutar el hilo remoto.", "No inyectado.", System.Windows.Media.Brushes.Red);
                            dll.Status = "Error: Hilo remoto fallido";
                            break;
                        default:
                            MostrarEstado("Fallo en la inyección.", "Error.", System.Windows.Media.Brushes.Yellow);
                            dll.Status = "Error: Fallo desconocido";
                            break;
                    }
                }
                
                StatusText.Text = $"Inyección completada: ({injected}/{enabledDlls.Count})";
                
                if (injected == enabledDlls.Count)
                {
                    MessageBox.Show($"Inyección exitosa: ({injected}/{enabledDlls.Count})", 
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Inyección falló";
            }
        }

        private void MostrarEstado(string mensaje, string estado, System.Windows.Media.Brush color)
        {
            StatusText.Text = mensaje;
            StatusText.Foreground = color;
            
            // También actualizar el estado del juego si es necesario
            GameStatusText.Text = estado;
            GameStatusText.Foreground = color;
        }

        private void KillGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InjectionManager.KillGame();
                StatusText.Text = LocalizationManager.GetString("GameKilled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GameType_Changed(object sender, RoutedEventArgs e)
        {
            if (LegacyRadio.IsChecked == true)
                SettingsManager.Settings.GameType = GameType.Legacy;
            else
                SettingsManager.Settings.GameType = GameType.Enhanced;
            
            SettingsManager.SaveSettings();
        }

        private void Launcher_Changed(object sender, RoutedEventArgs e)
        {
            if (RockstarRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.Rockstar;
            else if (EpicRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.EpicGames;
            else if (SteamRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.Steam;
            
            SettingsManager.SaveSettings();
        }

        private void AutoInject_Changed(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.AutoInject = AutoInjectCheckbox.IsChecked == true;
            SettingsManager.SaveSettings();
            
            if (SettingsManager.Settings.AutoInject)
                _autoInjectTimer?.Start();
            else
                _autoInjectTimer?.Stop();
        }

        private void LanguageSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LanguageSelector.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                var lang = item.Tag?.ToString() ?? "en";
                LocalizationManager.SetLanguage(lang);
                SettingsManager.Settings.Language = lang;
                SettingsManager.SaveSettings();
                UpdateUI();
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Tu versión está desactualizada. ¿Quieres ir al Discord de TessioScript para obtener la actualización?",
                    "Actualización Disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    VersionChecker.OpenDiscordUpdate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Discord: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Solo permitir arrastrar la ventana, no maximizar
            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Función de maximizar removida - ya no es necesaria

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://discord.gg/NH6pArJB",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Discord: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TikTok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.tiktok.com/@tessiogg",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir TikTok: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Twitch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.twitch.tv/tessiogg",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Twitch: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void YouTube_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.youtube.com/@TessioScript",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir YouTube: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameCheckTimer?.Stop();
            _autoInjectTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
