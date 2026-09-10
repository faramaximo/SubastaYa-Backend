// Reemplaza los '0000' por el puerto HTTPS real de tu BACKEND


let hubConnection = null;
let currentSalaId = null;

document.addEventListener("DOMContentLoaded", () => {
    configurarEventosFiltros();
    obtenerCatalogo();
    iniciarTemporizadorGlobal();
    // Asegurar vista por defecto: catálogo visible, sala oculta
    const salaEl = document.getElementById('sala-view');
    const catalogEl = document.getElementById('catalogo-view') || document.getElementById('catalog-view');
    if (salaEl) salaEl.classList.add('d-none');
    if (catalogEl) catalogEl.classList.remove('d-none');
    // Actualizar sidebar auth area según token / session
    if (window.updateAuthSidebar) window.updateAuthSidebar();
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
            elemento.addEventListener("input", obtenerCatalogo);
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

    // 👇 CAMBIO AQUÍ: Ahora usamos la ruta absoluta hacia el backend 👇
    let url = `${API_BASE_URL}/api/auctions?orderBy=${orden}`;

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
    
    // 🛡️ ESCUDO: Si no estamos en el index.html, no hace nada
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
                            <!-- 👇 NUEVO: Contenedor oculto para la duración 👇 -->
                            <div class="duracion-subasta mt-1" style="font-size: 0.75rem; color: #a29bfe; display: none; font-weight: 600;"></div>
                        </div>
                    </div>
                    <a href="#" data-id="${subasta.id}" class="btn btn-primary w-100 mt-3 ver-sala">Ver Sala</a>
                </div>
            </div>
        </div>
    `;
}

// Delegación de eventos: abrir sala al hacer click en cualquier botón .ver-sala
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.ver-sala');
    if (!btn) return;
    e.preventDefault();
    const id = btn.getAttribute('data-id');
    if (id) mostrarSala(id);
});

// Botón volver al catálogo
// Botón volver al catálogo (simple)
document.addEventListener('click', function (e) {
    const volver = e.target.closest('#btnVolverCatalogo');
    if (!volver) return;
    e.preventDefault();
    const salaEl = document.getElementById('sala-view');
    const catalogEl = document.getElementById('catalogo-view') || document.getElementById('catalog-view');
    if (salaEl) {
        // limpiar contenedores de sala básicos
        const img = salaEl.querySelector('#salaImagen'); if (img) img.src = '';
        const titulo = salaEl.querySelector('#salaTitulo'); if (titulo) titulo.innerText = '';
        const desc = salaEl.querySelector('#salaDescripcion'); if (desc) desc.innerText = '';
        const oferta = salaEl.querySelector('#salaOferta'); if (oferta) oferta.innerText = '$0';
        const historial = salaEl.querySelector('#historialPujas'); if (historial) historial.innerHTML = '';
        const monto = salaEl.querySelector('#montoPuja'); if (monto) monto.value = '';
        salaEl.classList.add('d-none');
    }
    if (catalogEl) catalogEl.classList.remove('d-none');
});

async function mostrarSala(id) {
    try {
        const resp = await fetch(`${API_BASE_URL}/api/auctions/${id}`);
        if (!resp.ok) throw new Error('No se pudo cargar la subasta');
        const subasta = await resp.json();

        // Rellenar datos en la vista de sala
        const salaView = document.getElementById('sala-view');
        const catalogView = document.getElementById('catalog-view');

        document.getElementById('salaTitulo').innerText = subasta.titulo || '';
        document.getElementById('salaDescripcion').innerText = subasta.descripcion || '';
        document.getElementById('salaImagen').src = subasta.urlImagen || '';
        const oferta = subasta.ofertaMasAlta && subasta.ofertaMasAlta > 0 ? subasta.ofertaMasAlta : subasta.precioBase;
        document.getElementById('salaOferta').innerText = `$${oferta}`;

        // Historial de pujas
        const historial = document.getElementById('historialPujas');
        historial.innerHTML = '';
        const pujas = subasta.pujas || subasta.Pujas || [];
        if (Array.isArray(pujas) && pujas.length > 0) {
            // Orden descendente por fecha
            pujas.sort((a,b) => new Date(b.fechaPuja || b.FechaPuja) - new Date(a.fechaPuja || a.FechaPuja));
            pujas.forEach((p, idx) => {
                const comprador = p.compradorId || p.compradorID || p.comprador || p.usuarioId || p.usuario || 'Anónimo';
                const montoVal = p.monto ?? p.Monto ?? p.MontoOferta ?? 0;
                const fecha = new Date(p.fechaPuja || p.FechaPuja || p.fecha || Date.now()).toLocaleString();
                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center';
                li.innerHTML = `<div><div class="fw-semibold">${comprador}</div><div class="sala-meta">${fecha}</div></div><span class=\"badge rounded-pill\">$${montoVal}</span>`;
                historial.appendChild(li);
            });
        } else {
            historial.innerHTML = '<li class="list-group-item text-muted">Aún no hay pujas</li>';
        }

        // Mostrar sala y ocultar catálogo
        const catalogEl = document.getElementById('catalogo-view') || document.getElementById('catalog-view');
        if (catalogEl) catalogEl.classList.add('d-none');
        if (salaView) salaView.classList.remove('d-none');

        // Preparar evento ofertar (simple, depende de autenticación)
        const btnOfertar = document.getElementById('btnOfertar');
        const inputMonto = document.getElementById('montoPuja');
        btnOfertar.onclick = async () => {
            const monto = parseFloat(inputMonto.value);
            if (!monto || monto <= 0) return showToast('danger', 'Ingresá un monto válido');

            // Requerimos token JWT en localStorage
            const token = localStorage.getItem('token');
            console.log('Token a enviar:', token);
            if (!token) {
                showToast('warning', 'Debés iniciar sesión para poder pujar.');
                window.location.href = '/login.html';
                return;
            }

            try {
                const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };
                const body = { subastaId: parseInt(id), monto };

                const r = await fetch(`${API_BASE_URL}/api/bids`, { method: 'POST', headers, body: JSON.stringify(body) });
                if (!r.ok) {
                    const json = await r.json().catch(() => null);
                    const text = json?.error || json?.message || await r.text();
                    throw new Error(text || 'Error al realizar la puja');
                }
                showToast('success', '¡Puja realizada con éxito!');
                // refrescar la sala
                mostrarSala(id);
            } catch (err) {
                console.error(err);
                showToast('danger', 'No se pudo enviar la puja: ' + (err.message || err));
            }
        };

        // Nota: lógica SignalR removida en esta restauración. Si se desea reactivar,
        // puede agregarse aquí una conexión simple a /hubs/auction y listeners para actualizar la sala en tiempo real.

    } catch (error) {
        console.error('Error al cargar sala:', error);
        showToast('danger', 'No se pudo cargar la sala. Ver consola.');
    }
}

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
                <span class="featured-badge">🔥 ¡Termina pronto!</span>
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
                        <!-- 👇 NUEVO: Contenedor oculto para la duración 👇 -->
                        <div class="duracion-subasta mt-1" style="font-size: 0.85rem; color: #a29bfe; display: none; font-weight: 600;"></div>
                    </div>
                </div>
                <a href="#" data-id="${subasta.id}" class="btn btn-primary btn-lg px-5 py-3 rounded-pill fw-bold ver-sala">Ofertar Ahora</a>
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
    if (diferenciaMs <= 0) return "00:00:00";
    
    const dias = Math.floor(diferenciaMs / (1000 * 60 * 60 * 24));
    const horas = Math.floor((diferenciaMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutos = Math.floor((diferenciaMs % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferenciaMs % (1000 * 60)) / 1000);

    if (dias > 0) {
        return `${dias}d ${String(horas).padStart(2, '0')}h ${String(minutos).padStart(2, '0')}m`;
    }

    return `${String(horas).padStart(2, '0')}:${String(minutos).padStart(2, '0')}:${String(segundos).padStart(2, '0')}`;
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

                // 👇 Lógica para calcular y mostrar la duración total de la subasta
                if (duracionDiv && duracionDiv.classList.contains('duracion-subasta')) {
                    const msInicio = new Date(inicioStr.endsWith('Z') ? inicioStr : inicioStr + 'Z').getTime();
                    const msFin = new Date(finStr.endsWith('Z') ? finStr : finStr + 'Z').getTime();
                    const duracionTotal = msFin - msInicio;
                    
                    duracionDiv.innerText = `Durará: ${formatearFechaRestante(duracionTotal)}`;
                    duracionDiv.style.display = "block"; // Lo hacemos visible
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