using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NavegadorWeb
{
    /// <summary>
    /// Representa un grupo de pestañas en el navegador.
    /// Implementa INotifyPropertyChanged para que la UI se actualice automáticamente.
    /// </summary>
    public class TabGroup : INotifyPropertyChanged
    {
        private string _groupName;
        public string GroupName
        {
            get { return _groupName; }
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        public ObservableCollection<BrowserTabItem> TabsInGroup { get; set; }

        public TabGroup(string name)
        {
            GroupName = name;
            TabsInGroup = new ObservableCollection<BrowserTabItem>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
