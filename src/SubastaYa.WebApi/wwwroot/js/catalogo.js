document.addEventListener("DOMContentLoaded", () => {
    configurarEventosFiltros();
    obtenerCatalogo();
    iniciarTemporizadorGlobal();
});

// ==========================================
// 1. CONFIGURACIÓN DE EVENTOS (Setup)
// ==========================================
function configurarEventosFiltros() {
    const idsFiltros = ["filtroEstado", "filtroCategoria", "precioMin", "precioMax", "buscadorGeneral"];
    idsFiltros.forEach(id => {
        document.getElementById(id).addEventListener("input", obtenerCatalogo);
    });

    const pills = document.querySelectorAll(".sort-pill");
    pills.forEach(pill => {
        pill.addEventListener("click", (e) => {
            pills.forEach(p => p.classList.remove("active"));
            e.target.classList.add("active");
            obtenerCatalogo();
        });
    });

    document.getElementById("btnLimpiar").addEventListener("click", limpiarFiltros);
}

function limpiarFiltros() {
    document.getElementById("filtroEstado").value = "todos";
    document.getElementById("filtroCategoria").value = "todas";
    document.getElementById("precioMin").value = "";
    document.getElementById("precioMax").value = "";
    document.getElementById("buscadorGeneral").value = "";
    
    document.querySelectorAll(".sort-pill").forEach(p => p.classList.remove("active"));
    document.querySelector('[data-sort="menor-tiempo"]').classList.add("active");

    obtenerCatalogo();
}

// ==========================================
// 2. COMUNICACIÓN CON LA API (Capa de Red)
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
    }
}

function construirUrlConFiltros() {
    const estado = document.getElementById("filtroEstado").value;
    const categoria = document.getElementById("filtroCategoria").value;
    const precioMin = document.getElementById("precioMin").value;
    const precioMax = document.getElementById("precioMax").value;
    const busqueda = document.getElementById("buscadorGeneral").value;
    const ordenActivo = document.querySelector(".sort-pill.active");
    const orden = ordenActivo ? ordenActivo.getAttribute("data-sort") : "menor-tiempo";

    let url = `/api/auctions?orderBy=${orden}`;
    if (estado && estado !== "todos") url += `&estado=${estado}`;
    if (categoria && categoria !== "todas") url += `&categoriaId=${categoria}`;
    if (precioMin) url += `&precioMin=${precioMin}`;
    if (precioMax) url += `&precioMax=${precioMax}`;
    if (busqueda) url += `&busqueda=${encodeURIComponent(busqueda)}`;
    
    return url;
}

// ==========================================
// 3. LÓGICA DE RENDERIZADO (Capa de Presentación)
// ==========================================
function procesarYRenderizarSubastas(subastas) {
    const grid = document.getElementById("gridSubastas");
    const featuredContainer = document.getElementById("featuredContainer");
    grid.innerHTML = "";
    featuredContainer.innerHTML = "";

    if (subastas.length === 0) {
        grid.innerHTML = `<div class="col-12 text-center text-muted py-5">No se encontraron subastas.</div>`;
        return;
    }

    // Identificar la subasta destacada (la activa que vence más pronto)
    let subastasActivas = subastas.filter(s => s.estado === 1 && calcularDiferenciaTiempo(s.fechaFin) > 0);
    
    if (subastasActivas.length > 0) {
        // Ordenamos por la que vence primero
        subastasActivas.sort((a, b) => calcularDiferenciaTiempo(a.fechaFin) - calcularDiferenciaTiempo(b.fechaFin));
        const destacada = subastasActivas[0];
        
        renderizarDestacada(destacada, featuredContainer);
        // Quitamos la destacada de la lista general
        subastas = subastas.filter(s => s.id !== destacada.id);
    }

    // Renderizar el resto de las tarjetas
    subastas.forEach(subasta => renderizarTarjeta(subasta, grid));
}

function renderizarTarjeta(subasta, contenedor) {
    contenedor.innerHTML += `
        <div class="col-12 col-sm-6 col-lg-3">
            <div class="auction-card">
                <div class="card-img-container">
                    <span class="category-badge">${subasta.categoriaNombre}</span>
                    <img src="${subasta.urlImagen}" alt="${subasta.titulo}">
                </div>
                <div class="card-body">
                    <h5 class="card-title">${subasta.titulo}</h5>
                    <div class="card-info-row">
                        <div>
                            <span class="info-label">Oferta Actual</span>
                            <span class="info-value">$${subasta.ofertaMasAlta}</span>
                        </div>
                        <div class="text-end">
                            <span class="info-label">Cierra en</span>
                            <span class="info-value timer-highlight live-timer" data-endtime="${subasta.fechaFin}">
                                ${formatearFechaRestante(subasta.fechaFin)}
                            </span>
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
                <span class="featured-badge">🔥 ¡Termina pronto! - ${subasta.categoriaNombre}</span>
                <h2 class="featured-title">${subasta.titulo}</h2>
                <div class="featured-info">
                    <div>
                        <span class="f-label">Oferta Actual</span>
                        <span class="f-value">$${subasta.ofertaMasAlta}</span>
                    </div>
                    <div>
                        <span class="f-label">Tiempo Restante</span>
                        <span class="f-value f-timer live-timer" data-endtime="${subasta.fechaFin}">
                            ${formatearFechaRestante(subasta.fechaFin)}
                        </span>
                    </div>
                </div>
                <a href="/sala.html?id=${subasta.id}" class="btn btn-primary btn-lg px-5 py-3 rounded-pill fw-bold">Ofertar Ahora</a>
            </div>
        </div>
    `;
}

// ==========================================
// 4. MANEJO DEL TIEMPO (Cálculos puros)
// ==========================================
function calcularDiferenciaTiempo(fechaStr) {
    // FIX UTC DEFINITIVO: Si el string no termina en Z, se lo agregamos forzadamente
    // Esto evita que JavaScript le sume 3 horas por estar en Argentina.
    const fechaUtcSegura = fechaStr.endsWith('Z') ? fechaStr : fechaStr + 'Z';
    return new Date(fechaUtcSegura).getTime() - new Date().getTime();
}

function formatearFechaRestante(fechaStr) {
    const diferencia = calcularDiferenciaTiempo(fechaStr);
    
    if (diferencia <= 0) return "Finalizada";
    
    const horas = Math.floor(diferencia / (1000 * 60 * 60));
    const minutos = Math.floor((diferencia % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferencia % (1000 * 60)) / 1000);

    return `${String(horas).padStart(2, '0')}:${String(minutos).padStart(2, '0')}:${String(segundos).padStart(2, '0')}`;
}

function iniciarTemporizadorGlobal() {
    setInterval(() => {
        document.querySelectorAll(".live-timer").forEach(timer => {
            const endTime = timer.getAttribute("data-endtime");
            if (endTime) {
                timer.innerText = formatearFechaRestante(endTime);
                
                // Recargar catálogo automáticamente si una subasta llega a cero
                if (timer.innerText === "Finalizada" && timer.dataset.reloaded !== "true") {
                    timer.dataset.reloaded = "true";
                    setTimeout(obtenerCatalogo, 2000); 
                }
            }
        });
    }, 1000);
}