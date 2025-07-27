using System;
using System.Collections.Generic;

namespace NavegadorWeb.Classes
{
    public class TabGroupState
    {
        public string GroupId { get; set; } = Guid.NewGuid().ToString();
        public string GroupName { get; set; } = "Default Group";
        public List<string?> TabUrls { get; set; } = new List<string?>();
        public string? SelectedTabUrl { get; set; }
    }
}
