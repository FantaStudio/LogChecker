using System.ComponentModel;

namespace LogAnalyzer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _folderPath = "";
        private int _minCount = 10;
        private bool _ignoreTimeout;
        private int _errors;
        private int _successes;
        private int _progress;

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
            }
        }

        public int MinCount
        {
            get => _minCount;
            set
            {
                _minCount = value;
                OnPropertyChanged(nameof(MinCount));
            }
        }

        public bool IgnoreTimeout
        {
            get => _ignoreTimeout;
            set
            {
                _ignoreTimeout = value;
                OnPropertyChanged(nameof(IgnoreTimeout));
            }
        }

        public int Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                OnPropertyChanged(nameof(Errors));
            }
        }

        public int Successes
        {
            get => _successes;
            set
            {
                _successes = value;
                OnPropertyChanged(nameof(Successes));
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

    }
}
