document.addEventListener("DOMContentLoaded", async () => {
    
    // 1. Verificamos que esté logueado
   const userId = sessionStorage.getItem("subastaya_user_id");
if (!userId) {
    window.location.href = "/login.html";
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
                contenedorCompras.innerHTML = `<div class="text-center text-white-50 py-5 mt-4" style="background: var(--bg-card); border-radius: 15px;">No tenés participación en ninguna subasta.</div>`;
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
                        <img src="${p.urlImagen}" class="panel-img" alt="Producto">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h4 class="fw-bold m-0">${p.titulo}</h4>
                                <span class="badge-estado ${badgeColor}">${badgeTexto}</span>
                            </div>
                            <!-- Contraste para texto secundario y principal. -->
                            <div class="row mt-3 text-white-50" style="font-size: 0.9rem;">
                                <div class="col-4"><strong class="text-white">Tu puja más alta:</strong><br>$${p.miMaximaPuja}</div>
                                <div class="col-4"><strong class="text-white">Oferta actual:</strong><br>$${p.ofertaGanadora}</div>
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
                contenedorVentas.innerHTML = `<div class="text-center text-white-50 py-5 mt-4" style="background: var(--bg-card); border-radius: 15px;">Todavía no publicaste ningún artículo.</div>`;
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
                        <img src="${v.urlImagen}" class="panel-img" alt="Producto">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h4 class="fw-bold m-0">${v.titulo}</h4>
                                <span class="badge-estado ${badgeColor}">${badgeTexto}</span>
                            </div>
                            <!-- Contraste para texto secundario y principal. -->
                            <div class="row mt-3 text-white-50" style="font-size: 0.9rem;">
                                <div class="col-4"><strong class="text-white">Precio Base:</strong><br>$${v.precioBase}</div>
                                <div class="col-4"><strong class="text-white">Oferta Más Alta:</strong><br>$${v.ofertaMasAlta}</div>
                                <div class="col-4"><strong class="text-white">Total de Pujas:</strong><br>${v.cantidadPujas}</div>
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
