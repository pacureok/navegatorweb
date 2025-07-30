using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NavegadorWeb.Classes
{
    public class TabGroup : INotifyPropertyChanged
    {
        public string GroupId { get; set; } // ¡CORREGIDO! Ahora es de lectura y escritura
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

        public TabGroup(string groupName)
        {
            GroupId = Guid.NewGuid().ToString();
            _groupName = groupName;
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
