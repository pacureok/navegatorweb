using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NavegadorWeb.Classes; // Para TabGroup

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
            if (!TabGroups.Any())
            {
                AddGroup(new TabGroup("Default Group"));
            }
            SelectedTabGroup = TabGroups.FirstOrDefault();
        }

        public void AddGroup(TabGroup group)
        {
            TabGroups.Add(group);
            OnPropertyChanged(nameof(TabGroups));
        }

        public void RemoveGroup(TabGroup group)
        {
            TabGroups.Remove(group);
            if (SelectedTabGroup == group)
            {
                SelectedTabGroup = TabGroups.FirstOrDefault();
            }
            OnPropertyChanged(nameof(TabGroups));
        }

        public TabGroup GetDefaultGroup()
        {
            return TabGroups.FirstOrDefault(g => g.GroupName == "Default Group") ?? CreateNewDefaultGroup();
        }

        private TabGroup CreateNewDefaultGroup()
        {
            var defaultGroup = new TabGroup("Default Group");
            TabGroups.Insert(0, defaultGroup);
            SelectedTabGroup = defaultGroup;
            return defaultGroup;
        }

        public TabGroup? GetGroupByTab(TabItemData tab)
        {
            return TabGroups.FirstOrDefault(g => g.TabsInGroup.Contains(tab));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
