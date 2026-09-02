document.addEventListener("DOMContentLoaded", () => {
    obtenerCatalogo();

    // Eventos de Filtros y Búsqueda
    document.getElementById("filtroEstado").addEventListener("change", obtenerCatalogo);
    document.getElementById("filtroCategoria").addEventListener("change", obtenerCatalogo);
    document.getElementById("precioMin").addEventListener("input", obtenerCatalogo);
    document.getElementById("precioMax").addEventListener("input", obtenerCatalogo);
    document.getElementById("buscadorGeneral").addEventListener("input", obtenerCatalogo);

    // Evento de Píldoras de Ordenamiento (Solo una activa a la vez)
    const pills = document.querySelectorAll(".sort-pill");
    pills.forEach(pill => {
        pill.addEventListener("click", (e) => {
            pills.forEach(p => p.classList.remove("active"));
            e.target.classList.add("active");
            obtenerCatalogo();
        });
    });

    // Evento para el botón "Limpiar Filtros"
    document.getElementById("btnLimpiar").addEventListener("click", () => {
        document.getElementById("filtroEstado").value = "todos";
        document.getElementById("filtroCategoria").value = "todas";
        document.getElementById("precioMin").value = "";
        document.getElementById("precioMax").value = "";
        document.getElementById("buscadorGeneral").value = "";
        
        pills.forEach(p => p.classList.remove("active"));
        document.querySelector('[data-sort="menor-tiempo"]').classList.add("active");

        obtenerCatalogo();
    });

    // Iniciar el reloj en tiempo real para todas las subastas mostradas
    iniciarTemporizadorGlobal();
});

async function obtenerCatalogo() {
    const estado = document.getElementById("filtroEstado").value;
    const categoria = document.getElementById("filtroCategoria").value;
    const precioMin = document.getElementById("precioMin").value;
    const precioMax = document.getElementById("precioMax").value;
    const busqueda = document.getElementById("buscadorGeneral").value;
    
    const ordenActivo = document.querySelector(".sort-pill.active");
    const orden = ordenActivo ? ordenActivo.getAttribute("data-sort") : "menor-tiempo";

    // 1. AHORA LE ENVIAMOS TODO AL BACKEND PARA QUE ÉL HAGA EL TRABAJO PESADO
    let url = `/api/auctions?orderBy=${orden}`;
    if (estado && estado !== "todos") url += `&estado=${estado}`;
    if (categoria && categoria !== "todas") url += `&categoriaId=${categoria}`;
    if (precioMin) url += `&precioMin=${precioMin}`;
    if (precioMax) url += `&precioMax=${precioMax}`;
    if (busqueda) url += `&busqueda=${encodeURIComponent(busqueda)}`;

    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error("Error al obtener el catálogo");

        let subastas = await response.json();
        
        const grid = document.getElementById("gridSubastas");
        const featuredContainer = document.getElementById("featuredContainer");
        grid.innerHTML = "";
        featuredContainer.innerHTML = "";

        if (subastas.length === 0) {
            grid.innerHTML = `<div class="col-12 text-center text-muted py-5">No se encontraron subastas con estos filtros.</div>`;
            return;
        }

        // 2. Extraer la destacada (la que cierra antes)
        let subastasActivas = subastas.filter(s => s.estado === 1 && (new Date(s.fechaFin) - new Date()) > 0);
        let destacada = null;

        if (subastasActivas.length > 0) {
            subastasActivas.sort((a, b) => new Date(a.fechaFin).getTime() - new Date(b.fechaFin).getTime());
            destacada = subastasActivas[0];
            renderizarDestacada(destacada, featuredContainer);
            subastas = subastas.filter(s => s.id !== destacada.id);
        }

        // 3. Renderizar el resto (El backend ya las mandó ordenadas)
        subastas.forEach(subasta => {
            grid.innerHTML += `
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
        });
    } catch (error) {
        console.error("Error:", error);
    }
}

// CORRECCIÓN DEL RELOJ: Ahora muestra las horas totales reales (ej: 48:00:00)
function formatearFechaRestante(fechaFinStr) {
    const diferencia = new Date(fechaFinStr).getTime() - new Date().getTime();
    if (diferencia <= 0) return "Finalizada";
    
    // Al quitar el módulo 24, permitimos que las horas superen el 24.
    const horas = Math.floor(diferencia / (1000 * 60 * 60));
    const minutos = Math.floor((diferencia % (1000 * 60 * 60)) / (1000 * 60));
    const segundos = Math.floor((diferencia % (1000 * 60)) / 1000);

    return `${String(horas).padStart(2, '0')}:${String(minutos).padStart(2, '0')}:${String(segundos).padStart(2, '0')}`;
}

// INYECCIÓN HTML DE LA TARJETA DESTACADA
function renderizarDestacada(subasta, container) {
    container.innerHTML = `
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

// MOTOR DEL RELOJ EN TIEMPO REAL
function iniciarTemporizadorGlobal() {
    setInterval(() => {
        // Busca todos los elementos que tengan la clase "live-timer"
        const timers = document.querySelectorAll(".live-timer");
        timers.forEach(timer => {
            const endTime = timer.getAttribute("data-endtime");
            if (endTime) {
                timer.innerText = formatearFechaRestante(endTime);
                
                // Si la subasta finalizó en tiempo real, recargar el catálogo entero
                if (timer.innerText === "Finalizada" && timer.dataset.reloaded !== "true") {
                    timer.dataset.reloaded = "true"; // Evita bucle infinito
                    setTimeout(() => obtenerCatalogo(), 2000); 
                }
            }
        });
    }, 1000); // 1000 milisegundos = 1 segundo
}