// pwa.js — corre en la app normal (Blazor)
// Registra el Service Worker y expone helpers de IndexedDB para C#

// ── Registro del Service Worker ───────────────────────────────────────────────
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js')
            .then(reg => console.log('[PWA] Service Worker registrado:', reg.scope))
            .catch(err => console.warn('[PWA] Error registrando SW:', err));
    });
}

// ── IndexedDB — helpers compartidos ─────────────────────────────────────────
function openPiedraAzulDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open('PiedraAzulDB', 1);
        req.onupgradeneeded = (e) => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains('cache')) {
                db.createObjectStore('cache', { keyPath: 'key' });
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror   = () => reject(req.error);
    });
}

// Llamado desde C# via JS interop al hacer login o crear cita
window.saveAppointmentsToIndexedDB = async (appointments) => {
    try {
        const db    = await openPiedraAzulDB();
        const tx    = db.transaction('cache', 'readwrite');
        const store = tx.objectStore('cache');
        store.put({ key: 'upcoming_appointments', data: appointments, timestamp: Date.now() });
        return true;
    } catch (e) {
        console.warn('[PWA] Error guardando citas:', e);
        return false;
    }
};

// Llamado desde C# para leer citas offline
window.getAppointmentsFromIndexedDB = async () => {
    try {
        const db    = await openPiedraAzulDB();
        return new Promise((resolve) => {
            const tx    = db.transaction('cache', 'readonly');
            const store = tx.objectStore('cache');
            const req   = store.get('upcoming_appointments');
            req.onsuccess = () => resolve(req.result?.data ?? null);
            req.onerror   = () => resolve(null);
        });
    } catch (e) {
        return null;
    }
};

// Verificar si hay conexión
window.isOnline = () => navigator.onLine;

// Persistir el modo UI en una cookie para que el servidor pueda leerla en el prerender SSR
window.setUiModeCookie = (mode) => {
    document.cookie = `uiMode=${mode};path=/;max-age=31536000;SameSite=Strict`;
};

// Descargar un archivo desde un data-URL (sin usar eval)
window.downloadFile = (fileName, dataContent) => {
    const a = document.createElement('a');
    a.href = 'data:' + dataContent;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

// Imprimir un documento HTML completo en un iframe aislado.
// Evita imprimir el SPA completo (sidebars, nav) y evita window.open() que el navegador bloquea.
window.printHtml = (html) => {
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow.document;
    doc.open();
    doc.write(html);
    doc.close();

    iframe.contentWindow.focus();
    setTimeout(() => {
        iframe.contentWindow.print();
        setTimeout(() => document.body.removeChild(iframe), 1000);
    }, 300);
};
