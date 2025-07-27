using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NavegadorWeb.Classes; // Para TabItemData

namespace NavegadorWeb.Classes
{
    public class TabGroup : INotifyPropertyChanged
    {
        public string GroupId { get; }
        private string _groupName;
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        public ObservableCollection<TabItemData> TabsInGroup { get; } = new ObservableCollection<TabItemData>();

        private TabItemData? _selectedTabItem;
        public TabItemData? SelectedTabItem
        {
            get => _selectedTabItem;
            set
            {
                if (_selectedTabItem != value)
                {
                    _selectedTabItem = value;
                    OnPropertyChanged(nameof(SelectedTabItem));
                }
            }
        }

        public TabGroup(string name)
        {
            GroupId = Guid.NewGuid().ToString();
            _groupName = name;
        }

        public TabGroup(string id, string name) // Constructor para restauración
        {
            GroupId = id;
            _groupName = name;
        }

        public void AddTab(TabItemData tab)
        {
            TabsInGroup.Add(tab);
            if (SelectedTabItem == null)
            {
                SelectedTabItem = tab;
            }
            OnPropertyChanged(nameof(TabsInGroup));
        }

        public void RemoveTab(TabItemData tab)
        {
            TabsInGroup.Remove(tab);
            if (SelectedTabItem == tab)
            {
                SelectedTabItem = TabsInGroup.FirstOrDefault();
            }
            OnPropertyChanged(nameof(TabsInGroup));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
