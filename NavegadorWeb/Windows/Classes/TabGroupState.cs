// This file defines a class to represent the state of a tab group for session restoration.

using System;
using System.Collections.Generic;

namespace NavegadorWeb.Classes
{
    /// <summary>
    /// Represents the serializable state of a single tab group,
    /// including its ID, name, and the URLs of its tabs.
    /// </summary>
    public class TabGroupState
    {
        public string GroupId { get; set; } = Guid.NewGuid().ToString(); // Unique ID for the tab group
        public string GroupName { get; set; } = "Default Group"; // Name of the tab group
        public List<string?> TabUrls { get; set; } = new List<string?>(); // List of URLs for tabs in this group
        public string? SelectedTabUrl { get; set; } // The URL of the currently selected tab within this group
    }
}
