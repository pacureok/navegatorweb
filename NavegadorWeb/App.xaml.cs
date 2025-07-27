protected override void OnStartup(StartupEventArgs e)
{
    string selectedLanguage = ConfigurationManager.AppSettings["UserPreferredLanguage"] ?? "es";
    LanguageService.ApplyCulture(selectedLanguage);
    base.OnStartup(e);
}
