using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GTAVInjector.Models;
using Microsoft.Win32;

namespace GTAVInjector.Core
{
    public enum InjectionResult
    {
        INJECT_OK,
        ERROR_OPEN_PROCESS,
        ERROR_DLL_NOTFOUND,
        ERROR_ALLOC,
        ERROR_WRITE,
        ERROR_CREATE_THREAD,
        ERROR_UNKNOWN
    }

    public static class InjectionManager
    {
        #region Windows API
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_READ = 0x0010;

        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 4;

        #endregion

        public static bool IsGameRunning()
        {
            var processName = SettingsManager.Settings.GameType == GameType.Legacy 
                ? "GTA5" 
                : "GTA5_Enhanced";
            
            return Process.GetProcessesByName(processName).Any();
        }

        public static void LaunchGame()
        {
            var gameType = SettingsManager.Settings.GameType;
            var launcherType = SettingsManager.Settings.LauncherType;

            switch (launcherType)
            {
                case LauncherType.Rockstar:
                    LaunchRockstar(gameType);
                    break;
                case LauncherType.EpicGames:
                    LaunchEpicGames(gameType);
                    break;
                case LauncherType.Steam:
                    LaunchSteam(gameType);
                    break;
            }
        }

        private static void LaunchRockstar(GameType gameType)
        {
            try
            {
                var regKey = gameType == GameType.Legacy
                    ? @"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V"
                    : @"SOFTWARE\WOW6432Node\Rockstar Games\GTAV Enhanced";

                using var key = Registry.LocalMachine.OpenSubKey(regKey);
                var installPath = key?.GetValue("InstallFolder")?.ToString();

                if (string.IsNullOrEmpty(installPath))
                    throw new Exception("GTA V installation path not found in registry");

                var exePath = Path.Combine(installPath, "PlayGTAV.exe");
                
                if (!File.Exists(exePath))
                    throw new Exception($"PlayGTAV.exe not found at: {exePath}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch via Rockstar Launcher: {ex.Message}");
            }
        }

        private static void LaunchEpicGames(GameType gameType)
        {
            var appId = gameType == GameType.Legacy
                ? "9d2d0eb64d5c44529cece33fe2a46482"
                : "8769e24080ea413b8ebca3f1b8c50951";

            var uri = $"com.epicgames.launcher://apps/{appId}?action=launch&silent=true";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }

        private static void LaunchSteam(GameType gameType)
        {
            var appId = gameType == GameType.Legacy ? "271590" : "3240220";
            var uri = $"steam://run/{appId}";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }

        public static void KillGame()
        {
            var processName = SettingsManager.Settings.GameType == GameType.Legacy 
                ? "GTA5" 
                : "GTA5_Enhanced";
            
            var processes = Process.GetProcessesByName(processName);
            
            if (!processes.Any())
                throw new Exception("Game is not running");

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to kill game process: {ex.Message}");
                }
            }
        }

        public static InjectionResult InjectDll(string dllPath)
        {
            // Verificar si la DLL existe
            if (!File.Exists(dllPath))
                return InjectionResult.ERROR_DLL_NOTFOUND;

            var processName = SettingsManager.Settings.GameType == GameType.Legacy 
                ? "GTA5" 
                : "GTA5_Enhanced";
            
            var processes = Process.GetProcessesByName(processName);
            
            // Verificar si el juego está ejecutándose
            if (!processes.Any())
                return InjectionResult.ERROR_OPEN_PROCESS;

            var process = processes.First();
            
            try
            {
                // Crear copia temporal de la DLL
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    "GTAV-Injector",
                    Path.GetFileName(dllPath)
                );

                var tempDir = Path.GetDirectoryName(tempPath);
                if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                File.Copy(dllPath, tempPath, true);

                // Inyectar la DLL
                return InjectDllInternal(process.Id, tempPath);
            }
            catch
            {
                return InjectionResult.ERROR_UNKNOWN;
            }
        }

        private static InjectionResult InjectDllInternal(int processId, string dllPath)
        {
            IntPtr hProcess = IntPtr.Zero;
            IntPtr allocMemAddress = IntPtr.Zero;

            try
            {
                // Abrir el proceso
                hProcess = OpenProcess(
                    PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | 
                    PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                    false, processId);

                if (hProcess == IntPtr.Zero)
                    return InjectionResult.ERROR_OPEN_PROCESS;

                // Obtener la dirección de LoadLibraryA
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                
                if (loadLibraryAddr == IntPtr.Zero)
                    return InjectionResult.ERROR_UNKNOWN;

                // Alocar memoria en el proceso remoto
                allocMemAddress = VirtualAllocEx(
                    hProcess, IntPtr.Zero, 
                    (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))),
                    MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                    return InjectionResult.ERROR_ALLOC;

                // Escribir la ruta de la DLL en la memoria del proceso
                byte[] bytes = Encoding.ASCII.GetBytes(dllPath);
                bool result = WriteProcessMemory(
                    hProcess, allocMemAddress, bytes, 
                    (uint)bytes.Length, out _);

                if (!result)
                    return InjectionResult.ERROR_WRITE;

                // Crear un thread remoto que ejecute LoadLibraryA
                IntPtr hThread = CreateRemoteThread(
                    hProcess, IntPtr.Zero, 0, 
                    loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);

                if (hThread == IntPtr.Zero)
                    return InjectionResult.ERROR_CREATE_THREAD;

                CloseHandle(hThread);
                return InjectionResult.INJECT_OK;
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }
    }
}
