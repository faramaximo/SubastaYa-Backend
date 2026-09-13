document.addEventListener("DOMContentLoaded", async () => {
    
    // 1. Verificamos que esté logueado
   const userId = sessionStorage.getItem("subastaya_user_id");
if (!userId) {
    window.location.href = "/pages/login.html";
    return;
}

    const contenedorCompras = document.getElementById("contenedorCompras");
    const contenedorVentas = document.getElementById("contenedorVentas");

    // ==========================================
    // CARGAR MIS COMPRAS / PUJAS
    // ==========================================
    async function cargarCompras() {
        try {
            const response = await fetch(`${API_BASE_URL}/api/usuarios/${userId}/pujas`);
            if (!response.ok) throw new Error("Error al cargar las pujas");
            const pujas = await response.json();

            if (pujas.length === 0) {
                contenedorCompras.innerHTML = `<div class="panel-empty-state">No tenés participación en ninguna subasta.</div>`;
                return;
            }

            contenedorCompras.innerHTML = "";
            pujas.forEach(p => {
                const soyGanador = p.miMaximaPuja >= p.ofertaGanadora;
                let badgeColor, badgeTexto;

                // CORRECCIÓN DE ESTADOS C# (0: Programada, 1: Activa, 2: Finalizada, 3: Desierta)
                if (p.estado === 1) { 
                    badgeColor = soyGanador ? 'bg-success' : 'bg-warning text-dark';
                    badgeTexto = soyGanador ? 'Liderando' : 'Superado';
                } else { // Estados 2 o 3 (Terminadas)
                    badgeColor = soyGanador ? 'bg-primary' : 'bg-secondary text-white';
                    badgeTexto = soyGanador ? '¡Ganaste!' : 'Finalizada - Perdiste';
                }

                contenedorCompras.innerHTML += `
                    <div class="panel-card">
                        <img src="${p.urlImagen}" class="panel-img" alt="${p.titulo}">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h4 class="fw-bold m-0">${p.titulo}</h4>
                                <span class="badge-estado ${badgeColor}">${badgeTexto}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-end flex-wrap gap-3 mt-3">
                                <div class="d-flex gap-4">
                                    <div>
                                        <span class="info-label">Tu puja más alta</span>
                                        <span class="info-value fs-5">$${p.miMaximaPuja}</span>
                                    </div>
                                    <div>
                                        <span class="info-label">Oferta actual</span>
                                        <span class="info-value fs-5">$${p.ofertaGanadora}</span>
                                    </div>
                                </div>
                                <a href="/pages/sala.html?id=${p.id}" class="btn btn-primary btn-sm px-4 py-2 fw-semibold text-nowrap">Ver Sala</a>
                            </div>
                        </div>
                    </div>
                `;
            });
        } catch (error) {
            contenedorCompras.innerHTML = `<div class="alert alert-danger">Error: ${error.message}</div>`;
        }
    }

    // ==========================================
    // CARGAR MIS PUBLICACIONES
    // ==========================================
    async function cargarVentas() {
        try {
            const response = await fetch(`${API_BASE_URL}/api/usuarios/${userId}/publicaciones`);
            if (!response.ok) throw new Error("Error al cargar las publicaciones");
            const ventas = await response.json();

            if (ventas.length === 0) {
                contenedorVentas.innerHTML = `<div class="panel-empty-state">Todavía no publicaste ningún artículo.</div>`;
                return;
            }

            contenedorVentas.innerHTML = "";
            ventas.forEach(v => {
                let badgeColor, badgeTexto;

                // CORRECCIÓN DE ESTADOS C#
                if (v.estado === 0) {
                    badgeColor = 'bg-info text-dark';
                    badgeTexto = 'Programada';
                } else if (v.estado === 1) {
                    badgeColor = 'bg-success';
                    badgeTexto = 'En curso';
                } else {
                    badgeColor = 'bg-secondary text-white';
                    badgeTexto = 'Finalizada';
                }

                contenedorVentas.innerHTML += `
                    <div class="panel-card">
                        <img src="${v.urlImagen}" class="panel-img" alt="${v.titulo}">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h4 class="fw-bold m-0">${v.titulo}</h4>
                                <span class="badge-estado ${badgeColor}">${badgeTexto}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-end flex-wrap gap-3 mt-3">
                                <div class="d-flex gap-4 flex-wrap">
                                    <div>
                                        <span class="info-label">Precio Base</span>
                                        <span class="info-value fs-5">$${v.precioBase}</span>
                                    </div>
                                    <div>
                                        <span class="info-label">Oferta Más Alta</span>
                                        <span class="info-value fs-5">$${v.ofertaMasAlta}</span>
                                    </div>
                                    <div>
                                        <span class="info-label">Total de Pujas</span>
                                        <span class="info-value fs-5">${v.cantidadPujas}</span>
                                    </div>
                                </div>
                                <a href="/pages/sala.html?id=${v.id}" class="btn btn-primary btn-sm px-4 py-2 fw-semibold text-nowrap">Ver Sala</a>
                            </div>
                        </div>
                    </div>
                `;
            });
        } catch (error) {
            contenedorVentas.innerHTML = `<div class="alert alert-danger">Error: ${error.message}</div>`;
        }
    }

    // Ejecutamos ambas cargas en simultáneo
    await Promise.all([cargarCompras(), cargarVentas()]);
});
