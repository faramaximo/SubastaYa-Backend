// Reemplaza los '0000' por el puerto HTTPS real de tu BACKEND


document.addEventListener("DOMContentLoaded", () => {
    configurarEventosFiltros();
    obtenerCatalogo();
    iniciarTemporizadorGlobal();
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
                    <a href="/sala.html?id=${subasta.id}" class="btn btn-primary w-100 mt-3">Ver Sala</a>
                </div>
            </div>
        </div>
    `;
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
                <a href="/sala.html?id=${subasta.id}" class="btn btn-primary btn-lg px-5 py-3 rounded-pill fw-bold">Ofertar Ahora</a>
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