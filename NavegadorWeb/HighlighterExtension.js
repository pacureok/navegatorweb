// HighlighterExtension.js
// Esta extensión resalta todas las ocurrencias de la palabra "agua" en la página.
// Si ya está resaltado, quita el resaltado.

(function() {
    // Definir la palabra clave a resaltar
    const keyword = "agua";
    const highlightClass = "aurora-highlight";
    const highlightStyle = "background-color: yellow; font-weight: bold;";

    // Función para quitar el resaltado
    function removeHighlight() {
        document.querySelectorAll(`.${highlightClass}`).forEach(el => {
            const parent = el.parentNode;
            while (el.firstChild) {
                parent.insertBefore(el.firstChild, el);
            }
            parent.removeChild(el);
            parent.normalize(); // Combinar nodos de texto adyacentes
        });
        window.auroraHighlighterInitialized = false;
    }

    // Si ya está inicializado (es decir, el resaltado ya está aplicado), quitarlo
    if (window.auroraHighlighterInitialized) {
        removeHighlight();
        return;
    }

    // Resaltar el texto
    const walk = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
    let node;
    const regex = new RegExp(keyword, 'gi'); // 'g' para global, 'i' para insensible a mayúsculas/minúsculas

    while ((node = walk.nextNode())) {
        if (node.nodeType === Node.TEXT_NODE && node.nodeValue.trim().length > 0) {
            const originalText = node.nodeValue;
            if (regex.test(originalText)) {
                const fragment = document.createDocumentFragment();
                let lastIndex = 0;
                let match;

                while ((match = regex.exec(originalText)) !== null) {
                    // Añadir el texto antes de la coincidencia
                    if (match.index > lastIndex) {
                        fragment.appendChild(document.createTextNode(originalText.substring(lastIndex, match.index)));
                    }

                    // Crear el span resaltado
                    const span = document.createElement('span');
                    span.className = highlightClass;
                    span.style.cssText = highlightStyle;
                    span.textContent = match[0];
                    fragment.appendChild(span);

                    lastIndex = regex.lastIndex;
                }

                // Añadir el texto después de la última coincidencia
                if (lastIndex < originalText.length) {
                    fragment.appendChild(document.createTextNode(originalText.substring(lastIndex)));
                }

                node.parentNode.replaceChild(fragment, node);
            }
        }
    }

    window.auroraHighlighterInitialized = true;
})();
