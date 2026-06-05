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

// Imprimir una tabla en un iframe aislado — evita imprimir el SPA completo (sidebars, nav, etc.)
// Si checkLast = true, cada fila añade una columna final con una casilla vacía ☐ para marcar
// manualmente la asistencia (✓ asistió / ✗ no asistió) sobre el papel.
window.printTable = (title, subtitle, headers, rows, checkLast) => {
    const esc = (s) => String(s ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    const thead = '<tr>' + headers.map((h, i) =>
        `<th class="${checkLast && i === headers.length - 1 ? 'chk-col' : ''}">${esc(h)}</th>`
    ).join('') + '</tr>';

    const tbody = rows.map(r => {
        const cells = r.map(c => '<td>' + esc(c) + '</td>').join('');
        const box = checkLast ? '<td class="chk-col"><span class="chk"></span></td>' : '';
        return '<tr>' + cells + box + '</tr>';
    }).join('');

    const html = `<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"><title>${esc(title)}</title>
        <style>
            * { box-sizing: border-box; }
            body { font-family: 'DM Sans', -apple-system, Segoe UI, sans-serif; color: #0F172A; margin: 32px; }
            h1 { font-size: 20px; margin: 0 0 4px; color: #257D8D; }
            .sub { font-size: 12px; color: #64748B; margin: 0 0 20px; }
            table { width: 100%; border-collapse: collapse; font-size: 12px; }
            thead th { text-align: left; text-transform: uppercase; letter-spacing: .04em;
                       font-size: 10px; color: #475569; border-bottom: 2px solid #257D8D;
                       padding: 8px 10px; }
            tbody td { padding: 8px 10px; border-bottom: 1px solid #E2E8F0; }
            tbody tr:nth-child(even) { background: #F8FAFC; }
            .chk-col { text-align: center; width: 70px; }
            .chk { display: inline-block; width: 16px; height: 16px;
                   border: 1.5px solid #475569; border-radius: 3px; }
            @media print { body { margin: 0; } tbody tr:nth-child(even) { background: #F8FAFC !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
        </style></head>
        <body>
            <h1>${esc(title)}</h1>
            <p class="sub">${esc(subtitle)}</p>
            <table><thead>${thead}</thead><tbody>${tbody}</tbody></table>
        </body></html>`;

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
