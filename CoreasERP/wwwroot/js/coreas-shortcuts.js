// Coreas ERP - Puente Global de Teclado, Atajos y Escáner de Códigos de Barras
window.CoreasShortcuts = {
    scannerBuffer: '',
    lastKeystrokeTime: 0,
    scannerThresholdMs: 55, // Umbral de velocidad de pistola lectora (ms entre pulsaciones)

    init: function () {
        try {
            console.log("CoreasShortcuts: Inicializando interceptor global de teclado y escáner...");
            
            window.addEventListener('keydown', function (e) {
                try {
                    if (!e || !e.key) return;
                    const now = new Date().getTime();
                    const timeDiff = now - window.CoreasShortcuts.lastKeystrokeTime;
                    window.CoreasShortcuts.lastKeystrokeTime = now;

                    // 1. Intercepción de Teclas de Función F1 a F12
                    var fMatch = typeof e.key === 'string' ? e.key.match(/^F([1-9]|1[0-2])$/) : null;
                    if (fMatch && !e.ctrlKey && !e.altKey && !e.metaKey) {
                        e.preventDefault();
                        e.stopPropagation();
                        var fNum = parseInt(fMatch[1], 10);
                        if (window.DotNet && window.DotNet.invokeMethodAsync) {
                            window.DotNet.invokeMethodAsync('CoreasERP', 'DispatchGlobalFunctionKey', fNum).catch(function () {});
                        }
                        return;
                    }

                    // 2. Intercepción de Atajos de Acción con Tecla Ctrl o Alt (Ctrl+R, Ctrl+D / Alt+D, Ctrl+G, Ctrl+P, Ctrl+B)
                    if ((e.ctrlKey || e.altKey) && !e.metaKey) {
                        var keyUpper = (e.key || '').toUpperCase();
                        var codeUpper = (e.code || '').replace(/^Key/, '').toUpperCase();
                        var matchKey = ['R', 'D', 'G', 'P', 'B'].find(function (k) { return k === keyUpper || k === codeUpper; });
                        if (matchKey) {
                            e.preventDefault();
                            e.stopPropagation();
                            if (window.DotNet && window.DotNet.invokeMethodAsync) {
                                window.DotNet.invokeMethodAsync('CoreasERP', 'DispatchGlobalShortcut', 'Ctrl+' + matchKey).catch(function () {});
                            }
                            return;
                        }
                    }

                    // 3. Intercepción de Barra Espaciadora para Buscador de Productos
                    if (e.code === 'Space' || e.key === ' ') {
                        const el = document.activeElement;
                        const tag = el && el.tagName ? el.tagName.toUpperCase() : '';
                        const hasClass = el && el.classList && typeof el.classList.contains === 'function' && el.classList.contains('trigger-spacebar-search');
                        const isWritingInInput = (tag === 'INPUT' && !['button', 'submit', 'checkbox', 'radio'].includes(el.type) && !el.readOnly && !hasClass) || tag === 'TEXTAREA';

                        // Si NO estamos redactando texto libre o estamos sobre la celda del disparador:
                        if (!isWritingInInput) {
                            e.preventDefault();
                            if (window.DotNet && window.DotNet.invokeMethodAsync) {
                                window.DotNet.invokeMethodAsync('CoreasERP', 'DispatchGlobalSpace').catch(function () {});
                            }
                            return;
                        }
                    }

                    // 4. Capturador Inteligente de Pistolas Lectoras de Códigos de Barras (Supermercado)
                    if (e.key === 'Enter') {
                        const buf = (window.CoreasShortcuts.scannerBuffer || '').trim();
                        window.CoreasShortcuts.scannerBuffer = '';

                        // Si se acumularon caracteres en ráfaga rápida (< 90ms de diferencia), fue un escaneo físico
                        if (buf.length >= 3 && timeDiff < 90) {
                            e.preventDefault();
                            e.stopPropagation();
                            if (window.DotNet && window.DotNet.invokeMethodAsync) {
                                window.DotNet.invokeMethodAsync('CoreasERP', 'DispatchBarcodeScanned', buf).catch(function () {});
                            }
                            return;
                        }

                        // Si fue una pulsación manual de Enter, notificar a .NET (cierre rápido de modal de cambio / atajos)
                        if (window.DotNet && window.DotNet.invokeMethodAsync) {
                            window.DotNet.invokeMethodAsync('CoreasERP', 'DispatchGlobalEnter').catch(function () {});
                        }
                    } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
                        // Si la tecla anterior tardó mucho tiempo, descartar buffer previo (era digitación manual)
                        if (timeDiff > window.CoreasShortcuts.scannerThresholdMs && window.CoreasShortcuts.scannerBuffer.length > 0) {
                            window.CoreasShortcuts.scannerBuffer = '';
                        }
                        window.CoreasShortcuts.scannerBuffer += e.key;
                    }
                } catch (keyErr) {
                    // Ignorar errores internos de teclado de forma silenciosa
                }
            }, true);
        } catch (initErr) {
            console.warn("CoreasShortcuts no pudo registrar listener:", initErr);
        }
    }
};

// Iniciar automáticamente al cargar la página
window.CoreasShortcuts.init();
