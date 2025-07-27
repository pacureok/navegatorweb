using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace NavegadorWeb.Services
{
    public static class LanguageService
    {
        private const string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";
        private const string SubscriptionKey = "TU_CLAVE_DE_TRADUCCION"; // Cámbiala por la clave real
        private const string Location = "global"; // O la región de tu recurso de Azure

        public static async Task<string> DetectPageLanguageAsync(CoreWebView2 webView)
        {
            if (webView == null) return "es";
            string script = @"(function() { return document.documentElement.lang || navigator.language || 'es'; })();";
            string result = await webView.ExecuteScriptAsync(script);
            return JsonSerializer.Deserialize<string>(result) ?? "es";
        }

        public static async Task<string> TranslateTextAsync(string text, string toLanguage)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", Location);

            var requestBody = new[] { new { Text = text } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{TranslatorEndpoint}&to={toLanguage}", content);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString() ?? text;
        }

        public static void ApplyCulture(string languageCode)
        {
            CultureInfo culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
