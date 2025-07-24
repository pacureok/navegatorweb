// ReaderMode.js
// Este script se inyectará en la página web para aplicar el modo lectura.

(function() {
    // Función para eliminar elementos no deseados
    function removeElements(selector) {
        document.querySelectorAll(selector).forEach(el => el.remove());
    }

    // Función para aplicar estilos de lectura
    function applyReaderStyles() {
        // Crear un contenedor principal para el contenido de lectura
        let readerContainer = document.createElement('div');
        readerContainer.id = 'mi-navegador-reader-mode-container';

        // Intentar encontrar el contenido principal de la página
        // Esto es heurístico y puede no funcionar perfectamente en todas las páginas
        let mainContent = document.querySelector('article, main, .post-content, .entry-content, #content, #main');

        if (!mainContent) {
            // Si no encontramos un elemento principal obvio, tomamos el body o un contenedor grande
            mainContent = document.body;
        }

        // Clonar el contenido principal para no modificar el original directamente
        // y para evitar problemas con eventos o scripts existentes
        let clonedContent = mainContent.cloneNode(true);

        // Limpiar el contenido clonado
        // Eliminar scripts, estilos, iframes, anuncios, etc.
        removeElements.call(clonedContent, 'script, style, iframe, noscript, svg, audio, video, form, input, button, textarea, select, header, footer, nav, aside, .ad, .advertisement, .sidebar, .comments, .related-posts, .share-buttons, .widget, [class*="ad-"], [id*="ad-"]');

        // Eliminar atributos de estilo inline que puedan interferir
        clonedContent.querySelectorAll('*').forEach(el => {
            el.removeAttribute('style');
            el.removeAttribute('class'); // Opcional: eliminar clases para limpiar estilos
            el.removeAttribute('id'); // Opcional: eliminar IDs
        });

        readerContainer.appendChild(clonedContent);

        // Limpiar el body original (eliminar todo excepto el nuevo contenedor de lectura)
        while (document.body.firstChild) {
            document.body.removeChild(document.body.firstChild);
        }
        document.body.appendChild(readerContainer);

        // Aplicar estilos CSS para el modo lectura
        let style = document.createElement('style');
        style.id = 'mi-navegador-reader-mode-style';
        style.textContent = `
            body {
                font-family: 'Georgia', serif;
                line-height: 1.6;
                color: #333;
                background-color: #f4f4f4;
                margin: 0;
                padding: 0;
            }
            #mi-navegador-reader-mode-container {
                max-width: 800px;
                margin: 40px auto;
                padding: 30px;
                background-color: #fff;
                box-shadow: 0 0 10px rgba(0,0,0,0.1);
                border-radius: 8px;
            }
            #mi-navegador-reader-mode-container p {
                margin-bottom: 1em;
                font-size: 1.1em;
            }
            #mi-navegador-reader-mode-container h1,
            #mi-navegador-reader-mode-container h2,
            #mi-navegador-reader-mode-container h3 {
                font-family: 'Helvetica Neue', sans-serif;
                color: #222;
                margin-top: 1.5em;
                margin-bottom: 0.8em;
                line-height: 1.2;
            }
            #mi-navegador-reader-mode-container img {
                max-width: 100%;
                height: auto;
                display: block;
                margin: 20px auto;
                border-radius: 4px;
            }
            #mi-navegador-reader-mode-container a {
                color: #007bff;
                text-decoration: underline;
            }
            #mi-navegador-reader-mode-container blockquote {
                border-left: 4px solid #ccc;
                padding-left: 15px;
                margin-left: 20px;
                font-style: italic;
                color: #555;
            }
            #mi-navegador-reader-mode-container ul,
            #mi-navegador-reader-mode-container ol {
                margin-left: 25px;
            }
            #mi-navegador-reader-mode-container li {
                margin-bottom: 0.5em;
            }
        `;
        document.head.appendChild(style);

        // Opcional: Desactivar eventos de scroll o interacciones que puedan romper el modo lectura
        // document.body.style.overflow = 'auto';
    }

    // Función para restaurar la página a su estado original (eliminar estilos y contenedor del modo lectura)
    function restoreOriginalStyles() {
        let readerContainer = document.getElementById('mi-navegador-reader-mode-container');
        if (readerContainer) {
            readerContainer.remove();
        }
        let readerStyle = document.getElementById('mi-navegador-reader-mode-style');
        if (readerStyle) {
            readerStyle.remove();
        }
        // Para restaurar el contenido original, necesitaríamos haberlo guardado antes.
        // En esta implementación simple, solo eliminamos los estilos de lectura.
        // Una recarga de página es la forma más sencilla de volver al estado original.
        // Si no se recarga, la página quedará "vacía" o con el contenido limpiado.
        window.location.reload(); // Recargar la página para restaurar el contenido original
    }

    // Comprobar si el modo lectura ya está activo para alternar
    if (document.getElementById('mi-navegador-reader-mode-container')) {
        restoreOriginalStyles();
    } else {
        applyReaderStyles();
    }

})();
