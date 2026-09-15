let hubConnection = null;
let currentSalaId = null;
let salaVersion = null;
let lastFechaFinStr = null; // Para detectar si el tiempo aumentó
let extensionTimer = null;  // Para controlar el desvanecimiento del mensaje
let serverTimeOffset = 0; // Guarda la diferencia en milisegundos
let userSaldoDisponible = null; // Almacena el saldo disponible del usuario activo

function obtenerHoraSincronizada() {
    return Date.now() + serverTimeOffset;
}

// EXTRAER EL ID DEL USUARIO DESDE EL TOKEN (Para saber quién lidera)
function getUserIdFromToken() {
    const token = localStorage.getItem('token');
    if (!token) return null;
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
            || payload.nameid
            || payload.sub
            || payload.id
            || payload.UserId
            || null;
    } catch (e) { return null; }
}

async function consultarSaldoUsuario() {
    const token = localStorage.getItem('token');
    if (!token) {
        userSaldoDisponible = null;
        return null;
    }
    try {
        const resp = await fetch(`${API_BASE_URL}/api/v1/wallets/balance`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        if (resp.ok) {
            const data = await resp.json();
            userSaldoDisponible = Number(data.saldoDisponible ?? data.SaldoDisponible ?? 0);
            return userSaldoDisponible;
        } else if (resp.status === 401) {
            userSaldoDisponible = null;
        }
    } catch (err) {
        console.error('Error al consultar saldo disponible:', err);
    }
    return userSaldoDisponible;
}

document.addEventListener("DOMContentLoaded", () => {
    if (window.updateAuthHeader) window.updateAuthHeader();

    const params = new URLSearchParams(window.location.search);
    const id = params.get('id');

    if (!id) {
        showToast('danger', 'No se especificó un ID de subasta.');
        setTimeout(() => window.location.href = '/index.html', 2000);
        return;
    }

    currentSalaId = id;
    mostrarSala(id);
    iniciarTemporizadorSalaDetallada();
    conectarSignalR(id).catch(err => console.error("Error SignalR:", err));

    // Validación preventiva de saldo al inicializar si hay token activo
    if (localStorage.getItem('token')) {
        consultarSaldoUsuario();
    }
});

// ==========================================================================
// SVG ICONS (Bootstrap Icons inline — sin dependencia externa)
// Heredan currentColor para adaptarse automáticamente al tema activo.
// ==========================================================================
const ICON = {
    clock:   `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M8 3.5a.5.5 0 0 0-1 0V9a.5.5 0 0 0 .252.434l3.5 2a.5.5 0 0 0 .496-.868L8 8.71V3.5z"/><path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm7-8A7 7 0 1 1 1 8a7 7 0 0 1 14 0z"/></svg>`,
    live:    `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M3.05 3.05a7 7 0 0 0 0 9.9.5.5 0 0 1-.707.707 8 8 0 0 1 0-11.314.5.5 0 0 1 .707.707zm2.122 2.122a4 4 0 0 0 0 5.656.5.5 0 1 1-.708.708 5 5 0 0 1 0-7.072.5.5 0 0 1 .708.708zm5.656-.708a.5.5 0 0 1 .708 0 5 5 0 0 1 0 7.072.5.5 0 1 1-.708-.708 4 4 0 0 0 0-5.656.5.5 0 0 1 0-.708zm2.122-2.12a.5.5 0 0 1 .707 0 8 8 0 0 1 0 11.313.5.5 0 0 1-.707-.707 7 7 0 0 0 0-9.9.5.5 0 0 1 0-.707zM10 8a2 2 0 1 1-4 0 2 2 0 0 1 4 0z"/></svg>`,
    ban:     `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M15 8a6.973 6.973 0 0 0-1.597-4.384l-9.79 9.79A7 7 0 0 0 15 8zM2.595 12.382l9.79-9.79a7 7 0 0 0-9.79 9.79zM8 1a7 7 0 1 1 0 14A7 7 0 0 1 8 1z"/></svg>`,
    check:   `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/></svg>`,
    trophy:  `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M2.5.5A.5.5 0 0 1 3 0h10a.5.5 0 0 1 .5.5c0 .538-.012 1.05-.034 1.536a3 3 0 1 1-1.133 5.89c-.79 1.865-1.878 2.777-2.833 3.011v2.173l1.425.356c.194.048.377.135.537.255L13.3 15.1a.5.5 0 0 1-.3.9H3a.5.5 0 0 1-.3-.9l1.838-1.379c.16-.12.343-.207.537-.255L6.5 13.11v-2.173c-.955-.234-2.043-1.146-2.833-3.012a3 3 0 1 1-1.132-5.89A33.076 33.076 0 0 1 2.5.5zm.099 2.54a2 2 0 0 0 .72 3.935c-.333-1.05-.588-2.346-.72-3.935zm10.083 3.935a2 2 0 0 0 .72-3.935c-.133 1.59-.388 2.885-.72 3.935z"/></svg>`,
    xCircle: `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/></svg>`,
    warning: `<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" style="vertical-align:-.1em;margin-right:4px"><path d="M7.938 2.016A.13.13 0 0 1 8.002 2a.13.13 0 0 1 .063.016.146.146 0 0 1 .054.057l6.857 11.667c.036.06.035.124.002.183a.163.163 0 0 1-.054.06.116.116 0 0 1-.066.017H1.146a.115.115 0 0 1-.066-.017.163.163 0 0 1-.054-.06.176.176 0 0 1 .002-.183L7.884 2.073a.147.147 0 0 1 .054-.057zm1.044-.45a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566z"/><path d="M7.002 12a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 5.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995z"/></svg>`,
};

// Función central para cargar los datos
async function mostrarSala(id) {
    try {
        const resp = await fetch(`${API_BASE_URL}/api/v1/auctions/${id}`);
        if (!resp.ok) throw new Error('No se pudo cargar la subasta');
        const subasta = await resp.json();

        const horaServidorStr = subasta.horaServidor || subasta.HoraServidor;
        if (horaServidorStr) {
            const msServidor = new Date(horaServidorStr.endsWith('Z') ? horaServidorStr : horaServidorStr + 'Z').getTime();
            serverTimeOffset = msServidor - Date.now();
        }

        salaVersion = resp.headers.get('ETag') || subasta.version || subasta.Version || null;

        const currentFechaFin = subasta.fechaFin || subasta.FechaFin;
        if (lastFechaFinStr && currentFechaFin) {
            const oldTime = parseApiUtcDate(lastFechaFinStr)?.getTime() || 0;
            const newTime = parseApiUtcDate(currentFechaFin)?.getTime() || 0;
            // Si la nueva fecha de fin aumentó (regla anti-sniping extiende +2 min = 120.000 ms)
            if (newTime - oldTime >= 10000) {
                mostrarNotificacionExtension();
            }
        }
        lastFechaFinStr = currentFechaFin;

        document.getElementById('salaTitulo').innerText = subasta.titulo || 'Sin Título';
        document.getElementById('salaDescripcion').innerText = subasta.descripcion || '';

        const salaImagen = document.getElementById('salaImagen');
        salaImagen.src = subasta.urlImagen || '';
        salaImagen.onerror = () => { salaImagen.src = '/img/placeholder.jpg'; };

        const pujas = Array.isArray(subasta.pujas || subasta.Pujas) ? (subasta.pujas || subasta.Pujas) : [];

        // ORDENAR PUJAS DE MAYOR A MENOR
        pujas.sort((a, b) => {
            const montoA = a.monto ?? a.Monto ?? a.MontoOferta ?? 0;
            const montoB = b.monto ?? b.Monto ?? b.MontoOferta ?? 0;
            return montoB - montoA;
        });

        const oferta = Math.max(
            Number(subasta.ofertaMasAlta || subasta.OfertaMasAlta || 0),
            Number(subasta.precioBase || subasta.PrecioBase || 0),
            ...pujas.map(p => Number(p.monto ?? p.Monto ?? p.MontoOferta ?? 0))
        );
        const incremento = Math.max(Number(subasta.incrementoMinimo || subasta.IncrementoMinimo || 0), 0.01);
        const pujaMinima = oferta + incremento;

        document.getElementById('salaOferta').innerText = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(oferta);
        document.getElementById('salaMinima').innerText = `Puja mínima: $${pujaMinima.toFixed(2)} (incremento: $${incremento.toFixed(2)})`;

        // ==========================================
        // LÓGICA DE ESTADO, LÍDER Y SITUACIÓN PERSONAL
        // ==========================================
        const myUserId = getUserIdFromToken();
        const faltanParaInicio = calcularDiferenciaTiempo(subasta.fechaInicio);
        const faltanParaFin = calcularDiferenciaTiempo(subasta.fechaFin);

        const badgeEstado = document.getElementById('salaEstadoDistintivo');
        const liderInfo = document.getElementById('liderInfo');
        const estadoPersonal = document.getElementById('estadoPersonalPuja');
        const inputMonto = document.getElementById('montoPuja');
        const btnOfertar = document.getElementById('btnOfertar');

        // 1. ESTADO DE LA SUBASTA (Badge con SVG icon)
        if (faltanParaInicio > 0) {
            badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-warning text-dark";
            badgeEstado.innerHTML = `${ICON.clock} PRÓXIMAMENTE`;
            inputMonto.disabled = true;
            btnOfertar.disabled = true;
            btnOfertar.innerText = "Aún no comienza";
        } else if (faltanParaFin > 0) {
            badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-success text-white";
            badgeEstado.innerHTML = `${ICON.live} SUBASTA EN VIVO`;
            inputMonto.disabled = false;
            btnOfertar.disabled = false;
            btnOfertar.innerText = "Ofertar";
        } else {
            inputMonto.disabled = true;
            btnOfertar.disabled = true;
            btnOfertar.innerText = "Subasta Cerrada";
            if (pujas.length === 0) {
                badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill sala-badge-desierta";
                badgeEstado.innerHTML = `${ICON.ban} SUBASTA DESIERTA`;
            } else {
                badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill sala-badge-finalizada";
                badgeEstado.innerHTML = `${ICON.check} SUBASTA FINALIZADA`;
            }
        }

        // 2. LÍDER Y ESTADO PERSONAL
        if (pujas.length > 0) {
            const lider = pujas[0];
            const liderId = String(lider.compradorId || lider.usuarioId);
            const liderNombre = lider.compradorNombre || lider.comprador || lider.usuarioNombre || "Usuario Anónimo";
            const subastaTerminada = faltanParaFin <= 0;

            if (myUserId && liderId === String(myUserId)) {
                liderInfo.innerHTML = `${subastaTerminada ? 'Ganador' : 'Líder'}: <strong class='text-success'>Vos</strong>`;
            } else {
                liderInfo.innerHTML = `${subastaTerminada ? 'Ganador' : 'Líder'}: <strong>${liderNombre}</strong>`;
            }

            const participe = myUserId ? pujas.some(p => String(p.compradorId || p.usuarioId) === String(myUserId)) : false;

            if (participe) {
                estadoPersonal.classList.remove('d-none');
                if (liderId === String(myUserId)) {
                    estadoPersonal.className = "mb-2 py-1 px-2 rounded border border-success bg-success bg-opacity-10 text-success fw-bold text-center";
                    estadoPersonal.innerHTML = subastaTerminada ? `${ICON.trophy} ¡Has ganado!` : `${ICON.trophy} Vas ganando`;
                } else {
                    estadoPersonal.className = "mb-2 py-1 px-2 rounded border border-danger bg-danger bg-opacity-10 text-danger fw-bold text-center";
                    estadoPersonal.innerHTML = subastaTerminada ? `${ICON.xCircle} Subasta finalizada` : `${ICON.warning} Superado (Actual: $${oferta.toFixed(2)})`;
                }
            } else {
                estadoPersonal.classList.add('d-none');
            }
        } else {
            const subastaTerminada = faltanParaFin <= 0;
            liderInfo.innerHTML = subastaTerminada ? "<span class='text-secondary fw-normal'>Subasta finalizada sin ofertas</span>" : "Líder: <em>Nadie aún</em>";
            estadoPersonal.classList.add('d-none');
        }

        // Temporizador y fecha superior
        const timerContainer = document.getElementById('sala-detailed-timer');
        if (timerContainer) {
            timerContainer.setAttribute('data-inicio', subasta.fechaInicio || '');
            timerContainer.setAttribute('data-fin', subasta.fechaFin || '');

            const fechaFinDate = parseApiUtcDate(subasta.fechaFin);
            if (fechaFinDate) {
                const opciones = { weekday: 'long', hour: '2-digit', minute: '2-digit' };
                let textoFecha = new Intl.DateTimeFormat('es-AR', opciones).format(fechaFinDate);
                document.getElementById('fechaFinTexto').innerText = textoFecha.charAt(0).toUpperCase() + textoFecha.slice(1);
            }
        }

        // Historial
        pujas.sort((a, b) => (parseApiUtcDate(b.fechaPuja || b.fecha)?.getTime() || 0) - (parseApiUtcDate(a.fechaPuja || a.fecha)?.getTime() || 0));

        const historial = document.getElementById('historialPujas');
        historial.innerHTML = '';
        if (pujas.length > 0) {
            pujas.forEach(p => {
                const comprador = p.compradorNombre || p.comprador || p.usuarioNombre || `Usuario #${p.compradorId || p.usuarioId || '—'}`;
                const montoVal = p.monto ?? p.Monto ?? p.MontoOferta ?? 0;
                const fecha = formatLocalDateTime(p.fechaPuja || p.fecha);

                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center bg-transparent px-0';
                li.innerHTML = `<div><div class="fw-semibold">${comprador}</div><div class="sala-bid-hint">${fecha}</div></div><span class="auction-room__amount fs-5">$${montoVal.toFixed(2)}</span>`;
                historial.appendChild(li);
            });
        } else {
            historial.innerHTML = '<li class="list-group-item bg-transparent px-0 border-0 sala-minimum-info">Aún no hay pujas</li>';
        }

        document.getElementById('contadorPujas').textContent = `${pujas.length} ${pujas.length === 1 ? 'puja' : 'pujas'}`;

        inputMonto.min = pujaMinima.toFixed(2);
        inputMonto.placeholder = pujaMinima.toFixed(2);
        if (document.activeElement !== inputMonto) inputMonto.value = pujaMinima.toFixed(2);
        document.getElementById('pujaAyuda').textContent = `Ingresá un monto igual o superior a $${pujaMinima.toFixed(2)}.`;

    } catch (error) {
        console.error('Error al cargar sala:', error);
        showToast('danger', 'Error al cargar la subasta.');
    }
}

// Ofertador
document.getElementById('btnOfertar').onclick = async () => {
    const inputMonto = document.getElementById('montoPuja');
    const monto = parseFloat(inputMonto.value);
    const minMonto = parseFloat(inputMonto.min);

    if (!monto || monto < minMonto) return showToast('warning', `La puja mínima es $${minMonto.toFixed(2)}.`);

    const token = localStorage.getItem('token');
    if (!token) {
        showToast('warning', 'Debés iniciar sesión para poder pujar.');
        if (window.openAuthModal) {
            window.openAuthModal('login');
        } else {
            setTimeout(() => window.location.href = '/pages/login.html', 1500);
        }
        return;
    }

    // Validación preventiva de saldo disponible
    if (userSaldoDisponible === null) {
        await consultarSaldoUsuario();
    }

    if (userSaldoDisponible !== null && monto > userSaldoDisponible) {
        showToast('danger', `Saldo insuficiente en billetera ($${userSaldoDisponible.toFixed(2)} disponibles)`);
        return;
    }

    try {
        const btn = document.getElementById('btnOfertar');
        btn.disabled = true;

        const headers = {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + localStorage.getItem('token')
        };
        if (salaVersion) headers['If-Match'] = salaVersion;

        const r = await fetch(`${API_BASE_URL}/api/v1/auctions/${currentSalaId}/bids`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ monto })
        });

        if (r.status === 409 || r.status === 412) {
            await mostrarSala(currentSalaId);
            showToast('warning', 'La oferta cambió mientras pujabas. Revisá el nuevo mínimo.');
            return;
        }

        if (!r.ok) {
            let errorMsg = 'Error al realizar la puja.';
            try {
                const errorData = await r.json();
                if (errorData.errors) {
                    const firstErrorKey = Object.keys(errorData.errors)[0];
                    errorMsg = errorData.errors[firstErrorKey][0];
                } else if (errorData.detail || errorData.message || errorData.title || errorData.error) {
                    // RFC 7807 / ProblemDetails: el texto específico para el cliente es "detail".
                    errorMsg = errorData.detail || errorData.message || errorData.title || errorData.error;
                } else if (typeof errorData === 'string') {
                    errorMsg = errorData;
                }
            } catch (jsonError) {
                const textError = await r.text().catch(() => null);
                if (textError) errorMsg = textError;
            }
            throw new Error(errorMsg);
        }

        const resultado = await r.json();
        showToast('success', '¡Puja realizada con éxito!');

        // Actualizar saldo disponible tras la puja
        await consultarSaldoUsuario();

        const extendido = resultado?.tiempoExtendido ?? resultado?.TiempoExtendido;
        const nuevaFechaFin = resultado?.nuevaFechaFin || resultado?.NuevaFechaFin;
        if (nuevaFechaFin) {
            const timerContainer = document.getElementById('sala-detailed-timer');
            if (timerContainer) timerContainer.setAttribute('data-fin', nuevaFechaFin);
        }
        if (extendido) {
            mostrarNotificacionExtension();
        }
        await mostrarSala(currentSalaId);
        actualizarTemporizador();
    } catch (err) {
        showToast('danger', err.message);
    } finally {
        document.getElementById('btnOfertar').disabled = false;
    }
};

// Utils de tiempo y UI
function parseApiUtcDate(value) {
    if (!value) return null;
    const raw = String(value).trim();
    const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(raw);
    const date = new Date(hasTimezone ? raw : `${raw}Z`);
    return Number.isNaN(date.getTime()) ? null : date;
}

function calcularDiferenciaTiempo(fechaStr) {
    const date = parseApiUtcDate(fechaStr);
    if (!date) return 0;
    return date.getTime() - obtenerHoraSincronizada();
}

function formatLocalDateTime(value) {
    const date = parseApiUtcDate(value);
    if (!date) return "Fecha no disponible";
    return new Intl.DateTimeFormat("es-AR", { dateStyle: "short", timeStyle: "short" }).format(date);
}

let toastSequence = 0;

function showToast(type, message) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toastEl = document.createElement('div');
    const toastId = window.crypto?.randomUUID?.() ?? `toast-${Date.now()}-${++toastSequence}`;
    toastEl.id = toastId;
    toastEl.className = 'toast align-items-center text-white bg-' + (type === 'success' ? 'success' : type === 'danger' ? 'danger' : 'dark') + ' border-0';

    const content = document.createElement('div');
    content.className = 'd-flex';
    const body = document.createElement('div');
    body.className = 'toast-body';
    body.textContent = message || 'Error al procesar la oferta';
    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'btn-close btn-close-white ms-auto me-2 mt-2';
    closeButton.dataset.bsDismiss = 'toast';
    closeButton.setAttribute('aria-label', 'Cerrar');
    content.append(body, closeButton);
    toastEl.appendChild(content);
    container.appendChild(toastEl);
    new bootstrap.Toast(toastEl, { delay: 4000 }).show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

function actualizarTemporizador() {
    const timerContainer = document.getElementById("sala-detailed-timer");
    if (!timerContainer) return;

    const inicioStr = timerContainer.getAttribute("data-inicio");
    const finStr = timerContainer.getAttribute("data-fin");
    if (!inicioStr || !finStr) return;

    const faltanParaInicio = calcularDiferenciaTiempo(inicioStr);
    const faltanParaFin = calcularDiferenciaTiempo(finStr);

    if (faltanParaInicio <= 0 && faltanParaInicio > -1500) { mostrarSala(currentSalaId); }
    if (faltanParaFin <= 0 && faltanParaFin > -1500) { mostrarSala(currentSalaId); }

    const alertaUltimoMinuto = document.getElementById("alertaUltimoMinuto");
    const progressEl = document.getElementById("timer-progress");

    const timerNumbers = ['timer-days', 'timer-hours', 'timer-minutes', 'timer-seconds'];
    const timerLabels  = ['label-days',  'label-hours',  'label-minutes',  'label-seconds'];

    // ==========================================
    // ALERTAS DE TIEMPO (5 MINUTOS Y 1 MINUTO)
    // ==========================================
    if (faltanParaInicio <= 0 && faltanParaFin > 0 && faltanParaFin <= 60000) {
        // --- ESTADO ROJO (Menos de 1 minuto) ---
        if (alertaUltimoMinuto) {
            alertaUltimoMinuto.classList.remove('d-none', 'alert-warning');
            alertaUltimoMinuto.classList.add('alert-danger');
            alertaUltimoMinuto.innerText = '¡Último minuto! Las pujas pueden extender la subasta.';
        }
        if (progressEl) progressEl.style.backgroundColor = 'var(--bs-danger, #dc3545)';

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = 'var(--bs-danger, #dc3545)'; }
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = 'var(--bs-danger, #dc3545)'; }
        });

    } else if (faltanParaInicio <= 0 && faltanParaFin > 0 && faltanParaFin <= 300000) {
        // --- ESTADO AMARILLO (Menos de 5 minutos) ---
        if (alertaUltimoMinuto) {
            alertaUltimoMinuto.classList.remove('d-none', 'alert-danger');
            alertaUltimoMinuto.classList.add('alert-warning');
            alertaUltimoMinuto.innerText = 'La subasta está por finalizar';
        }
        if (progressEl) progressEl.style.backgroundColor = 'var(--bs-warning, #ffc107)';

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = 'var(--bs-warning, #ffc107)'; }
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = 'var(--bs-warning, #ffc107)'; }
        });

    } else {
        // --- ESTADO NORMAL: hereda del tema activo ---
        if (alertaUltimoMinuto) alertaUltimoMinuto.classList.add('d-none');
        if (progressEl) progressEl.style.backgroundColor = '';  // Deja que lo maneje sala.css via --accent-color

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = ''; }  // Hereda var(--text-principal) del CSS
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.style.color = ''; }  // Hereda var(--text-secundario) via .sala-timer-label
        });
    }

    let diferenciaMs = faltanParaInicio > 0 ? faltanParaInicio : faltanParaFin;
    if (diferenciaMs < 0) diferenciaMs = 0;

    const dias     = Math.floor(diferenciaMs / (1000 * 60 * 60 * 24));
    const horas    = Math.floor((diferenciaMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutos  = Math.floor((diferenciaMs % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferenciaMs % (1000 * 60)) / 1000);

    document.getElementById("timer-days").innerText    = String(dias).padStart(2, '0');
    document.getElementById("timer-hours").innerText   = String(horas).padStart(2, '0');
    document.getElementById("timer-minutes").innerText = String(minutos).padStart(2, '0');
    document.getElementById("timer-seconds").innerText = String(segundos).padStart(2, '0');

    if (progressEl) {
        if (faltanParaInicio > 0) progressEl.style.width = '0%';
        else if (faltanParaFin <= 0) progressEl.style.width = '100%';
        else {
            const dateInicio = parseApiUtcDate(inicioStr);
            const dateFin    = parseApiUtcDate(finStr);
            if (dateInicio && dateFin) {
                const msInicio = dateInicio.getTime();
                const msFin    = dateFin.getTime();
                let porcentaje = ((obtenerHoraSincronizada() - msInicio) / (msFin - msInicio)) * 100;
                progressEl.style.width = `${Math.min(100, Math.max(0, porcentaje))}%`;
            }
        }
    }
}

function iniciarTemporizadorSalaDetallada() {
    actualizarTemporizador();
    setInterval(actualizarTemporizador, 1000);
}

async function conectarSignalR(subastaId) {
    if (!hubConnection) {
        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/auction`)
            .withAutomaticReconnect()
            .build();

        hubConnection.on("NuevaPujaRegistrada", async (resultado) => {
            const idNotificado = resultado?.subastaId ?? resultado?.SubastaId;
            if (String(currentSalaId) === String(idNotificado)) {
                const extendido = resultado?.tiempoExtendido ?? resultado?.TiempoExtendido;
                const nuevaFin  = resultado?.nuevaFechaFin ?? resultado?.NuevaFechaFin;
                if (nuevaFin) {
                    const timerContainer = document.getElementById('sala-detailed-timer');
                    if (timerContainer) timerContainer.setAttribute('data-fin', nuevaFin);
                }
                if (extendido) { mostrarNotificacionExtension(); }
                await mostrarSala(currentSalaId);
                actualizarTemporizador();
                if (localStorage.getItem('token')) {
                    consultarSaldoUsuario();
                }
            }
        });

        hubConnection.on("SubastaCerrada", async (data) => {
            const id = data?.subastaId ?? data?.SubastaId;
            if (String(currentSalaId) === String(id)) {
                await mostrarSala(currentSalaId);
                actualizarTemporizador();
            }
        });

        hubConnection.on("SubastaDesierta", async (subastaId) => {
            if (String(currentSalaId) === String(subastaId)) {
                await mostrarSala(currentSalaId);
                actualizarTemporizador();
            }
        });

        hubConnection.on("SubastaIniciada", async (subastaId) => {
            if (String(currentSalaId) === String(subastaId)) {
                await mostrarSala(currentSalaId);
                actualizarTemporizador();
            }
        });

        hubConnection.onreconnected(async () => {
            if (currentSalaId) await hubConnection.invoke("UnirseASubasta", String(currentSalaId));
        });

        await hubConnection.start();
        if (hubConnection.state === signalR.HubConnectionState.Connected) {
            await hubConnection.invoke("UnirseASubasta", String(subastaId));
        }
    }
}

function mostrarNotificacionExtension() {
    const alerta = document.getElementById('alertaExtensionTiempo');
    if (!alerta) return;

    // Si ya había un timer corriendo porque hicieron pujas seguidas, lo reiniciamos
    if (extensionTimer) clearTimeout(extensionTimer);

    // Mostramos la alerta con animación suave
    alerta.classList.remove('d-none');
    void alerta.offsetWidth; // Forzar reflow para animación CSS
    alerta.style.opacity = '1';

    // A los 5 segundos la ocultamos suavemente
    extensionTimer = setTimeout(() => {
        alerta.style.opacity = '0';
        setTimeout(() => {
            alerta.classList.add('d-none');
        }, 500); // Espera a que termine el fade out
    }, 5000);
}
