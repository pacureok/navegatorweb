// This file defines the TabGroup class, which represents a collection of browser tabs.
// It allows for organizing tabs into logical groups.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NavegadorWeb.Classes; // For TabItemData

namespace NavegadorWeb.Classes
{
    /// <summary>
    /// Represents a group of browser tabs.
    /// Implements INotifyPropertyChanged to enable data binding in the UI.
    /// </summary>
    public class TabGroup : INotifyPropertyChanged
    {
        public string GroupId { get; set; } // <--- ¡CORREGIDO! Ahora es de lectura y escritura para la serialización
        private string _groupName;

        // Public property for the group's name with change notification
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

        // ObservableCollection to hold tabs within this group, for UI binding
        public ObservableCollection<TabItemData> TabsInGroup { get; } = new ObservableCollection<TabItemData>();

        private TabItemData? _selectedTabItem;

        // Public property for the currently selected tab within this group
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

        /// <summary>
        /// Initializes a new instance of the TabGroup class with a specified group name and a unique ID.
        /// </summary>
        /// <param name="groupName">The name of the tab group.</param>
        public TabGroup(string groupName)
        {
            GroupId = Guid.NewGuid().ToString(); // Assign a unique ID when a new group is created
            _groupName = groupName;
        }

        /// <summary>
        /// Adds a tab to this group.
        /// </summary>
        /// <param name="tab">The TabItemData to add.</param>
        public void AddTab(TabItemData tab)
        {
            TabsInGroup.Add(tab);
            if (SelectedTabItem == null) // Automatically select the first tab added
            {
                SelectedTabItem = tab;
            }
            OnPropertyChanged(nameof(TabsInGroup)); // Notify UI that the collection changed
        }

        /// <summary>
        /// Removes a tab from this group.
        /// </summary>
        /// <param name="tab">The TabItemData to remove.</param>
        public void RemoveTab(TabItemData tab)
        {
            TabsInGroup.Remove(tab);
            if (SelectedTabItem == tab) // If the removed tab was selected, select another one
            {
                SelectedTabItem = TabsInGroup.FirstOrDefault();
            }
            OnPropertyChanged(nameof(TabsInGroup)); // Notify UI that the collection changed
        }

        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
