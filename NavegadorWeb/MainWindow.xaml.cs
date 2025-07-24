private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
{
    WebView2 currentWebView = sender as WebView2;
    if (currentWebView != null && e.IsSuccess)
    {
        // Asegúrate de que esto solo se ejecute una vez por CoreWebView2
        currentWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
        currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

        // ***** NUEVA LÍNEA PARA HABILITAR F12 (Developer Tools) *****
        // Habilita el menú contextual del botón derecho para incluir la opción "Inspect"
        // y permite abrir las herramientas de desarrollador con F12.
        currentWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        // Si quieres que el usuario pueda hacer clic derecho y ver el menú de WebView2 por defecto
        // currentWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true; // Esto habilitaría F12 y otros atajos del navegador

        // También podemos escuchar eventos para saber cuándo se abren/cierran las DevTools si fuera necesario:
        // currentWebView.CoreWebView2.DevToolsRequested += CoreWebView2_DevToolsRequested;
    }
}
