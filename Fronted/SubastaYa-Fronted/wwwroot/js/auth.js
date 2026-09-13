(() => {
    const sessionKeys = ["subastaya_user_id", "subastaya_user_name"];
    const clearSession = () => { sessionKeys.forEach((key) => sessionStorage.removeItem(key)); localStorage.removeItem("token"); };
    const tokenPayload = (token) => { try { const part = token.split(".")[1]; const encoded = part.replace(/-/g, "+").replace(/_/g, "/"); return JSON.parse(atob(encoded.padEnd(encoded.length + (4 - encoded.length % 4) % 4, "="))); } catch { return null; } };
    const getAuthenticatedUser = () => {
        const token = localStorage.getItem("token"), id = sessionStorage.getItem("subastaya_user_id"), payload = token && tokenPayload(token);
        const tokenId = payload?.nameid || payload?.sub || payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
        if (!token || !id || !payload?.exp || payload.exp * 1000 <= Date.now() || String(tokenId) !== id) { clearSession(); return null; }
        return { id, name: sessionStorage.getItem("subastaya_user_name") || payload.unique_name || payload.name || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "Usuario" };
    };
    const escapeHtml = (value) => String(value).replace(/[&<>'"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" })[c]);

    document.addEventListener("DOMContentLoaded", () => {
        const page = window.location.pathname.toLowerCase();
        const isProtected = ["crear-subasta", "billetera", "panel", "perfil"].some((route) => page.includes(route));
        const themeToggle = document.getElementById("themeToggle");
        const authActions = document.querySelector(".auth-actions");
        const applyTheme = (theme) => {
            const dark = theme === "dark";
            document.body.dataset.theme = dark ? "dark" : "light";
            if (themeToggle) { themeToggle.setAttribute("aria-pressed", String(dark)); themeToggle.setAttribute("aria-label", dark ? "Activar modo claro" : "Activar modo oscuro"); themeToggle.querySelector(".theme-toggle__label").textContent = dark ? "Claro" : "Oscuro"; }
        };
        applyTheme(localStorage.getItem("subastaya_theme") || "light");
        themeToggle?.addEventListener("click", () => { const next = document.body.dataset.theme === "dark" ? "light" : "dark"; localStorage.setItem("subastaya_theme", next); applyTheme(next); });
        window.getAuthenticatedUser = getAuthenticatedUser;
        window.updateAuthHeader = () => {
            const user = getAuthenticatedUser();
            if (!user && isProtected) { window.location.replace("/pages/login.html"); return; }
            if (!authActions) return;
            if (!user) { authActions.innerHTML = '<a href="/pages/login.html" class="auth-link">Ingresar</a><a href="/pages/registro.html" class="auth-button">Crear cuenta</a>'; return; }
            authActions.innerHTML = `<div class="user-menu"><span class="user-menu__name">Hola, ${escapeHtml(user.name)}</span><a class="user-menu__settings" href="/pages/perfil.html#configuracion" aria-label="Configuración" title="Configuración"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19.14 12.94c.04-.31.06-.62.06-.94s-.02-.63-.07-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.61-.22l-2.39.96a7.2 7.2 0 0 0-1.62-.94L14.38 2.8A.5.5 0 0 0 13.89 2h-3.84a.5.5 0 0 0-.49.41l-.36 2.54c-.59.24-1.14.55-1.62.93l-2.39-.96a.5.5 0 0 0-.61.22L2.66 8.46a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.07.64-.07.96s.03.64.07.94L2.78 14.16a.5.5 0 0 0-.12.64l1.92 3.32a.5.5 0 0 0 .61.22l2.39-.96c.48.38 1.03.69 1.62.94l.36 2.54a.5.5 0 0 0 .49.41h3.84a.5.5 0 0 0 .49-.41l.36-2.54c.59-.24 1.14-.55 1.62-.94l2.39.96a.5.5 0 0 0 .61-.22l1.92-3.32a.5.5 0 0 0-.12-.64l-2.02-1.58ZM12 15.5A3.5 3.5 0 1 1 12 8a3.5 3.5 0 0 1 0 7.5Z"/></svg></a><button id="btnCerrarSesion" class="user-menu__logout" type="button">Cerrar sesión</button></div>`;
            document.getElementById("btnCerrarSesion")?.addEventListener("click", () => {
                if (!window.confirm("¿Querés cerrar sesión?")) return;
                clearSession();
                window.location.assign("/index.html");
            });
        };
        window.updateAuthHeader();
    });
})();
