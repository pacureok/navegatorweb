using System;

namespace NavegadorWeb.Classes
{
    public class PasswordEntry
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }
}
