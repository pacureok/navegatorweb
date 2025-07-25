// MicrophoneControl.js
// Este script intenta silenciar o activar los MediaStreamTrack de audio (micrófono)
// en la página web actual.
// Nota: Solo funciona si la página ya ha solicitado y obtenido acceso al micrófono.
// No controla el micrófono a nivel de sistema operativo.

(function() {
    let message = { success: false, state: 'unknown' };

    try {
        // Buscar todos los MediaStreamTrack de audio activos
        // Esto es un desafío, ya que los streams pueden no ser globalmente accesibles.
        // La forma más robusta sería que la propia página web expusiera sus streams.
        // Sin embargo, podemos intentar buscar streams asociados a elementos de video/audio
        // o a través de navigator.mediaDevices si están en un contexto persistente.

        // En muchos casos, los streams están asociados a objetos que no son globales.
        // Una aproximación es intentar encontrar streams de video/audio elementos
        // o si la página usa WebRTC, sus RTCPeerConnections.

        // Para una solución más general, nos basaremos en que el navegador ya tiene acceso
        // y que los MediaStreamTrack pueden ser manipulados si se encuentran.

        let audioTracksFound = 0;
        let newMuteState = true; // Asumimos que queremos mutear por defecto, o alternar

        // Iterar sobre todos los MediaStreamTrack activos en el contexto del navegador (si son accesibles)
        // Esto es un placeholder, ya que no hay una API directa para obtener *todos* los streams activos.
        // La forma más común es que la página pase el stream a una función que inyectemos.
        // Como no tenemos eso, intentaremos un enfoque más amplio pero menos garantizado.

        // Intentar acceder a streams de audio de MediaDevices (si están activos y accesibles)
        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.enumerateDevices().then(devices => {
                devices.forEach(device => {
                    if (device.kind === 'audioinput' && device.deviceId) {
                        // Si hay un MediaStream activo para este dispositivo, intentar mutearlo
                        // Esto es muy difícil de hacer sin el MediaStream object en sí.
                        // La solución más fiable es que la página web exponga su MediaStream.
                        // Como no podemos garantizar eso, este script es una 'mejor suposición'.

                        // Una forma común es que los tracks de audio estén en window.localStream (si la página lo expone)
                        // o asociados a RTCPeerConnection.getSenders().getStats().
                        // Para simplificar, nos centraremos en la propiedad 'enabled' de los tracks si los encontramos.

                        // Si la página tiene un MediaStream global (ej. window.myAudioStream)
                        if (window.myAudioStream && window.myAudioStream.getAudioTracks) {
                            window.myAudioStream.getAudioTracks().forEach(track => {
                                if (track.kind === 'audio') {
                                    track.enabled = !track.enabled; // Alternar estado
                                    newMuteState = !track.enabled;
                                    audioTracksFound++;
                                }
                            });
                        }

                        // Si la página usa RTCPeerConnection (común en videollamadas/streaming)
                        // Esto es más complejo y requeriría iterar sobre todas las conexiones.
                        // Por ahora, nos centraremos en lo más simple: encontrar tracks activos.
                        // No hay una API global para obtener todos los MediaStreamTrack activos.

                        // Como fallback, si no encontramos streams globales, intentaremos un mensaje
                        // para indicar que no se pudo controlar.
                    }
                });
            });
        }

        // Una solución más directa sería que la página web exponga una función o un objeto
        // con el MediaStream que está usando. Sin esa cooperación, es una estimación.

        // Si se encontraron y manipularon tracks
        if (audioTracksFound > 0) {
            message.success = true;
            message.state = newMuteState ? 'muted' : 'unmuted';
        } else {
            message.success = false;
            message.state = 'no_active_microphone_found';
            message.error = 'No se encontraron flujos de micrófono activos o accesibles en esta página.';
        }

    } catch (e) {
        message.success = false;
        message.state = 'error';
        message.error = e.message;
    }

    // Devolver el resultado a C#
    return JSON.stringify(message);
})();
