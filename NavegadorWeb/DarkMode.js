// DarkMode.js
// Este script se inyectará en la página web para aplicar/desaplicar el modo oscuro.

(function() {
    const styleId = 'mi-navegador-dark-mode-style';
    let styleTag = document.getElementById(styleId);

    if (styleTag) {
        // El modo oscuro está activo, así que lo desactivamos
        styleTag.remove();
        document.documentElement.classList.remove('mi-navegador-dark-mode-active');
        document.body.classList.remove('mi-navegador-dark-mode-active'); // También para el body
    } else {
        // El modo oscuro no está activo, así que lo activamos
        styleTag = document.createElement('style');
        styleTag.id = styleId;
        styleTag.textContent = `
            html.mi-navegador-dark-mode-active, body.mi-navegador-dark-mode-active {
                background-color: #1a1a1a !important;
                color: #e0e0e0 !important;
            }
            /* Aplicar estilos a todos los elementos para asegurar la herencia y anulación */
            html.mi-navegador-dark-mode-active *, body.mi-navegador-dark-mode-active * {
                background-color: inherit !important; /* Heredar el fondo para consistencia */
                color: inherit !important; /* Heredar el color del texto */
                border-color: #333 !important; /* Bordes más oscuros */
            }
            /* Invertir imágenes, videos e iframes para que no sean demasiado brillantes */
            html.mi-navegador-dark-mode-active img,
            html.mi-navegador-dark-mode-active video,
            html.mi-navegador-dark-mode-active iframe {
                filter: invert(1) hue-rotate(180deg) !important;
            }
            /* Ajustes para elementos que podrían no heredar bien o necesitar anulaciones específicas */
            html.mi-navegador-dark-mode-active a {
                color: #8ab4f8 !important; /* Azul más claro para enlaces */
            }
            html.mi-navegador-dark-mode-active button,
            html.mi-navegador-dark-mode-active input[type="button"],
            html.mi-navegador-dark-mode-active input[type="submit"] {
                background-color: #333 !important;
                color: #e0e0e0 !important;
                border: 1px solid #555 !important;
            }
            html.mi-navegador-dark-mode-active input,
            html.mi-navegador-dark-mode-active textarea {
                background-color: #2a2a2a !important;
                color: #e0e0e0 !important;
                border: 1px solid #444 !important;
            }
            html.mi-navegador-dark-mode-active select {
                background-color: #2a2a2a !important;
                color: #e0e0e0 !important;
                border: 1px solid #444 !important;
            }
            /* Anular estilos inline que puedan tener fondos blancos */
            html.mi-navegador-dark-mode-active [style*="background-color: rgb(255, 255, 255)"],
            html.mi-navegador-dark-mode-active [style*="background: rgb(255, 255, 255)"] {
                background-color: #1a1a1a !important;
            }
            /* Anulaciones específicas para elementos comunes que podrían resistir la inversión */
            html.mi-navegador-dark-mode-active .header,
            html.mi-navegador-dark-mode-active .footer,
            html.mi-navegador-dark-mode-active .navbar,
            html.mi-navegador-dark-mode-active .sidebar {
                background-color: #222 !important;
                color: #e0e0e0 !important;
            }
            /* Asegurar que los fondos de los elementos de formulario se oscurezcan */
            html.mi-navegador-dark-mode-active input:not([type="checkbox"]):not([type="radio"]),
            html.mi-navegador-dark-mode-active textarea,
            html.mi-navegador-dark-mode-active select {
                background-color: #2a2a2a !important;
                color: #e0e0e0 !important;
            }
            /* Ajustes para scrollbars en navegadores que lo soportan */
            html.mi-navegador-dark-mode-active ::-webkit-scrollbar-thumb {
                background-color: #555 !important;
            }
            html.mi-navegador-dark-mode-active ::-webkit-scrollbar-track {
                background-color: #222 !important;
            }
        `;
        document.head.appendChild(styleTag);
        // Añadir clases al html y body para activar los estilos
        document.documentElement.classList.add('mi-navegador-dark-mode-active');
        document.body.classList.add('mi-navegador-dark-mode-active');
    }
})();
