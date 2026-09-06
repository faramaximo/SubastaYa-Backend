document.addEventListener("DOMContentLoaded", () => {
    // 1. Leemos la sesión aislada de la pestaña actual
    const userId = sessionStorage.getItem("subastaya_user_id");
    const userName = sessionStorage.getItem("subastaya_user_name");
    



    // 2. Identificamos en qué página estamos
    const paginaActual = window.location.pathname.toLowerCase();
    const esPaginaProtegida = paginaActual.includes("crear-subasta") || 
                              paginaActual.includes("billetera") || 
                              paginaActual.includes("panel");

    const sidebarBottom = document.querySelector(".sidebar-bottom");

    if (userId && userName) {
        // ==========================================
        // ESTADO: USUARIO LOGUEADO
        // ==========================================
        if (sidebarBottom) {
            // Inyectamos la estructura completa con clases de contraste y configuración
            sidebarBottom.innerHTML = `
                <div class="pt-3 border-top border-secondary border-opacity-25">
                    <div class="text-white-50 mb-1" style="font-size: 0.85rem;">Conectado como:</div>
                    <div class="text-white fw-bold mb-3">${userName}</div>

                    <a href="#" class="text-white-50 text-decoration-none d-block mb-2">
                        ⚙️ Configuración
                    </a>
                    
                    <a href="#" id="btnCerrarSesion" class="text-danger fw-bold text-decoration-none d-block">
                        Cerrar sesión
                    </a>
                </div>
            `;

            // Le damos vida al botón de Cerrar Sesión
            document.getElementById("btnCerrarSesion").addEventListener("click", (e) => {
                e.preventDefault();
                sessionStorage.clear(); // Borramos solo la sesión de esta pestaña
                window.location.href = "/login.html"; // Lo mandamos al login
            });
        }
    } else {
        // ==========================================
        // ESTADO: USUARIO NO LOGUEADO (Invitado)
        // ==========================================
        
        // Si intenta entrar a una sección protegida sin loguearse, lo echamos
        if (esPaginaProtegida) {
            window.location.href = "/login.html";
            return;
        }

        // Si está en el catálogo público, le mostramos el botón de Iniciar Sesión
        if (sidebarBottom) {
            sidebarBottom.innerHTML = `
                <a href="/login.html" class="text-success text-decoration-none d-flex align-items-center gap-2 px-3 py-2 fw-bold">
                    🔑 Iniciar Sesión
                </a>
            `;
        }
    }
});