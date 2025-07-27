// Este archivo contiene la lógica de inicio de la aplicación WPF.
// Se encarga de aplicar la cultura del idioma preferido por el usuario al inicio.

using System.Configuration; // Necesario para ConfigurationManager
using System.Windows;
using NavegadorWeb.Services; // Necesario para LanguageService

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Se invoca cuando la aplicación se inicia.
        /// Aplica la cultura del idioma seleccionada por el usuario.
        /// </summary>
        /// <param name="e">Argumentos de inicio.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Obtiene el idioma preferido del usuario desde la configuración de la aplicación
            string selectedLanguage = ConfigurationManager.AppSettings["UserPreferredLanguage"] ?? "es";
            
            // Aplica la cultura del idioma a la aplicación
            LanguageService.ApplyCulture(selectedLanguage);
            
            base.OnStartup(e);
        }
    }
}
