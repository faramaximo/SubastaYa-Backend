let hubConnection = null;
let currentSalaId = null;
let salaVersion = null;
let lastFechaFinStr = null; // Para detectar si el tiempo aumentó
let extensionTimer = null;  // Para controlar el desvanecimiento del mensaje
let serverTimeOffset = 0; // Guarda la diferencia en milisegundos

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
});

// Función central para cargar los datos
async function mostrarSala(id) {
    try {
        const resp = await fetch(`${API_BASE_URL}/api/auctions/${id}`);
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
        lastFechaFinStr = currentFechaFin; // Actualizamos el registro de la fecha




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

        const oferta = Math.max(Number(subasta.ofertaMasAlta || subasta.OfertaMasAlta || 0), Number(subasta.precioBase || subasta.PrecioBase || 0), ...pujas.map(p => Number(p.monto ?? p.Monto ?? p.MontoOferta ?? 0)));
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

        // 1. ESTADO DE LA SUBASTA (Badge)
        if (faltanParaInicio > 0) {
            badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-warning text-dark";
            badgeEstado.innerHTML = "🟡 PRÓXIMAMENTE";
            inputMonto.disabled = true;
            btnOfertar.disabled = true;
            btnOfertar.innerText = "Aún no comienza";
        } else if (faltanParaFin > 0) {
            badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-success text-white";
            badgeEstado.innerHTML = "🟢 SUBASTA EN VIVO";
            inputMonto.disabled = false;
            btnOfertar.disabled = false;
            btnOfertar.innerText = "Ofertar";
        } else {
            inputMonto.disabled = true;
            btnOfertar.disabled = true;
            btnOfertar.innerText = "Subasta Cerrada";
            if (pujas.length === 0) {
                badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-dark text-white";
                badgeEstado.innerHTML = "⚫ SUBASTA DESIERTA";
            } else {
                badgeEstado.className = "d-inline-block px-3 py-1 mb-3 fw-bold rounded-pill bg-secondary text-white";
                badgeEstado.innerHTML = "⚫ SUBASTA FINALIZADA";
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
                    estadoPersonal.innerHTML = subastaTerminada ? "¡Has ganado!" : "✓ Vas ganando";
                } else {
                    estadoPersonal.className = "mb-2 py-1 px-2 rounded border border-danger bg-danger bg-opacity-10 text-danger fw-bold text-center";
                    estadoPersonal.innerHTML = subastaTerminada ? "❌ Subasta finalizada" : `⚠ Superado (Actual: $${oferta.toFixed(2)})`;
                }
            } else {
                estadoPersonal.classList.add('d-none');
            }
        } else {
            const subastaTerminada = faltanParaFin <= 0;
            liderInfo.innerHTML = subastaTerminada ? "Ganador: <em>Nadie</em>" : "Líder: <em>Nadie aún</em>";
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
                li.className = 'list-group-item d-flex justify-content-between align-items-center bg-transparent border-light-subtle px-0';
                li.innerHTML = `<div><div class="fw-semibold">${comprador}</div><div class="text-muted" style="font-size:0.8rem">${fecha}</div></div><span class="auction-room__amount text-primary fs-5">$${montoVal.toFixed(2)}</span>`;
                historial.appendChild(li);
            });
        } else {
            historial.innerHTML = '<li class="list-group-item text-muted bg-transparent px-0 border-0">Aún no hay pujas</li>';
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

    try {
        const btn = document.getElementById('btnOfertar');
        btn.disabled = true;

        const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };
        if (salaVersion) headers['If-Match'] = salaVersion;

        const r = await fetch(`${API_BASE_URL}/api/bids`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ subastaId: parseInt(currentSalaId), monto })
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
                } else if (errorData.error || errorData.message || errorData.detail || errorData.title) {
                    errorMsg = errorData.error || errorData.message || errorData.detail || errorData.title;
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

function showToast(type, message) {
    const container = document.getElementById('toastContainer');
    const toastEl = document.createElement('div');
    toastEl.className = 'toast align-items-center text-white bg-' + (type === 'success' ? 'success' : type === 'danger' ? 'danger' : 'dark') + ' border-0';
    toastEl.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white ms-auto me-2 mt-2" data-bs-dismiss="toast"></button></div>`;
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
    const timerLabels = ['label-days', 'label-hours', 'label-minutes', 'label-seconds'];

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
        if (progressEl) progressEl.style.backgroundColor = '#dc3545';

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-dark', 'text-warning');
                el.classList.add('text-danger');
            }
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-muted', 'text-warning');
                el.classList.add('text-danger');
            }
        });

    } else if (faltanParaInicio <= 0 && faltanParaFin > 0 && faltanParaFin <= 300000) {
        // --- ESTADO AMARILLO (Menos de 5 minutos) ---
        if (alertaUltimoMinuto) {
            alertaUltimoMinuto.classList.remove('d-none', 'alert-danger');
            alertaUltimoMinuto.classList.add('alert-warning');
            alertaUltimoMinuto.innerText = 'La subasta está por finalizar';
        }
        if (progressEl) progressEl.style.backgroundColor = '#ffc107';

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-dark', 'text-danger');
                el.classList.add('text-warning');
            }
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-muted', 'text-danger');
                el.classList.add('text-warning');
            }
        });

    } else {
        // --- ESTADO NORMAL ---
        if (alertaUltimoMinuto) alertaUltimoMinuto.classList.add('d-none');
        if (progressEl) progressEl.style.backgroundColor = '#0d6efd';

        timerNumbers.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-danger', 'text-warning');
                el.classList.add('text-dark');
            }
        });
        timerLabels.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.classList.remove('text-danger', 'text-warning');
                el.classList.add('text-muted');
            }
        });
    }

    let diferenciaMs = faltanParaInicio > 0 ? faltanParaInicio : faltanParaFin;
    if (diferenciaMs < 0) diferenciaMs = 0;

    const dias = Math.floor(diferenciaMs / (1000 * 60 * 60 * 24));
    const horas = Math.floor((diferenciaMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutos = Math.floor((diferenciaMs % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferenciaMs % (1000 * 60)) / 1000);

    document.getElementById("timer-days").innerText = String(dias).padStart(2, '0');
    document.getElementById("timer-hours").innerText = String(horas).padStart(2, '0');
    document.getElementById("timer-minutes").innerText = String(minutos).padStart(2, '0');
    document.getElementById("timer-seconds").innerText = String(segundos).padStart(2, '0');

    if (progressEl) {
        if (faltanParaInicio > 0) progressEl.style.width = '0%';
        else if (faltanParaFin <= 0) progressEl.style.width = '100%';
        else {
            const dateInicio = parseApiUtcDate(inicioStr);
            const dateFin = parseApiUtcDate(finStr);
            if (dateInicio && dateFin) {
                const msInicio = dateInicio.getTime();
                const msFin = dateFin.getTime();
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
        hubConnection = new signalR.HubConnectionBuilder().withUrl(`${API_BASE_URL}/hubs/auction`).withAutomaticReconnect().build();

        hubConnection.on("NuevaPujaRegistrada", async (resultado) => {
            const idNotificado = resultado?.subastaId ?? resultado?.SubastaId;
            if (String(currentSalaId) === String(idNotificado)) {
                const extendido = resultado?.tiempoExtendido ?? resultado?.TiempoExtendido;
                const nuevaFin = resultado?.nuevaFechaFin ?? resultado?.NuevaFechaFin;
                if (nuevaFin) {
                    const timerContainer = document.getElementById('sala-detailed-timer');
                    if (timerContainer) timerContainer.setAttribute('data-fin', nuevaFin);
                }
                if (extendido) {
                    mostrarNotificacionExtension();
                }
                await mostrarSala(currentSalaId);
                actualizarTemporizador();
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