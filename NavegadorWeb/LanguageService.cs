// Este archivo define el servicio de idioma para el navegador.
// Incluye funcionalidades para detectar el idioma de una página web y traducir texto.

using Microsoft.Web.WebView2.Core; // Necesario para CoreWebView2
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization; // Necesario para CultureInfo
using System.Threading; // Necesario para Thread
using System.Threading.Tasks; // Necesario para Task

namespace NavegadorWeb.Services
{
    /// <summary>
    /// Proporciona servicios relacionados con el idioma, como la detección y traducción de texto.
    /// </summary>
    public static class LanguageService
    {
        // Endpoint y clave de suscripción para el servicio de traducción de Azure
        private const string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";
        private const string SubscriptionKey = "TU_CLAVE_DE_TRADUCCION"; // <-- ¡IMPORTANTE! Reemplaza con tu clave de Azure
        private const string Location = "global"; // La región de tu recurso de Azure Translator

        /// <summary>
        /// Detecta el idioma de la página web actual en una instancia de WebView2.
        /// </summary>
        /// <param name="webView">La instancia de WebView2 de la que se detectará el idioma.</param>
        /// <returns>El código de idioma detectado (por ejemplo, "es", "en"), o "es" si no se puede detectar.</returns>
        public static async Task<string> DetectPageLanguageAsync(CoreWebView2 webView)
        {
            if (webView == null) return "es"; // Valor predeterminado si WebView2 es nulo
            
            // Script JavaScript para obtener el idioma de la página
            string script = @"(function() { return document.documentElement.lang || navigator.language || 'es'; })();";
            
            try
            {
                string result = await webView.ExecuteScriptAsync(script);
                // Deserializa el resultado JSON y devuelve el idioma, o "es" si es nulo
                return JsonSerializer.Deserialize<string>(result) ?? "es";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detectar el idioma de la página: {ex.Message}");
                return "es"; // En caso de error, devuelve el idioma predeterminado
            }
        }

        /// <summary>
        /// Traduce el texto dado a un idioma específico utilizando el servicio de traducción de Azure.
        /// </summary>
        /// <param name="text">El texto a traducir.</param>
        /// <param name="toLanguage">El código del idioma al que se va a traducir (por ejemplo, "en", "fr").</param>
        /// <returns>El texto traducido, o el texto original si la traducción falla.</returns>
        public static async Task<string> TranslateTextAsync(string text, string toLanguage)
        {
            // Verifica si la clave de suscripción es la predeterminada y advierte al usuario
            if (SubscriptionKey == "TU_CLAVE_DE_TRADUCCION" || string.IsNullOrEmpty(SubscriptionKey))
            {
                Console.WriteLine("Advertencia: La clave de suscripción de Azure Translator no ha sido configurada. La traducción no funcionará.");
                return text; // Devuelve el texto original si la clave no está configurada
            }

            using var client = new HttpClient();
            // Agrega los encabezados de suscripción necesarios para la API de Azure Translator
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", Location);

            // Crea el cuerpo de la solicitud JSON con el texto a traducir
            var requestBody = new[] { new { Text = text } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                // Realiza la llamada a la API de traducción
                var response = await client.PostAsync($"{TranslatorEndpoint}&to={toLanguage}", content);
                response.EnsureSuccessStatusCode(); // Lanza una excepción si la respuesta no es exitosa
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                // Extrae el texto traducido del resultado JSON
                return doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString() ?? text;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error de solicitud HTTP al traducir: {ex.Message}");
                return text; // Devuelve el texto original en caso de error HTTP
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error al analizar la respuesta JSON de traducción: {ex.Message}");
                return text; // Devuelve el texto original en caso de error de JSON
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado durante la traducción: {ex.Message}");
                return text; // Devuelve el texto original en caso de cualquier otro error
            }
        }

        /// <summary>
        /// Aplica una cultura específica al hilo actual de la aplicación.
        /// Esto afecta cómo se formatean las fechas, números y cadenas.
        /// </summary>
        /// <param name="languageCode">El código de idioma de la cultura a aplicar (por ejemplo, "es-ES", "en-US").</param>
        public static void ApplyCulture(string languageCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = culture; // Cultura para formateo (números, fechas)
                Thread.CurrentThread.CurrentUICulture = culture; // Cultura para recursos de UI (cadenas, etc.)
            }
            catch (CultureNotFoundException ex)
            {
                Console.WriteLine($"Cultura '{languageCode}' no encontrada: {ex.Message}. Se usará la cultura predeterminada.");
                // Puedes optar por establecer una cultura de respaldo aquí, por ejemplo:
                // Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                // Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }
        }
    }
}
