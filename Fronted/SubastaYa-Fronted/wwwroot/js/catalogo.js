let hubConnection = null;
let hubConnectionStartPromise = null;
let currentSalaId = null;
let salaRefreshId = null;
let salaVersion = null;

// Las fechas del API se generan en UTC. Si una respuesta ISO no incluye zona,
// se debe interpretar como UTC antes de formatearla para la zona del navegador.
function parseApiUtcDate(value) {
    if (!value) return null;
    const raw = String(value).trim();
    const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(raw);
    const date = new Date(hasTimezone ? raw : `${raw}Z`);
    return Number.isNaN(date.getTime()) ? null : date;
}

function formatLocalDateTime(value) {
    const date = parseApiUtcDate(value);
    if (!date) return "Fecha no disponible";
    return new Intl.DateTimeFormat("es-AR", {
        dateStyle: "short",
        timeStyle: "short"
    }).format(date);
}

document.addEventListener("DOMContentLoaded", () => {
    configurarEventosFiltros();
    obtenerCatalogo();
    conectarSignalR().catch(error => console.error("No se pudo conectar al tiempo real:", error));
    iniciarTemporizadorGlobal();
    iniciarTemporizadorSalaDetallada();
    // Asegurar vista por defecto: catálogo visible, sala oculta
    const salaEl = document.getElementById('sala-view');
    const catalogEl = document.getElementById('catalogo-view') || document.getElementById('catalog-view');
    if (salaEl) salaEl.classList.add('d-none');
    if (catalogEl) catalogEl.classList.remove('d-none');
    // QUÉ HACE: refresca las acciones de cuenta de la cabecera.
    // POR QUÉ: la sesión puede cambiar sin recargar el catálogo.
    if (window.updateAuthHeader) window.updateAuthHeader();
});

// ==========================================
// 1. CONFIGURACIÓN DE EVENTOS (Blindada)
// ==========================================
function configurarEventosFiltros() {
    const idsFiltros = ["filtroEstado", "filtroCategoria", "precioMin", "precioMax", "buscadorGeneral"];
    
    // Escudo: Solo agregamos el evento si el filtro existe en esta pantalla
    idsFiltros.forEach(id => {
        const elemento = document.getElementById(id);
        if (elemento) {
            const evento = elemento.tagName === "SELECT" ? "change" : "input";
            elemento.addEventListener(evento, obtenerCatalogo);
        }
    });

    document.querySelectorAll(".sort-pill").forEach(pill => {
        pill.addEventListener("click", (e) => {
            document.querySelectorAll(".sort-pill").forEach(p => p.classList.remove("active"));
            e.target.classList.add("active");
            obtenerCatalogo();
        });
    });

    const btnLimpiar = document.getElementById("btnLimpiar");
    if (btnLimpiar) btnLimpiar.addEventListener("click", limpiarFiltros);
}

function limpiarFiltros() {
    const setVal = (id, val) => { if(document.getElementById(id)) document.getElementById(id).value = val; };
    
    setVal("filtroEstado", "todos");
    setVal("filtroCategoria", "todas");
    setVal("precioMin", "");
    setVal("precioMax", "");
    setVal("buscadorGeneral", "");
    
    document.querySelectorAll(".sort-pill").forEach(p => p.classList.remove("active"));
    const btnTiempo = document.querySelector('[data-sort="menor-tiempo"]');
    if (btnTiempo) btnTiempo.classList.add("active");

    obtenerCatalogo();
}

// ==========================================
// 2. COMUNICACIÓN CON LA API
// ==========================================
async function obtenerCatalogo() {
    const url = construirUrlConFiltros();

    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error("Error al consultar el servidor");
        
        const subastas = await response.json();
        procesarYRenderizarSubastas(subastas);
    } catch (error) {
        console.error("Error de conexión:", error);
        const grid = document.getElementById("gridSubastas");
        if (grid) grid.innerHTML = `<div class="col-12 text-center text-danger py-5">Error de red. Verificá que el backend de C# esté corriendo.</div>`;
    }
}

function construirUrlConFiltros() {
    const getVal = (id) => document.getElementById(id) ? document.getElementById(id).value : "";

    const estado = getVal("filtroEstado");
    const categoria = getVal("filtroCategoria");
    const precioMin = getVal("precioMin");
    const precioMax = getVal("precioMax");
    const busqueda = getVal("buscadorGeneral");

    const ordenActivo = document.querySelector(".sort-pill.active");
    const orden = ordenActivo ? ordenActivo.getAttribute("data-sort") : "menor-tiempo";

    let url = `${API_BASE_URL}/api/v1/auctions?orderBy=${orden}`;

    if (estado && estado !== "todos") url += `&estado=${estado}`;
    if (categoria && categoria !== "todas") url += `&categoriaId=${categoria}`;
    if (precioMin) url += `&precioMin=${precioMin}`;
    if (precioMax) url += `&precioMax=${precioMax}`;
    if (busqueda) url += `&busqueda=${encodeURIComponent(busqueda)}`;

    return url;
}

// ==========================================
// 3. LÓGICA DE RENDERIZADO (El Dibujante)
// ==========================================
function procesarYRenderizarSubastas(subastas) {
    const grid = document.getElementById("gridSubastas");
    const featuredContainer = document.getElementById("featuredContainer");
    
    if (!grid || !featuredContainer) return; 

    grid.innerHTML = "";
    featuredContainer.innerHTML = "";

    if (!Array.isArray(subastas) || subastas.length === 0) {
        grid.innerHTML = `<div class="col-12 text-center text-muted py-5">No se encontraron subastas.</div>`;
        return;
    }

    // Buscamos las Activas (Estado === 1) para destacar la que venza más rápido
    let subastasActivas = subastas.filter(s => s.estado === 1 && calcularDiferenciaTiempo(s.fechaFin) > 0);
    
    if (subastasActivas.length > 0) {
        subastasActivas.sort((a, b) => calcularDiferenciaTiempo(a.fechaFin) - calcularDiferenciaTiempo(b.fechaFin));
        renderizarDestacada(subastasActivas[0], featuredContainer);
        subastas = subastas.filter(s => s.id !== subastasActivas[0].id);
    }

    subastas.forEach(subasta => renderizarTarjeta(subasta, grid));
}

function renderizarTarjeta(subasta, contenedor) {
    const inicioStr = subasta.fechaInicio || "";
    const finStr = subasta.fechaFin || "";

    contenedor.innerHTML += `
        <div class="col-12 col-sm-6 col-lg-3">
            <div class="auction-card">
                <div class="card-img-container">
                    <span class="category-badge">${subasta.categoriaNombre || 'Categoría'}</span>
                    <img src="${subasta.urlImagen}" alt="${subasta.titulo}">
                </div>
                <div class="card-body">
                    <h5 class="card-title">${subasta.titulo}</h5>
                    <div class="card-info-row">
                        <div>
                            <span class="info-label">Oferta Actual</span>
                            <span class="info-value">$${subasta.ofertaMasAlta || subasta.precioBase}</span>
                        </div>
                        <div class="text-end">
                            <span class="info-label dynamic-label">Calculando...</span>
                            <span class="info-value live-timer" data-inicio="${inicioStr}" data-fin="${finStr}">
                                --:--:--
                            </span>
                        </div>
                    </div>
                    <a href="/pages/sala.html?id=${subasta.id}" class="btn btn-primary w-100 mt-3">Ver Sala</a>
                </div>
            </div>
        </div>
    `;
}

// Delegación de eventos: abrir sala al hacer click en cualquier botón .ver-sala


// Botón volver al catálogo: obtiene el estado actualizado antes de mostrarlo.




// Toast helper (Bootstrap)
function showToast(type, message, title) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toastId = 'toast-' + Date.now();
    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center text-bg-transparent border-0`;
    toastEl.id = toastId;
    toastEl.role = 'alert';
    toastEl.ariaLive = 'assertive';
    toastEl.ariaAtomic = 'true';

    const bgClass = type === 'success' ? 'toast-custom-success' : (type === 'danger' ? 'toast-custom-danger' : 'bg-dark text-white');

    toastEl.innerHTML = `
        <div class="d-flex ${bgClass}" style="padding:12px;border-radius:8px;min-width:240px;">
            <div class="toast-body">${title ? `<strong>${title}</strong><br/>` : ''}${message}</div>
            <button type="button" class="btn-close btn-close-white ms-auto me-2" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    container.appendChild(toastEl);
    const bsToast = new bootstrap.Toast(toastEl, { delay: 4000 });
    bsToast.show();
    // Remover después de ocultarse
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

function renderizarDestacada(subasta, contenedor) {
    contenedor.innerHTML = `
        <div class="featured-card">
            <div class="featured-img-container">
                <img src="${subasta.urlImagen}" alt="${subasta.titulo}">
            </div>
            <div class="featured-details">
                <span class="featured-badge">Cierra pronto</span>
                <h2 class="featured-title">${subasta.titulo}</h2>
                <div class="featured-info">
                    <div>
                        <span class="f-label">Oferta Actual</span>
                        <span class="f-value">$${subasta.ofertaMasAlta || subasta.precioBase}</span>
                    </div>
                    <div>
                        <span class="f-label dynamic-label">Calculando...</span>
                        <span class="f-value f-timer live-timer timer-highlight" data-inicio="${subasta.fechaInicio || ''}" data-fin="${subasta.fechaFin || ''}">
                            --:--:--
                        </span>
                        <!-- Contenedor oculto para la duración. -->
                        <div class="duracion-subasta mt-1" style="font-size: 0.85rem; color: #a29bfe; display: none; font-weight: 600;"></div>
                    </div>
                </div>
               <a href="/pages/sala.html?id=${subasta.id}" class="btn btn-primary btn-lg px-5 py-3 rounded-pill fw-bold">Ofertar Ahora</a>
            </div>
        </div>
    `;
}

// ==========================================
// 4. MANEJO DEL TIEMPO EN TIEMPO REAL
// ==========================================
function calcularDiferenciaTiempo(fechaStr) {
    if (!fechaStr) return 0;
    const fechaUtcSegura = fechaStr.endsWith('Z') ? fechaStr : fechaStr + 'Z';
    return new Date(fechaUtcSegura).getTime() - new Date().getTime();
}

function formatearFechaRestante(diferenciaMs) {
    if (diferenciaMs <= 0) return "0h 00m 00s";
    
    const dias = Math.floor(diferenciaMs / (1000 * 60 * 60 * 24));
    const horas = Math.floor((diferenciaMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutos = Math.floor((diferenciaMs % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferenciaMs % (1000 * 60)) / 1000);

    if (dias > 0) {
        return `${dias}d ${String(horas).padStart(2, '0')}h ${String(minutos).padStart(2, '0')}m`;
    }

    return `${horas}h ${String(minutos).padStart(2, '0')}m ${String(segundos).padStart(2, '0')}s`;
}

async function conectarSignalR() {
    if (!hubConnection) {
        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/auction`)
            .withAutomaticReconnect()
            .build();

        // Todas las pestañas reciben este evento para mantener el catálogo sincronizado.
        hubConnection.on("SubastaActualizada", async () => {
            const catalogo = document.getElementById('catalogo-view') || document.getElementById('catalog-view');
            if (catalogo && !catalogo.classList.contains('d-none')) {
                await obtenerCatalogo();
            }
        });

        // Solo quienes están dentro de la sala reciben el detalle para refrescarla al instante.
        hubConnection.on("NuevaPujaRegistrada", async (resultado) => {
            const subastaId = resultado?.subastaId ?? resultado?.SubastaId;
            const sala = document.getElementById('sala-view');
            if (String(currentSalaId) === String(subastaId) && sala && !sala.classList.contains('d-none')) {
                await mostrarSala(subastaId);
            }
        });
        // NUEVO: Escuchar cuando la subasta termina con un ganador
        hubConnection.on("SubastaCerrada", async (datos) => {
            const subastaId = datos?.subastaId ?? datos?.SubastaId;
            const monto = datos?.montoFinal ?? datos?.MontoFinal;
            const sala = document.getElementById('sala-view');

            // Solo disparamos el toast visual y el refresco si el usuario está ADENTRO de esta sala
            if (String(currentSalaId) === String(subastaId) && sala && !sala.classList.contains('d-none')) {
                showToast('success', `La subasta ha finalizado. Monto ganador: $${monto}`, '¡Subasta Cerrada!');
                // Asumiendo que tienes una función mostrarSala(id), esto refrescará la UI
                // para que desaparezca el botón de pujar y se muestre el ganador
                if (typeof mostrarSala === 'function') await mostrarSala(subastaId);
            }
        });

        // NUEVO: Escuchar cuando la subasta termina sin pujas
        hubConnection.on("SubastaDesierta", async (subastaId) => {
            const sala = document.getElementById('sala-view');

            if (String(currentSalaId) === String(subastaId) && sala && !sala.classList.contains('d-none')) {
                showToast('warning', `El tiempo finalizó sin que nadie hiciera una oferta.`, 'Subasta Desierta');
                if (typeof mostrarSala === 'function') await mostrarSala(subastaId);
            }
        });

        // Al reconectar, SignalR asigna una conexión nueva y hay que volver a unirse al grupo.
        hubConnection.onreconnected(async () => {
            if (currentSalaId !== null) await unirseASubasta(currentSalaId);
        });

        hubConnectionStartPromise = hubConnection.start().catch(error => {
            hubConnection = null;
            hubConnectionStartPromise = null;
            throw error;
        });
    }

    await hubConnectionStartPromise;
    return hubConnection;
}

async function unirseASubasta(subastaId) {
    const conexion = await conectarSignalR();
    if (conexion.state === signalR.HubConnectionState.Connected) {
        await conexion.invoke("UnirseASubasta", String(subastaId));
    }
}

function iniciarTemporizadorGlobal() {
    setInterval(() => {
        document.querySelectorAll(".live-timer").forEach(timer => {
            const inicioStr = timer.getAttribute("data-inicio");
            const finStr = timer.getAttribute("data-fin");
            const label = timer.previousElementSibling; 
            const duracionDiv = timer.nextElementSibling; // Capturamos el nuevo texto de duración

            if (!label) return;

            const faltanParaInicio = calcularDiferenciaTiempo(inicioStr);
            const faltanParaFin = calcularDiferenciaTiempo(finStr);

            if (faltanParaInicio > 0) {
                // ESTADO: PRÓXIMA (Azul)
                label.innerText = "Comienza en";
                timer.className = timer.classList.contains('f-timer') 
                    ? "f-value f-timer live-timer text-info" 
                    : "info-value live-timer text-info fw-bold";
                timer.innerText = formatearFechaRestante(faltanParaInicio);

                // Lógica para calcular y mostrar la duración total de la subasta (solo en tarjeta destacada).
                if (duracionDiv && duracionDiv.classList.contains('duracion-subasta') && timer.classList.contains('f-timer')) {
                    const msInicio = new Date(inicioStr.endsWith('Z') ? inicioStr : inicioStr + 'Z').getTime();
                    const msFin = new Date(finStr.endsWith('Z') ? finStr : finStr + 'Z').getTime();
                    const duracionTotal = msFin - msInicio;
                    
                    duracionDiv.innerText = `Durará: ${formatearFechaRestante(duracionTotal)}`;
                    duracionDiv.style.display = "block"; // Lo hacemos visible
                } else if (duracionDiv) {
                    duracionDiv.style.display = "none";
                }
            } 
            else if (faltanParaFin > 0) {
                // ESTADO: ACTIVA (Rojo)
                label.innerText = "Cierra en";
                timer.className = timer.classList.contains('f-timer') 
                    ? "f-value f-timer live-timer timer-highlight" 
                    : "info-value live-timer timer-highlight fw-bold";
                timer.innerText = formatearFechaRestante(faltanParaFin);
                
                // Ocultamos la duración porque la subasta ya está corriendo
                if (duracionDiv && duracionDiv.classList.contains('duracion-subasta')) duracionDiv.style.display = "none";
            } 
            else {
                // ESTADO: FINALIZADA (Gris)
                label.innerText = "Estado";
                timer.className = timer.classList.contains('f-timer') 
                    ? "f-value f-timer live-timer text-white-50" 
                    : "info-value live-timer text-white-50 fw-bold";
                timer.innerText = "Finalizada";

                // Ocultamos la duración
                if (duracionDiv && duracionDiv.classList.contains('duracion-subasta')) duracionDiv.style.display = "none";
            }
        });
    }, 1000); 
}
