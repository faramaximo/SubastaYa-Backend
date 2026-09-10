document.addEventListener("DOMContentLoaded", () => {
    // 1. Leemos la sesión aislada de la pestaña actual
    const userId = sessionStorage.getItem("subastaya_user_id");
    const userName = sessionStorage.getItem("subastaya_user_name");
    



    // 2. Identificamos en qué página estamos
    const paginaActual = window.location.pathname.toLowerCase();
    const esPaginaProtegida = paginaActual.includes("crear-subasta") || 
                              paginaActual.includes("billetera") || 
                              paginaActual.includes("panel");

    // Render function para poder invocarla desde otras partes
    const sidebarBottomEl = document.querySelector(".sidebar-bottom-placeholder");

    function decodeNameFromToken(token) {
        try {
            const parts = token.split('.');
            if (parts.length < 2) return null;
            const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
            return payload.unique_name || payload.name || payload.email || null;
        } catch (e) { return null; }
    }

    window.updateAuthSidebar = function() {
        const userId = sessionStorage.getItem("subastaya_user_id");
        let userName = sessionStorage.getItem("subastaya_user_name");
        const token = localStorage.getItem('token');

        if (!userName && token) {
            // Intentamos obtener el nombre desde el JWT si no está en sessionStorage
            userName = decodeNameFromToken(token) || 'Usuario';
        }

        if (userId || token) {
            if (sidebarBottomEl) {
                sidebarBottomEl.innerHTML = `
                    <div class="pt-3 border-top border-secondary border-opacity-25 px-3 py-2">
                        <div class="text-white-50 mb-1" style="font-size: 0.85rem;">Conectado como:</div>
                        <div class="text-white fw-bold mb-2">${userName || 'Usuario'}</div>
                        <a href="#" class="text-white-50 text-decoration-none d-block mb-2">⚙️ Configuración</a>
                        <a href="#" id="btnCerrarSesion" class="text-danger fw-bold text-decoration-none d-block">Cerrar sesión</a>
                    </div>
                `;
                const btn = document.getElementById("btnCerrarSesion");
                if (btn) btn.addEventListener("click", (e) => {
                    e.preventDefault();
                    sessionStorage.clear();
                    localStorage.removeItem('token');
                    window.location.href = "/login.html";
                });
            }
            return;
        }

        // Estado invitado
        if (esPaginaProtegida) { window.location.href = "/login.html"; return; }
        if (sidebarBottomEl) {
            sidebarBottomEl.innerHTML = `<a href="/login.html" class="text-success text-decoration-none d-flex align-items-center gap-2 px-3 py-2 fw-bold">🔑 Iniciar Sesión</a>`;
        }
    };

    // Ejecutamos al cargar
    window.updateAuthSidebar();
});