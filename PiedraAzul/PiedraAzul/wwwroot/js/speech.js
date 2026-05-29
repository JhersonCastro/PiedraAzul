// Reconocimiento de voz (Web Speech API) para la consulta inteligente.
// Expone window.PiedraSpeech con isSupported / start / stop.
window.PiedraSpeech = (function () {
    let recognition = null;

    function isSupported() {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    }

    function start(dotnetRef) {
        const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SR) return false;

        stop();

        recognition = new SR();
        recognition.lang = 'es-ES';
        recognition.continuous = true;
        recognition.interimResults = false;

        recognition.onresult = function (e) {
            let finalText = '';
            for (let i = e.resultIndex; i < e.results.length; i++) {
                if (e.results[i].isFinal) {
                    finalText += e.results[i][0].transcript;
                }
            }
            if (finalText && dotnetRef) {
                dotnetRef.invokeMethodAsync('OnSpeechResult', finalText.trim());
            }
        };

        recognition.onerror = function () {
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnSpeechEnded');
        };

        recognition.onend = function () {
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnSpeechEnded');
        };

        try {
            recognition.start();
            return true;
        } catch (e) {
            return false;
        }
    }

    function stop() {
        if (recognition) {
            try { recognition.stop(); } catch (e) { /* ignore */ }
            recognition = null;
        }
    }

    return { isSupported, start, stop };
})();
