using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GTAVInjector.Core;
using GTAVInjector.Models;
using Microsoft.Win32;
using System.Net.Http;
using System.Windows.Threading;

namespace GTAVInjector
{
    public partial class MainWindow : Window
    {
        private const string TESSIO_DISCORD_URL = "https://gtaggs.wirdland.xyz/discord";

        public ObservableCollection<DllEntry> DllEntries { get; set; }
        private System.Windows.Threading.DispatcherTimer? _gameCheckTimer;
        private System.Windows.Threading.DispatcherTimer? _autoInjectTimer;
        private bool _gameWasRunning = false;
        private bool _autoInjectionCompleted = false;
        private bool _isLoadingSettings = false; // Bandera para evitar guardado durante carga

        private readonly DispatcherTimer versionCheckTimer = new DispatcherTimer();
        // private string currentLocalVersion = "1.0.7"; // Variable no utilizada - comentada para evitar warning
        private readonly DispatcherTimer _httpVersionTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            // ✅ VALIDACIÓN INICIAL ÚNICA (sin timers repetitivos)
            _ = PerformInitialVersionCheckAsync();



            DllEntries = new ObservableCollection<DllEntry>();
            DllListView.ItemsSource = DllEntries;

            LoadSettings();
            InitializeTimers();


            // Mover la llamada a UpdateUI() al evento Loaded para asegurar que los controles estén inicializados
            Loaded += (s, e) =>
            {
                UpdateUI();
                // Delay para asegurar que la UI esté completamente renderizada
                this.Dispatcher.BeginInvoke(new Action(() => {
                    StartParallaxAnimation();
                    // Desactivar bandera DESPUÉS de que todo esté completamente cargado
                    _isLoadingSettings = false;
                    System.Diagnostics.Debug.WriteLine("[LOADING] Bandera _isLoadingSettings desactivada - eventos habilitados");
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        /// <summary>
        /// 🚀 VALIDACIÓN INICIAL ÚNICA - Se ejecuta solo al iniciar
        /// </summary>
        private async Task PerformInitialVersionCheckAsync()
        {
            // Mostrar estado de carga inicial
            Dispatcher.Invoke(() =>
            {
                VersionStatusText.Text = "🔄 Validando versión...";
                VersionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                
                // Mantener funcionalidad básica habilitada
                LaunchButton.IsEnabled = true;
                InjectButton.IsEnabled = true;
                KillButton.IsEnabled = true;
            });

            try
            {
                var validator = new VersionValidator();
                var info = await validator.ValidateVersionSilentAsync();
                
                Dispatcher.Invoke(() =>
                {
                    HandleVersionValidationResult(info);
                });
                
                System.Diagnostics.Debug.WriteLine($"✅ Validación inicial completada: {info.ErrorType} - {info.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Validación inicial falló: {ex.Message}");
                
                Dispatcher.Invoke(() =>
                {
                    // 🚀 MODO OFFLINE: Permitir funcionamiento completo
                    VersionStatusText.Text = "🔌 Modo offline - funcionamiento local";
                    VersionStatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                    EnableFullFunctionality();
                });
            }
        }

        // 🚀 SISTEMA DE VALIDACIÓN SIMPLIFICADO

        /// <summary>
        /// 🎯 MANEJO INTELIGENTE DE DIFERENTES ESCENARIOS DE VERSIONES
        /// </summary>
        private void HandleVersionValidationResult(VersionValidationInfo info)
        {
            switch (info.ErrorType)
            {
                case ValidationErrorType.None:
                    // ✅ VERSIONES IGUALES: Todo perfecto
                    VersionStatusText.Text = $"✅ Versión válida v{info.ClientVersion}";
                    VersionStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    EnableFullFunctionality();
                    break;

                case ValidationErrorType.VersionMismatch:
                    if (info.IsClientOutdated)
                    {
                        // ❌ CLIENTE DESACTUALIZADO: Funcionalidad limitada
                        var versionGap = CalculateVersionGap(info.ClientVersion, info.ServerVersion);
                        
                        if (versionGap <= 2) // Diferencia menor: Permitir con advertencia
                        {
                            VersionStatusText.Text = $"⚠️ DESACTUALIZADO (menor) v{info.ClientVersion} → v{info.ServerVersion}";
                            VersionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                            
                            // 🚀 PERMITIR FUNCIONAMIENTO CON ADVERTENCIA
                            EnableFullFunctionality();
                            ShowUpdateNotification(info.ServerVersion, false); // No crítico
                        }
                        else // Diferencia mayor: Bloquear funciones críticas
                        {
                            VersionStatusText.Text = $"❌ DESACTUALIZADO (crítico) v{info.ClientVersion} → v{info.ServerVersion}";
                            VersionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                            
                            // 🚫 BLOQUEAR FUNCIONES CRÍTICAS
                            LaunchButton.IsEnabled = false;
                            InjectButton.IsEnabled = false;
                            KillButton.IsEnabled = false;
                            
                            ShowUpdateNotification(info.ServerVersion, true); // Crítico
                        }
                    }
                    else if (info.IsClientNewer)
                    {
                        // 🆕 CLIENTE MÁS NUEVO: Permitir funcionamiento (usuario avanzado)
                        VersionStatusText.Text = $"🚀 Cliente avanzado v{info.ClientVersion} > v{info.ServerVersion}";
                        VersionStatusText.Foreground = System.Windows.Media.Brushes.Cyan;
                        EnableFullFunctionality();
                    }
                    else
                    {
                        // ⚠️ VERSIONES DIFERENTES PERO MISMA NUMERACIÓN
                        VersionStatusText.Text = $"⚠️ Versión diferente detectada - verificar manualmente";
                        VersionStatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                        EnableFullFunctionality();
                    }
                    break;

                case ValidationErrorType.ConnectionError:
                case ValidationErrorType.Timeout:
                    // 🔌 SERVIDOR NO DISPONIBLE: Modo offline completo
                    VersionStatusText.Text = $"🔌 Modo offline v{info.ClientVersion} - servidor no disponible";
                    VersionStatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                    EnableFullFunctionality();
                    break;

                case ValidationErrorType.ServerError:
                    // 🔧 ERROR DEL SERVIDOR: Permitir funcionamiento local
                    VersionStatusText.Text = $"🔧 Error del servidor - usando validación local v{info.ClientVersion}";
                    VersionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    EnableFullFunctionality();
                    break;

                default:
                    // ❓ ERROR DESCONOCIDO: Modo conservador
                    VersionStatusText.Text = $"❓ Estado incierto v{info.ClientVersion} - verificar conexión";
                    VersionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                    EnableFullFunctionality(); // Permitir funcionamiento por defecto
                    break;
            }
        }

        /// <summary>
        /// 🚀 HABILITAR FUNCIONALIDAD COMPLETA
        /// </summary>
        private void EnableFullFunctionality()
        {
            LaunchButton.IsEnabled = true;
            InjectButton.IsEnabled = InjectionManager.IsGameRunning();
            KillButton.IsEnabled = InjectionManager.IsGameRunning();
            
            UpdateButton.Visibility = Visibility.Collapsed;
            ChangelogButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 📊 CALCULAR DIFERENCIA ENTRE VERSIONES (para determinar criticidad)
        /// </summary>
        private int CalculateVersionGap(string clientVersion, string serverVersion)
        {
            try
            {
                var clientParts = clientVersion.Split('.').Select(int.Parse).ToArray();
                var serverParts = serverVersion.Split('.').Select(int.Parse).ToArray();
                
                // Calcular diferencia en versión principal
                int majorDiff = Math.Abs((serverParts.ElementAtOrDefault(0)) - (clientParts.ElementAtOrDefault(0)));
                int minorDiff = Math.Abs((serverParts.ElementAtOrDefault(1)) - (clientParts.ElementAtOrDefault(1)));
                int patchDiff = Math.Abs((serverParts.ElementAtOrDefault(2)) - (clientParts.ElementAtOrDefault(2)));
                
                // Devolver la diferencia más significativa
                if (majorDiff > 0) return majorDiff * 100; // Diferencia mayor es crítica
                if (minorDiff > 0) return minorDiff * 10;  // Diferencia menor es importante
                return patchDiff; // Diferencia de patch es menor
            }
            catch
            {
                return 0; // Si hay error, asumir compatibilidad
            }
        }

        /// <summary>
        /// 🔔 MOSTRAR NOTIFICACIÓN DE ACTUALIZACIÓN
        /// </summary>
        private void ShowUpdateNotification(string newVersion, bool isCritical)
        {
            if (isCritical)
            {
                UpdateButton.Visibility = Visibility.Visible;
                UpdateButton.Content = $"🚨 ACTUALIZAR A v{newVersion}";
                ChangelogButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateButton.Visibility = Visibility.Visible;
                UpdateButton.Content = $"⬆️ Actualizar a v{newVersion}";
                ChangelogButton.Visibility = Visibility.Visible; // Mantener ambos visibles
            }
        }


        private void LoadSettings()
        {
            _isLoadingSettings = true; // Activar bandera para evitar guardado
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

            // Iniciar timer de auto-inject si está habilitado
            if (settings.AutoInject)
            {
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] Habilitado en configuración - iniciando timer");
                _autoInjectionCompleted = false; // Resetear estado al cargar
                _autoInjectTimer?.Start();
            }

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
                Interval = TimeSpan.FromSeconds(2) // Reducir intervalo para mejor responsividad
            };
            _autoInjectTimer.Tick += AutoInjectTimer_Tick;

            // Iniciar timer si auto-inject ya está habilitado
            if (SettingsManager.Settings.AutoInject)
            {
                _autoInjectTimer.Start();
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] Timer iniciado en InitializeTimers");
            }
        }

        private void UpdateGameStatus()
        {
            bool isRunning = InjectionManager.IsGameRunning();

            if (isRunning)
            {
                GameStatusText.Text = LocalizationManager.GetString("GameRunning");
                GameStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;

                // ✅ MANTENER BOTÓN DE LANZAR HABILITADO (el usuario puede querer lanzar otra instancia)
                // LaunchButton.IsEnabled = false; // ← REMOVIDO
                
                // Habilitar botones de juego activo
                InjectButton.IsEnabled = true;
                KillButton.IsEnabled = true;


                // Si el juego no estaba corriendo antes y ahora sí, resetear auto-inject
                if (!_gameWasRunning)
                {
                    _autoInjectionCompleted = false;
                    System.Diagnostics.Debug.WriteLine("Juego iniciado - Estado de auto-inyección reseteado para nueva sesión");
                }

                _gameWasRunning = true;
            }
            else
            {
                GameStatusText.Text = LocalizationManager.GetString("GameNotRunning");
                GameStatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                // ✅ MANTENER FUNCIONALIDAD HABILITADA CUANDO NO HAY JUEGO
                LaunchButton.IsEnabled = true;
                InjectButton.IsEnabled = false; // Solo deshabilitar inyección si no hay juego
                KillButton.IsEnabled = false;

                // Si el juego estaba ejecutándose antes y ahora no, resetear el estado
                if (_gameWasRunning)
                {
                    _autoInjectionCompleted = false;
                    _gameWasRunning = false;

                    // Resetear estados de inyección
                    foreach (var dll in DllEntries)
                    {
                        dll.Status = LocalizationManager.GetString("NotInjected");
                    }

                    // Resetear el texto de estado
                    if (StatusText != null)
                    {
                        var currentLang = LocalizationManager.CurrentLanguage;
                        StatusText.Text = currentLang.ToLower() == "es" ? "Listo" : "Ready";
                        StatusText.Foreground = System.Windows.Media.Brushes.White;
                    }

                    System.Diagnostics.Debug.WriteLine("Juego cerrado - Estado de auto-inyección reseteado");
                }
            }
        }

        private async void AutoInjectTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 🔍 VERIFICACIONES BÁSICAS
                bool gameRunning = InjectionManager.IsGameRunning();
                bool autoInjectEnabled = SettingsManager.Settings.AutoInject;
                
                System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] 🔄 Tick - Habilitado: {autoInjectEnabled}, Juego: {gameRunning}, Completado: {_autoInjectionCompleted}");
                
                // Salir si autoinyección está deshabilitada
                if (!autoInjectEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ❌ Deshabilitado - deteniendo timer");
                    _autoInjectTimer?.Stop();
                    return;
                }
                
                // Si no hay juego ejecutándose, resetear estado y esperar
                if (!gameRunning)
                {
                    if (_gameWasRunning)
                    {
                        // El juego se cerró, resetear estados
                        _autoInjectionCompleted = false;
                        _gameWasRunning = false;
                        System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] 🔄 Juego cerrado - estado reseteado");
                    }
                    return;
                }
                
                // 🎯 VERIFICAR DLLs DISPONIBLES
                var enabledDlls = DllEntries.Where(d => d.Enabled).ToList();
                if (!enabledDlls.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ⚠️ No hay DLLs habilitadas para inyectar");
                    return;
                }
                
                // 🔍 VERIFICAR ESTADO DE INYECCIÓN
                var notInjectedText = LocalizationManager.GetString("NotInjected");
                var notInjected = enabledDlls.Where(d => 
                    string.IsNullOrEmpty(d.Status) ||
                    d.Status == notInjectedText ||
                    d.Status.StartsWith("Error:")).ToList();
                
                System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] 📊 DLLs habilitadas: {enabledDlls.Count}, Pendientes: {notInjected.Count}");
                
                // Si hay DLLs no inyectadas, intentar inyectar
                if (notInjected.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] 🎯 Iniciando inyección automática...");
                    StatusText.Text = LocalizationManager.GetString("AutoInjecting");

                    // Esperar a que el juego cargue completamente
                    await Task.Delay(2000);

                    // Solo inyectar si el juego sigue ejecutándose después del delay
                    if (InjectionManager.IsGameRunning())
                    {
                        await InjectDllsAsync();

                        // Verificar resultados después de la inyección
                        var stillNotInjected = enabledDlls.Where(d =>
                            d.Status == LocalizationManager.GetString("NotInjected") ||
                            d.Status.StartsWith("Error:")).ToList();

                        if (!stillNotInjected.Any())
                        {
                            _autoInjectionCompleted = true;
                            System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ✅ Todas las DLLs inyectadas exitosamente");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] ⚠️ {stillNotInjected.Count} DLLs aún no inyectadas, reintentará en próximo ciclo");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] Juego cerrado durante el delay - cancelando inyección");
                    }
                }
                else
                {
                    if (!_autoInjectionCompleted)
                    {
                        _autoInjectionCompleted = true;
                        System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ✅ Todas las DLLs ya inyectadas - completado");
                        
                        // Mostrar mensaje de éxito
                        Dispatcher.Invoke(() =>
                        {
                            var currentLang = LocalizationManager.CurrentLanguage;
                            StatusText.Text = currentLang.ToLower() == "es" ? 
                                "🚀 Auto-inyección completada" : "🚀 Auto-injection completed";
                            StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                        });
                    }
                    return;
                }
                
                // 🚀 EJECUTAR INYECCIÓN AUTOMÁTICA
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] 🎯 Iniciando inyección automática...");
                
                // Actualizar UI
                Dispatcher.Invoke(() =>
                {
                    var currentLang = LocalizationManager.CurrentLanguage;
                    StatusText.Text = currentLang.ToLower() == "es" ? 
                        "🔄 Auto-inyectando..." : "🔄 Auto-injecting...";
                    StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                });
                
                // Esperar a que el juego esté completamente cargado
                await Task.Delay(3000);
                
                // Verificar nuevamente que el juego sigue ejecutándose
                if (!InjectionManager.IsGameRunning())
                {
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ⚠️ Juego cerrado durante la espera - cancelando");
                    return;
                }
                
                // 💉 EJECUTAR INYECCIÓN
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] 💉 Ejecutando inyección de DLLs...");
                await InjectDllsAsync();
                
                // 📊 VERIFICAR RESULTADOS
                var finalCheck = enabledDlls.Where(d => 
                    string.IsNullOrEmpty(d.Status) ||
                    d.Status == notInjectedText ||
                    d.Status.StartsWith("Error:")).ToList();
                
                if (!finalCheck.Any())
                {
                    _autoInjectionCompleted = true;
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ✅ ¡ÉXITO! Todas las DLLs inyectadas correctamente");
                    
                    // Mostrar éxito en UI
                    Dispatcher.Invoke(() =>
                    {
                        var currentLang = LocalizationManager.CurrentLanguage;
                        StatusText.Text = currentLang.ToLower() == "es" ? 
                            "✅ Auto-inyección exitosa" : "✅ Auto-injection successful";
                        StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] ⚠️ {finalCheck.Count} DLLs fallaron - reintentará en próximo ciclo");
                    
                    // Mostrar estado de reintento
                    Dispatcher.Invoke(() =>
                    {
                        var currentLang = LocalizationManager.CurrentLanguage;
                        StatusText.Text = currentLang.ToLower() == "es" ? 
                            $"⚠️ {finalCheck.Count} DLLs fallaron - reintentando..." : 
                            $"⚠️ {finalCheck.Count} DLLs failed - retrying...";
                        StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] ❌ ERROR CRÍTICO: {ex.Message}");
                
                // Mostrar error en UI
                Dispatcher.Invoke(() =>
                {
                    var currentLang = LocalizationManager.CurrentLanguage;
                    StatusText.Text = currentLang.ToLower() == "es" ? 
                        "❌ Error en auto-inyección" : "❌ Auto-injection error";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                });
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
                if (DevsTitle != null) DevsTitle.Text = LocalizationManager.GetString("Devs");
                if (VersionStatusTitle != null) VersionStatusTitle.Text = LocalizationManager.GetString("VersionStatus");
                if (UpdateButton != null) UpdateButton.Content = LocalizationManager.GetString("UpdateAvailable");
                if (ChangelogButton != null) ChangelogButton.Content = LocalizationManager.GetString("ViewChangelog");


                // Actualizar textos de requisitos
                if (VcRequirementText != null) VcRequirementText.Text = LocalizationManager.GetString("VcRequirement");
                if (GtaRequirementText != null) GtaRequirementText.Text = LocalizationManager.GetString("GtaRequirement");
                if (AdminRequirementText != null) AdminRequirementText.Text = LocalizationManager.GetString("AdminRequirement");

                // Actualizar texto "Idioma"
                if (LanguageLabel != null) LanguageLabel.Text = LocalizationManager.GetString("Language");

                // Actualizar botones "Remove" en la lista de DLLs
                UpdateRemoveButtonsText();

                // Actualizar StatusText según idioma
                var currentLang = LocalizationManager.CurrentLanguage;
                if (currentLang.ToLower() == "es")
                {
                    if (StatusText != null && (StatusText.Text == "Ready" || StatusText.Text == "Listo"))
                        StatusText.Text = "Listo";
                }
                else
                {
                    if (StatusText != null && (StatusText.Text == "Listo" || StatusText.Text == "Ready"))
                        StatusText.Text = "Ready";
                }
            }
            catch (Exception ex)
            {
                // Registrar el error para depuración
                System.Diagnostics.Debug.WriteLine($"Error en UpdateUI: {ex.Message}");
            }
        }

        private void UpdateRemoveButtonsText()
        {
            try
            {
                // Obtener idioma directamente del ComboBox seleccionado
                string currentLang = "en";
                if (LanguageSelector?.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
                {
                    currentLang = selectedItem.Tag?.ToString() ?? "en";
                }

                var removeText = currentLang.ToLower() == "es" ? "Quitar" : "Remove";

                System.Diagnostics.Debug.WriteLine($"Idioma detectado: {currentLang}, Texto del botón: {removeText}");

                // Forzar regeneración completa del ListView
                if (DllListView != null && DllListView.ItemsSource != null)
                {
                    var items = DllListView.ItemsSource;
                    DllListView.ItemsSource = null;

                    // Actualizar el texto por defecto en el XAML
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        DllListView.ItemsSource = items;

                        // Esperar a que se regeneren los items y luego actualizar
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateVisualRemoveButtons(removeText);
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en UpdateRemoveButtonsText: {ex.Message}");
            }
        }

        private void UpdateVisualRemoveButtons(string text)
        {
            try
            {
                if (DllListView == null) return;

                for (int i = 0; i < DllListView.Items.Count; i++)
                {
                    var container = DllListView.ItemContainerGenerator.ContainerFromIndex(i) as System.Windows.Controls.ListViewItem;
                    if (container != null)
                    {
                        var textBlock = FindVisualChild<System.Windows.Controls.TextBlock>(container, "RemoveButtonText");
                        if (textBlock != null)
                        {
                            textBlock.Text = text;
                            System.Diagnostics.Debug.WriteLine($"Botón {i} actualizado a: {text}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en UpdateVisualRemoveButtons: {ex.Message}");
            }
        }

        private T? FindVisualChild<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.DependencyObject
        {
            try
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild && (child as System.Windows.FrameworkElement)?.Name == name)
                    {
                        return typedChild;
                    }

                    var result = FindVisualChild<T>(child, name);
                    if (result != null)
                        return result;
                }
            }
            catch
            {
                // Ignorar errores de búsqueda visual
            }

            return null;
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
                if (!_isLoadingSettings) // Solo guardar si no estamos cargando
                    SettingsManager.SaveSettings();
            }
        }

        private void RemoveDll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DllEntry dll)
            {
                DllEntries.Remove(dll);
                SettingsManager.Settings.DllEntries = DllEntries.ToList();
                if (!_isLoadingSettings) // Solo guardar si no estamos cargando
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
                            dll.Status = "Inyectado";
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

                // Después de un pequeño delay, resetear el texto de estado
                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var currentLang = LocalizationManager.CurrentLanguage;
                        StatusText.Text = currentLang.ToLower() == "es" ? "Listo" : "Ready";
                        StatusText.Foreground = System.Windows.Media.Brushes.White;
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoInject_Changed(object sender, RoutedEventArgs e)
        {
            // Evitar guardar configuración durante la carga inicial
            if (_isLoadingSettings) 
            {
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] Cambio ignorado durante carga inicial");
                return;
            }

            bool isEnabled = AutoInjectCheckbox.IsChecked == true;
            
            System.Diagnostics.Debug.WriteLine($"[AUTO-INJECT] Checkbox cambió a: {isEnabled}");
            
            // Actualizar configuración
            SettingsManager.Settings.AutoInject = isEnabled;
            SettingsManager.SaveSettings();
            
            // Controlar el timer de auto-inyección
            if (isEnabled)
            {
                // Activar auto-inyección
                _autoInjectionCompleted = false; // Resetear estado
                _autoInjectTimer?.Start();
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ✅ ACTIVADO - Timer iniciado");
                
                // Si el juego ya está ejecutándose, intentar inyectar inmediatamente
                if (InjectionManager.IsGameRunning())
                {
                    System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] Juego detectado - iniciando inyección inmediata");
                    Task.Run(async () => {
                        await Task.Delay(1000); // Pequeño delay
                        Dispatcher.Invoke(() => AutoInjectTimer_Tick(null, EventArgs.Empty));
                    });
                }
            }
            else
            {
                // Desactivar auto-inyección
                _autoInjectTimer?.Stop();
                _autoInjectionCompleted = false;
                System.Diagnostics.Debug.WriteLine("[AUTO-INJECT] ❌ DESACTIVADO - Timer detenido");
            }
        }

        private void GameType_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings) return; // No guardar durante la carga inicial

            if (LegacyRadio.IsChecked == true)
                SettingsManager.Settings.GameType = GameType.Legacy;
            else
                SettingsManager.Settings.GameType = GameType.Enhanced;

            SettingsManager.SaveSettings();
        }

        private void Launcher_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
            {
                System.Diagnostics.Debug.WriteLine("[EVENT DEBUG] Launcher_Changed bloqueado por _isLoadingSettings");
                return; // No guardar durante la carga inicial
            }
            System.Diagnostics.Debug.WriteLine("[EVENT DEBUG] Launcher_Changed ejecutándose - bandera desactivada");

            if (RockstarRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.Rockstar;
            else if (EpicRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.EpicGames;
            else if (SteamRadio.IsChecked == true)
                SettingsManager.Settings.LauncherType = LauncherType.Steam;

            SettingsManager.SaveSettings();
        }


        private void LanguageSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings) return; // No guardar durante la carga inicial

            if (LanguageSelector.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                var lang = item.Tag?.ToString() ?? "en";
                LocalizationManager.SetLanguage(lang);
                SettingsManager.Settings.Language = lang;
                SettingsManager.SaveSettings();
                
                // Forzar actualización completa
                UpdateUI();
                
                // Forzar actualización específica de botones Remove después de un pequeño delay
                Dispatcher.BeginInvoke(new Action(() => {
                    UpdateRemoveButtonsText();
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                // Actualizar texto de StatusText según idioma (verificar que no sea null)
                if (StatusText != null)
                {
                    if (lang.ToLower() == "es")
                    {
                        if (StatusText.Text == "Ready" || StatusText.Text == "Listo")
                            StatusText.Text = "Listo";
                    }
                    else
                    {
                        if (StatusText.Text == "Listo" || StatusText.Text == "Ready")
                            StatusText.Text = "Ready";
                    }
                }
            }
        }

        private void ComboBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Abrir el dropdown del ComboBox cuando se hace clic en cualquier parte
            if (LanguageSelector != null && e.LeftButton == MouseButtonState.Pressed)
            {
                LanguageSelector.IsDropDownOpen = !LanguageSelector.IsDropDownOpen;
                e.Handled = true; // Evitar que el evento se propague
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            VersionChecker.OpenDiscordUpdate();
        }

        private void Changelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Tessio/TessioScript-Launcher/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir changelog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ✨ NUEVO MÉTODO PARA VERIFICAR ACTUALIZACIONES MANUALMENTE ✨
        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Deshabilitar el botón mientras se verifica (si existe)
                // CheckUpdatesButton puede no existir en esta versión del XAML

                // Actualizar estado de la interfaz
                var checkingStatusText = LocalizationManager.CurrentLanguage.ToLower() == "es" ? 
                    "🌐 Verificando versión desde internet..." : 
                    "🌐 Checking version from internet...";
                VersionStatusText.Text = checkingStatusText;
                VersionStatusText.Foreground = System.Windows.Media.Brushes.Yellow;

                // 🔄 USAR SISTEMA HTTP LOCAL EN LUGAR DE GITHUB
                // bool isOutdated = await VersionChecker.ForceCheckForUpdatesAsync(); // DESHABILITADO
                
                // 🚀 VERIFICAR USANDO VALIDADOR HTTP LOCAL
                var validator = new VersionValidator();
                var info = await validator.ValidateVersionSilentAsync();
                bool isOutdated = !info.IsValid && info.ErrorType == ValidationErrorType.VersionMismatch && info.IsClientOutdated;
                
                // 🚀 OBTENER INFORMACIÓN DEL SERVIDOR HTTP LOCAL
                // var versionInfo = VersionChecker.GetVersionInfo(); // DESHABILITADO
                var serverInfo = await validator.GetServerInfoAsync();
                
                // Actualizar interfaz con resultado
                // UpdateVersionStatus(isOutdated); // Método no disponible en esta versión

                // Mostrar mensaje informativo localizado
                string message;
                string title;
                MessageBoxImage icon;

                var isSpanish = LocalizationManager.CurrentLanguage.ToLower() == "es";

                // Usar información de la validación en lugar de versionInfo
                string currentVersion = info.ClientVersion ?? "1.0.7";
                string serverVersion = info.ServerVersion ?? (serverInfo?.version ?? "Unknown");

                if (isOutdated)
                {
                    if (isSpanish)
                    {
                        message = $"🆕 ¡Nueva versión disponible!\n\n" +
                                 $"📱 Versión actual: v{currentVersion}\n" +
                                 $"🔥 Versión nueva: v{serverVersion}\n\n" +
                                 $"Se recomienda actualizar para obtener las últimas mejoras y correcciones.";
                        title = "Actualización Disponible";
                    }
                    else
                    {
                        message = $"🆕 New version available!\n\n" +
                                 $"📱 Current version: v{currentVersion}\n" +
                                 $"🔥 Latest version: v{serverVersion}\n\n" +
                                 $"It's recommended to update to get the latest improvements and fixes.";
                        title = "Update Available";
                    }
                    icon = MessageBoxImage.Information;
                }
                else if (!string.IsNullOrEmpty(serverVersion))
                {
                    if (isSpanish)
                    {
                        message = $"✅ ¡Estás usando la versión más reciente!\n\n" +
                                 $"📱 Versión actual: v{currentVersion}\n" +
                                 $"🌐 Última versión: v{serverVersion}\n\n" +
                                 $"No se requiere actualización.";
                        title = "Versión Actualizada";
                    }
                    else
                    {
                        message = $"✅ You're using the latest version!\n\n" +
                                 $"📱 Current version: v{currentVersion}\n" +
                                 $"🌐 Latest version: v{serverVersion}\n\n" +
                                 $"No update required.";
                        title = "Up to Date";
                    }
                    icon = MessageBoxImage.Information;
                }
                else
                {
                    if (isSpanish)
                    {
                        message = "⚠️ No se pudo verificar la versión.\n\n" +
                                 $"📱 Versión actual: v{currentVersion}\n\n" +
                                 $"Verifica tu conexión a internet e intenta nuevamente.";
                        title = "Error de Verificación";
                    }
                    else
                    {
                        message = "⚠️ Could not verify version.\n\n" +
                                 $"📱 Current version: v{currentVersion}\n\n" +
                                 $"Check your internet connection and try again.";
                        title = "Verification Error";
                    }
                    icon = MessageBoxImage.Warning;
                }

                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                // Error inesperado
                var errorText = LocalizationManager.CurrentLanguage.ToLower() == "es" ? 
                    "❌ Error al verificar versión" : 
                    "❌ Error checking version";
                VersionStatusText.Text = errorText;
                VersionStatusText.Foreground = System.Windows.Media.Brushes.Red;

                var isSpanish = LocalizationManager.CurrentLanguage.ToLower() == "es";
                var errorMessage = isSpanish ? 
                    $"❌ Error inesperado al verificar actualizaciones:\n\n{ex.Message}\n\nIntenta nuevamente más tarde." :
                    $"❌ Unexpected error checking for updates:\n\n{ex.Message}\n\nPlease try again later.";
                var errorTitle = isSpanish ? "Error de Verificación" : "Verification Error";

                MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Error en CheckUpdates_Click: {ex}");
            }
            finally
            {
                // Rehabilitar el botón (si existe)
                // CheckUpdatesButton puede no existir en esta versión del XAML
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

                    FileName = TESSIO_DISCORD_URL,
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

        private void StartParallaxAnimation()
        {
            try
            {
                // Buscar y comenzar la nueva animación de fondo GTA V
                var storyboard = (System.Windows.Media.Animation.Storyboard)FindResource("BackgroundAnimation");
                if (storyboard != null)
                {
                    // Forzar inicio de la animación en este window
                    storyboard.Begin(this, true);
                    System.Diagnostics.Debug.WriteLine("Animación de fondo GTA V iniciada correctamente");

                    // Verificar que los elementos estén visibles
                    if (BackgroundImage != null && ParallaxLayer1 != null && ParallaxLayer2 != null)
                    {
                        BackgroundImage.Visibility = Visibility.Visible;
                        ParallaxLayer1.Visibility = Visibility.Visible;
                        ParallaxLayer2.Visibility = Visibility.Visible;
                        System.Diagnostics.Debug.WriteLine("Elementos de fondo configurados como visibles");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo encontrar la animación BackgroundAnimation");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al iniciar animación de fondo: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameCheckTimer?.Stop();
            _autoInjectTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
