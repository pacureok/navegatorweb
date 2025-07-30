using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NavegadorWeb.Classes
{
    public class TabGroupManager : INotifyPropertyChanged
    {
        public ObservableCollection<TabGroup> TabGroups { get; } = new ObservableCollection<TabGroup>();

        private TabGroup? _selectedTabGroup;

        public TabGroup? SelectedTabGroup
        {
            get => _selectedTabGroup;
            set
            {
                if (_selectedTabGroup != value)
                {
                    _selectedTabGroup = value;
                    OnPropertyChanged(nameof(SelectedTabGroup));
                }
            }
        }

        public TabGroupManager()
        {
            // Asegura que siempre haya al menos un grupo de pestañas por defecto
            if (!TabGroups.Any())
            {
                AddTabGroup(new TabGroup("Grupo Principal"));
            }
            SelectedTabGroup ??= TabGroups.FirstOrDefault();
        }

        public void AddTabGroup(TabGroup group)
        {
            TabGroups.Add(group);
            OnPropertyChanged(nameof(TabGroups));
        }

        public void RemoveTabGroup(TabGroup group)
        {
            if (TabGroups.Count > 1) // No permitir eliminar el último grupo
            {
                TabGroups.Remove(group);
                if (SelectedTabGroup == group)
                {
                    SelectedTabGroup = TabGroups.FirstOrDefault();
                }
                OnPropertyChanged(nameof(TabGroups));
            }
        }

        public TabGroup? GetDefaultGroup()
        {
            return TabGroups.FirstOrDefault(g => g.GroupName == "Grupo Principal") ?? TabGroups.FirstOrDefault();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
