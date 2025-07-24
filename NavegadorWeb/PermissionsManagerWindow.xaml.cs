using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
{
    // Clase auxiliar para mostrar la información del permiso en la ListView
    public class SitePermission
    {
        public string Origin { get; set; }
        public string PermissionType { get; set; }
        public string State { get; set; }
        public CoreWebView2PermissionKind Kind { get; set; } // Para revocar el permiso
    }

    /// <summary>
    /// Lógica de interacción para PermissionsManagerWindow.xaml
    /// </summary>
    public partial class PermissionsManagerWindow : Window
    {
        // Delegado para obtener el CoreWebView2Environment del entorno predeterminado de MainWindow
        public delegate CoreWebView2Environment GetDefaultEnvironmentDelegate();
        private GetDefaultEnvironmentDelegate _getDefaultEnvironmentCallback;

        public PermissionsManagerWindow(GetDefaultEnvironmentDelegate getDefaultEnvironmentCallback)
        {
            InitializeComponent();
            _getDefaultEnvironmentCallback = getDefaultEnvironmentCallback;
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se ha cargado.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPermissions();
        }

        /// <summary>
        /// Carga la lista de permisos y los muestra en la ListView.
        /// </summary>
        private async void LoadPermissions()
        {
            CoreWebView2Environment environment = _getDefaultEnvironmentCallback?.Invoke();
            if (environment == null)
            {
                MessageBox.Show("No se pudo acceder al entorno del navegador para cargar los permisos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Obtener todos los permisos del perfil predeterminado
                // Nota: CoreWebView2.GetPermissionsAsync() puede no estar disponible o no funcionar como se espera
                // para todos los tipos de permisos o en todas las versiones de WebView2.
                // Esta es una aproximación y puede necesitar ajustes.
                // La forma más robusta sería interceptar los eventos de solicitud de permiso y guardarlos manualmente.

                // Por ahora, simularemos algunos permisos o nos basaremos en lo que WebView2 exponga.
                // La API de WebView2 no expone directamente una lista de todos los permisos concedidos.
                // Generalmente, se gestionan a través de eventos como CoreWebView2.PermissionRequested.
                // Para este ejemplo, vamos a listar los tipos de permisos que podrían existir y
                // si el usuario ha interactuado con ellos, se gestionarían internamente.

                // Dado que no hay una API directa para listar *todos* los permisos concedidos,
                // vamos a simular una lista o mostrar un mensaje.
                // Para una implementación real, necesitarías guardar los permisos concedidos
                // en un archivo o base de datos local cuando se conceden.

                // --- SIMULACIÓN / ENFOQUE SIMPLIFICADO ---
                // Si tuviéramos una forma de almacenar permisos concedidos:
                // List<SitePermission> grantedPermissions = YourPermissionStorage.LoadPermissions();
                // PermissionsListView.ItemsSource = grantedPermissions;
                // --- FIN SIMULACIÓN ---

                // Para un ejemplo funcional, podemos mostrar un mensaje o una lista vacía
                // y explicar que los permisos se gestionan al solicitarlos.
                // Si el usuario ha concedido permisos a través de los pop-ups de WebView2,
                // esos permisos se almacenan en el perfil de usuario de WebView2.
                // Revocarlos de forma programática es posible si conoces el origen y el tipo.

                // Ejemplo de cómo listar permisos si tuviéramos acceso a ellos:
                // (Esto es pseudo-código o asume una API que podría no existir directamente)
                var permissions = new List<SitePermission>();

                // Ejemplo de permisos que podrían ser gestionados:
                // Estas son las CoreWebView2PermissionKind que existen:
                // Geolocation, Notifications, Microphone, Camera, ClipboardRead, ClipboardWrite,
                // PointerLock, WebXR, MediaKeySystem, DesktopCapture, FileSystemWrite, FileSystemRead,
                // WindowManagement, OtherSensors, Midi, Serial, Usb, Bluetooth, BackgroundSync, PaymentHandler.

                // Un enfoque más realista para un gestor de permisos sería:
                // 1. Cuando un sitio solicita un permiso (CoreWebView2.PermissionRequested),
                //    guardar el origen y el tipo de permiso en una lista persistente (ej. archivo JSON).
                // 2. En esta ventana, cargar esa lista persistente.
                // 3. Al revocar, usar environment.ClearHostPermission(origin, kind) o similar.

                // Como no tenemos un sistema de almacenamiento de permisos implementado aún,
                // mostraremos un mensaje informativo.

                // Si tuvieras un sistema de almacenamiento:
                // var storedPermissions = YourPermissionManager.GetStoredPermissions();
                // foreach (var perm in storedPermissions)
                // {
                //     permissions.Add(new SitePermission
                //     {
                //         Origin = perm.Origin,
                //         PermissionType = perm.Type.ToString(),
                //         State = perm.State.ToString(), // Concedido, Denegado, Preguntar
                //         Kind = perm.Type
                //     });
                // }

                // Por ahora, solo mostraremos un mensaje si no hay una API directa para listar.
                // Si la API de WebView2 permitiera listar permisos concedidos:
                // var grantedPermissions = await environment.GetGrantedPermissionsAsync(); // No existe tal API directamente
                // foreach (var perm in grantedPermissions) { /* ... */ }

                // Dado que la API directa para listar permisos concedidos no está disponible en WebView2
                // de forma genérica para todos los tipos, el ListView estará vacío por defecto
                // a menos que implementes un sistema de almacenamiento propio.
                // Para este ejemplo, mostraremos un mensaje si la lista está vacía.

                // Para demostrar la funcionalidad de revocación, podemos añadir un permiso de ejemplo.
                // En una aplicación real, estos permisos se añadirían cuando el usuario los conceda.
                // permissions.Add(new SitePermission { Origin = "https://example.com", PermissionType = "Cámara", State = "Concedido", Kind = CoreWebView2PermissionKind.Camera });
                // permissions.Add(new SitePermission { Origin = "https://maps.google.com", PermissionType = "Geolocalización", State = "Concedido", Kind = CoreWebView2PermissionKind.Geolocation });


                PermissionsListView.ItemsSource = permissions;

                if (!permissions.Any())
                {
                    // Mostrar un mensaje si no hay permisos listados (debido a la limitación de la API)
                    PermissionsListView.Items.Add(new TextBlock { Text = "Los permisos se gestionan automáticamente por el navegador cuando un sitio los solicita.\nActualmente, WebView2 no expone una API para listar todos los permisos concedidos directamente.\nPara revocarlos, puedes borrar los datos de navegación en 'Opciones'." });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar permisos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Revocar".
        /// </summary>
        private async void RevokePermission_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            SitePermission permission = button?.Tag as SitePermission;

            if (permission != null)
            {
                CoreWebView2Environment environment = _getDefaultEnvironmentCallback?.Invoke();
                if (environment == null)
                {
                    MessageBox.Show("No se pudo acceder al entorno del navegador para revocar el permiso.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Revocar el permiso específico
                    // Nota: CoreWebView2.ClearHostPermission() requiere el origen y el tipo de permiso.
                    // Si el permiso no fue concedido a través del sistema de permisos de WebView2,
                    // esta llamada podría no tener efecto.
                    await environment.ClearHostPermissionAsync(permission.Origin, permission.Kind);

                    MessageBox.Show($"Permiso '{permission.PermissionType}' revocado para '{permission.Origin}'.", "Permiso Revocado", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPermissions(); // Recargar la lista para reflejar el cambio
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al revocar el permiso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Actualizar".
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPermissions();
        }

        /// <summary>
        /// Cierra la ventana.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
