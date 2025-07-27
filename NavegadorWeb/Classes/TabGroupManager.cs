// This file defines the TabGroupManager class, which manages multiple TabGroup instances.
// It provides methods for adding, removing, and selecting tab groups, and accessing the default group.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NavegadorWeb.Classes; // For TabGroup

namespace NavegadorWeb.Classes
{
    /// <summary>
    /// Manages a collection of TabGroup instances.
    /// Implements INotifyPropertyChanged to enable data binding in the UI.
    /// </summary>
    public class TabGroupManager : INotifyPropertyChanged
    {
        // ObservableCollection to hold all tab groups, for UI binding
        public ObservableCollection<TabGroup> TabGroups { get; } = new ObservableCollection<TabGroup>();

        private TabGroup? _selectedTabGroup;

        // Public property for the currently selected tab group
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

        /// <summary>
        /// Initializes a new instance of the TabGroupManager class.
        /// Creates a default tab group if none exist.
        /// </summary>
        public TabGroupManager()
        {
            if (!TabGroups.Any())
            {
                AddGroup(new TabGroup("Default Group")); // Create a default group if none exists
            }
            SelectedTabGroup = TabGroups.FirstOrDefault(); // Select the first group by default
        }

        /// <summary>
        /// Adds a new tab group to the manager.
        /// </summary>
        /// <param name="group">The TabGroup to add.</param>
        public void AddGroup(TabGroup group)
        {
            TabGroups.Add(group);
            OnPropertyChanged(nameof(TabGroups)); // Notify UI that the collection changed
        }

        /// <summary>
        /// Removes a tab group from the manager.
        /// </summary>
        /// <param name="group">The TabGroup to remove.</param>
        public void RemoveGroup(TabGroup group)
        {
            TabGroups.Remove(group);
            if (SelectedTabGroup == group) // If the removed group was selected, select another one
            {
                SelectedTabGroup = TabGroups.FirstOrDefault();
            }
            OnPropertyChanged(nameof(TabGroups)); // Notify UI that the collection changed
        }

        /// <summary>
        /// Gets the default tab group. Creates it if it doesn't exist.
        /// </summary>
        /// <returns>The default TabGroup.</returns>
        public TabGroup GetDefaultGroup()
        {
            return TabGroups.FirstOrDefault(g => g.GroupName == "Default Group") ?? CreateNewDefaultGroup();
        }

        /// <summary>
        /// Creates a new default tab group and adds it to the manager.
        /// </summary>
        /// <returns>The newly created default TabGroup.</returns>
        private TabGroup CreateNewDefaultGroup()
        {
            var defaultGroup = new TabGroup("Default Group");
            TabGroups.Insert(0, defaultGroup); // Insert at the beginning
            SelectedTabGroup = defaultGroup; // Select the new default group
            return defaultGroup;
        }

        /// <summary>
        /// Finds the TabGroup that contains the specified TabItemData.
        /// </summary>
        /// <param name="tab">The TabItemData to search for.</param>
        /// <returns>The TabGroup containing the tab, or null if not found.</returns>
        public TabGroup? GetGroupByTab(TabItemData tab)
        {
            return TabGroups.FirstOrDefault(g => g.TabsInGroup.Contains(tab));
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
