// PageColorExtractor.js
(function() {
    function getDominantColor(element) {
        if (!element) return null;

        // Intentar obtener el color de fondo de la barra de navegación principal o del cuerpo
        let color = window.getComputedStyle(element).backgroundColor;

        // Si el color es transparente o no definido, buscar en elementos más específicos
        if (!color || color === 'rgba(0, 0, 0, 0)' || color === 'transparent') {
            // Buscar un posible header o barra superior
            let header = document.querySelector('header, .header, .navbar, .top-bar, #header, #navbar, #top-bar');
            if (header) {
                color = window.getComputedStyle(header).backgroundColor;
            }
        }

        // Si sigue siendo transparente o no definido, usar el color de fondo del body/html
        if (!color || color === 'rgba(0, 0, 0, 0)' || color === 'transparent') {
            color = window.getComputedStyle(document.body).backgroundColor;
        }
        if (!color || color === 'rgba(0, 0, 0, 0)' || color === 'transparent') {
            color = window.getComputedStyle(document.documentElement).backgroundColor;
        }

        // Convertir el color a formato hexadecimal si es RGB/RGBA
        if (color && color.startsWith('rgb')) {
            let rgba = color.match(/\d+/g);
            if (rgba && rgba.length >= 3) {
                let r = parseInt(rgba[0]).toString(16).padStart(2, '0');
                let g = parseInt(rgba[1]).toString(16).padStart(2, '0');
                let b = parseInt(rgba[2]).toString(16).padStart(2, '0');
                return `#${r}${g}${b}`.toUpperCase();
            }
        }
        return color; // Devolver el color tal cual si ya es hex o no pudo convertirse
    }

    // Devuelve el color dominante de la página. Puedes ajustar el elemento a analizar.
    let dominantColor = getDominantColor(document.body); // O document.querySelector('body')

    return JSON.stringify({ dominantColor: dominantColor });
})();
