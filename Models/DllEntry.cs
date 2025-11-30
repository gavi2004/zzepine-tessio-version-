using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTAVInjector.Models
{
    public class DllEntry : INotifyPropertyChanged
    {
        private string _path = string.Empty;
        private string _fileName = string.Empty;
        private bool _enabled = true;
        private string _status = "Not Injected";

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum GameType
    {
        Legacy,
        Enhanced
    }

    public enum LauncherType
    {
        Rockstar,
        EpicGames,
        Steam
    }
}
