private string _userPreferredLanguage = "es";

public string UserPreferredLanguage
{
    get => _userPreferredLanguage;
    set
    {
        if (_userPreferredLanguage != value)
        {
            _userPreferredLanguage = value;
            OnPropertyChanged(nameof(UserPreferredLanguage));
        }
    }
}
