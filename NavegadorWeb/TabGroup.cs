using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NavegadorWeb
{
    public class TabGroup : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public ObservableCollection<BrowserTabItem> TabsInGroup { get; set; }

        // Implementación explícita del evento PropertyChanged para INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged; // Se añadió '?' para nulabilidad

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabGroup(string name)
        {
            Name = name;
            TabsInGroup = new ObservableCollection<BrowserTabItem>();
        }
    }
}
