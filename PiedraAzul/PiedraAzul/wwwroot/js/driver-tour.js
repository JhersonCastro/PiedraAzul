let driverInstance = null;

// Inyectar estilos personalizados de Piedra Azul
function injectCustomStyles() {
    if (document.getElementById('driver-piedra-azul-styles')) return;

    const style = document.createElement('style');
    style.id = 'driver-piedra-azul-styles';
    style.textContent = `
        /* Variables de Paleta Piedra Azul */
        :root {
            --pa-primary: #257d8d;
            --pa-primary-hover: #1a5f6d;
            --pa-dark: #0f172a;
            --pa-muted: #64748b;
            --pa-light: #94a3b8;
            --pa-border: #e2e8f0;
            --pa-bg-btn: #f8fafc;
            --pa-bg-btn-hover: #f1f5f9;
            --pa-font: 'DM Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
        }

        /* Popover principal y tipografía base */
        .driver-popover {
            font-family: var(--pa-font) !important;
            background-color: #ffffff !important;
            color: var(--pa-dark) !important;
            box-shadow: 0 20px 25px -5px rgba(15, 23, 42, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04) !important;
            border-radius: 12px !important;
            border: 1px solid var(--pa-border) !important;
            min-width: 290px !important;
            max-width: 350px !important;
            padding: 22px !important;
        }

        /* Título del popover */
        .driver-popover-title {
            color: var(--pa-primary) !important;
            font-size: 16px !important;
            font-weight: 700 !important;
            margin-bottom: 8px !important;
        }

        /* Descripción */
        .driver-popover-description {
            color: var(--pa-muted) !important;
            font-size: 15px !important;
            line-height: 1.7 !important;
            margin-bottom: 18px !important;
        }

        /* ── Base compartida de botones ── */
        .driver-popover-footer button {
            font-family: var(--pa-font), system-ui, -apple-system, sans-serif !important;
            font-size: 15px !important;
            font-weight: 600 !important;
            padding: 9px 20px !important;
            border-radius: 8px !important;
            border: 1.5px solid transparent !important;
            cursor: pointer !important;
            text-shadow: none !important;
            box-shadow: none !important;
            /* Solo transicionar colores, nunca 'all' (evita artefactos de compositing) */
            transition: background-color 0.18s ease, border-color 0.18s ease, color 0.18s ease !important;
            /* Antialiased es correcto para texto sobre fondos de color */
            -webkit-font-smoothing: antialiased !important;
            -moz-osx-font-smoothing: grayscale !important;
        }

        /* ── Botón "Anterior" — estilo secundario ── */
        .driver-popover-prev-btn {
            background-color: #ffffff !important;
            color: var(--pa-muted) !important;
            border-color: var(--pa-border) !important;
        }
        .driver-popover-prev-btn:hover {
            background-color: var(--pa-bg-btn-hover) !important;
            border-color: var(--pa-primary) !important;
            color: var(--pa-primary) !important;
        }

        /* ── Botón "Siguiente" / "Finalizar" — estilo primario ── */
        .driver-popover-next-btn {
            background-color: var(--pa-primary) !important;
            color: #ffffff !important;
            border-color: var(--pa-primary) !important;
        }
        .driver-popover-next-btn:hover {
            background-color: var(--pa-primary-hover) !important;
            border-color: var(--pa-primary-hover) !important;
        }

        /* ── Botón deshabilitado ── */
        .driver-popover-btn-disabled {
            opacity: 0.35 !important;
            cursor: not-allowed !important;
            pointer-events: none !important;
        }

        /* Texto de progreso (e.g. 1 de 4) */
        .driver-popover-progress-text {
            color: var(--pa-muted) !important;
            font-size: 13px !important;
            font-weight: 600 !important;
        }

        /* Botón cerrar (X) */
        .driver-popover-close-btn {
            color: #cbd5e1 !important;
            transition: color 0.2s ease !important;
        }

        .driver-popover-close-btn:hover {
            color: var(--pa-primary) !important;
        }

        /* ARREGLO DE LA FLECHA: Dejamos que Driver la rote de forma nativa sin romper su geometría */
        .driver-popover-arrow {
            background-color: #ffffff !important;
            border: none !important;
        }

        /* CONTORNO DEL HIGHLIGHT: Un marco fino de marca sin alterar el fondo del elemento */
        .driver-stage {
            background-color: transparent !important;
            border-radius: 8px !important;
            outline: 3px solid var(--pa-primary) !important;
            box-shadow: 0 0 15px rgba(37, 125, 141, 0.15) !important;
        }
    `;
    document.head.appendChild(style);
}

window.DriverTour = {

    isMobile: function () {
        return window.innerWidth < 768;
    },

    isTourSeen: function (tourId) {
        return localStorage.getItem('tour_' + tourId + '_seen') === 'true';
    },

    markTourAsSeen: function (tourId) {
        localStorage.setItem('tour_' + tourId + '_seen', 'true');
    },

    startTour: function (tourSteps, tourId, dotnetRef) {
        if (driverInstance) {
            driverInstance.destroy();
            driverInstance = null;
        }

        // Inyectar estilos personalizados de Piedra Azul
        injectCustomStyles();

        const driverFn = window.driver?.js?.driver
            ?? window.driver?.driver
            ?? window.driver;

        if (typeof driverFn !== 'function') {
            console.error('[DriverTour] driver.js no encontrado.');
            return;
        }

        const invoke = async function (name, ...args) {
            if (!dotnetRef) return;
            try { await dotnetRef.invokeMethodAsync(name, ...args); }
            catch (e) { console.warn('[DriverTour]', name, 'falló:', e); }
        };

        const navigateIfNeeded = async function (stepIdx) {
            if (stepIdx < 0 || stepIdx >= tourSteps.length) return;
            const step = tourSteps[stepIdx];
            if (step?.tab) {
                await invoke('TourNavigateToTab', step.tab);
                // Dar tiempo a Blazor para re-renderizar el DOM tras el cambio de tab
                await new Promise(r => setTimeout(r, 180));
            }
        };

        // Configuración nativa limpia para Driver.js
        const config = {
            allowClose: true,
            animate: true,
            overlayColor: '#0f172a',          // Color del fondo oscuro controlado por JS
            overlayOpacity: 0.45,             // Opacidad ideal: oscurece el fondo sin cegar la pantalla
            stagePadding: 6,
            stageRadius: 8,
            showProgress: true,
            popoverOffset: 12,
            nextBtnText: 'Siguiente →',
            prevBtnText: '← Anterior',
            doneBtnText: 'Finalizar',
            steps: tourSteps,
            onDestroyStarted: function () {
                if (tourId) window.DriverTour.markTourAsSeen(tourId);
                driverInstance.destroy();
                driverInstance = null;
            }
        };

        if (dotnetRef) {
            config.onNextClick = async function () {
                await navigateIfNeeded(driverInstance.getActiveIndex() + 1);
                driverInstance.moveNext();
            };
            config.onPrevClick = async function () {
                await navigateIfNeeded(driverInstance.getActiveIndex() - 1);
                driverInstance.movePrevious();
            };
        }

        driverInstance = driverFn(config);

        // Navegar al tab del primer step si lo tiene
        navigateIfNeeded(0).then(() => driverInstance.drive());
    },

    // Avanza al siguiente paso del tour.
    //
    // targetSelector  → selector CSS del elemento que debe existir en el DOM.
    // waitForContent  → si es true, además espera a que desaparezca cualquier
    //                   spinner (.animate-spin) dentro del elemento, garantizando
    //                   que el contenido ya se renderizó antes de posicionar el popover.
    //
    // Máximo de espera: 40 intentos × 150ms = 6 segundos.
    advanceTour: function (targetSelector, waitForContent) {
        if (!driverInstance) return;

        if (!targetSelector) {
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    if (driverInstance) driverInstance.moveNext();
                });
            });
            return;
        }

        var attempts = 0;
        var maxAttempts = 40;

        var check = function () {
            if (!driverInstance) return;

            var el = document.querySelector(targetSelector);

            if (!el) {
                // Elemento todavía no en el DOM
                if (attempts >= maxAttempts) return;
                attempts++;
                setTimeout(check, 150);
                return;
            }

            // Si se pidió esperar contenido, verificar que no haya spinner activo
            if (waitForContent && el.querySelector('.animate-spin')) {
                if (attempts >= maxAttempts) {
                    // Tiempo agotado: avanzar igual (evita que el tour se quede bloqueado)
                    requestAnimationFrame(function () {
                        if (driverInstance) driverInstance.moveNext();
                    });
                    return;
                }
                attempts++;
                setTimeout(check, 150);
                return;
            }

            // Elemento listo → avanzar
            requestAnimationFrame(function () {
                if (driverInstance) driverInstance.moveNext();
            });
        };

        setTimeout(check, 100);
    },

    // Avanza DOS veces:
    //   1. Inmediatamente (muestra el paso "Cargando…")
    //   2. Cuando el elemento targetSelector existe en el DOM y no tiene spinner
    //      (el contenido ya terminó de cargarse)
    //
    // Máximo de espera: 80 intentos × 150 ms ≈ 12 segundos.
    waitForContentAndAdvance: function (targetSelector) {
        if (!driverInstance) return;

        // Avance inmediato al paso de "cargando"
        requestAnimationFrame(function () {
            if (driverInstance) driverInstance.moveNext();
        });

        var attempts = 0;
        var maxAttempts = 80;

        var check = function () {
            if (!driverInstance) return;

            var el = document.querySelector(targetSelector);

            if (!el) {
                if (attempts >= maxAttempts) return;
                attempts++;
                setTimeout(check, 150);
                return;
            }

            // Esperar a que el spinner desaparezca
            if (el.querySelector('.animate-spin')) {
                if (attempts >= maxAttempts) {
                    requestAnimationFrame(function () {
                        if (driverInstance) driverInstance.moveNext();
                    });
                    return;
                }
                attempts++;
                setTimeout(check, 150);
                return;
            }

            // Contenido listo → avanzar al paso del scheduler
            requestAnimationFrame(function () {
                if (driverInstance) driverInstance.moveNext();
            });
        };

        // Iniciar polling con un pequeño delay para que moveNext() del primer
        // avance tenga tiempo de ejecutarse antes de evaluar el DOM.
        setTimeout(check, 350);
    },

    skipTour: function (tourId) {
        if (tourId) window.DriverTour.markTourAsSeen(tourId);
        if (driverInstance) {
            driverInstance.destroy();
            driverInstance = null;
        }
    },

    destroyTour: function () {
        if (driverInstance) {
            driverInstance.destroy();
            driverInstance = null;
        }
    }
};