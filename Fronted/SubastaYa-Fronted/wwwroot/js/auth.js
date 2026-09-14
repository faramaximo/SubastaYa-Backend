(() => {
    const sessionKeys = ["subastaya_user_id", "subastaya_user_name"];
    const clearSession = () => { 
        sessionKeys.forEach((key) => sessionStorage.removeItem(key)); 
        localStorage.removeItem("token"); 
    };

    const tokenPayload = (token) => { 
        try { 
            const part = token.split(".")[1]; 
            const encoded = part.replace(/-/g, "+").replace(/_/g, "/"); 
            return JSON.parse(atob(encoded.padEnd(encoded.length + (4 - encoded.length % 4) % 4, "="))); 
        } catch { 
            return null; 
        } 
    };

    const getAuthenticatedUser = () => {
        const token = localStorage.getItem("token");
        const id = sessionStorage.getItem("subastaya_user_id");
        const payload = token && tokenPayload(token);
        const tokenId = payload?.nameid || payload?.sub || payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
        if (!token || !id || !payload?.exp || payload.exp * 1000 <= Date.now() || String(tokenId) !== id) { 
            clearSession(); 
            return null; 
        }
        return { 
            id, 
            name: sessionStorage.getItem("subastaya_user_name") || payload.unique_name || payload.name || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "Usuario" 
        };
    };

    const escapeHtml = (value) => String(value).replace(/[&<>'"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" })[c]);

    // Asegurar que auth.css esté cargado en la página
    if (!document.querySelector('link[href*="auth.css"]')) {
        const link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = "/css/auth.css";
        document.head.appendChild(link);
    }

    // ==========================================================================
    // VENTANA FLOTANTE DE AUTENTICACIÓN (LOGIN & REGISTRO)
    // ==========================================================================
    function ensureAuthModal() {
        if (document.getElementById("authModal")) return;

        const modalHtml = `
            <div id="authModal" class="auth-modal-overlay" aria-hidden="true">
                <div class="auth-modal-card" role="dialog" aria-modal="true" aria-labelledby="authModalTitle">
                    <button type="button" class="auth-modal-close" id="authModalClose" aria-label="Cerrar modal">&times;</button>
                    
                    <div class="text-center mb-2">
                        <h2 id="authModalTitle" class="auth-modal-title">SubastaYa</h2>
                        <p id="authModalSubtitle" class="auth-modal-subtitle">Ingresá a tu cuenta</p>
                    </div>

                    <!-- Selector de Pestañas -->
                    <div class="auth-tabs" role="tablist">
                        <button type="button" class="auth-tab-btn active" id="tabBtnLogin" role="tab" aria-selected="true">Iniciar Sesión</button>
                        <button type="button" class="auth-tab-btn" id="tabBtnRegister" role="tab" aria-selected="false">Crear Cuenta</button>
                    </div>

                    <!-- VISTA: INICIAR SESIÓN -->
                    <div id="viewLogin" class="auth-view">
                        <form id="modalFormLogin" novalidate>
                            <div class="mb-3 text-start">
                                <label for="modalLoginEmail" class="form-label-custom">Correo Electrónico</label>
                                <input type="email" class="form-control auth-input" id="modalLoginEmail" required placeholder="ejemplo@correo.com" autocomplete="email">
                            </div>
                            <div class="mb-3 text-start">
                                <label for="modalLoginPassword" class="form-label-custom">Contraseña</label>
                                <input type="password" class="form-control auth-input" id="modalLoginPassword" required placeholder="••••••••" autocomplete="current-password">
                            </div>
                            <div id="modalLoginError" class="alert alert-danger auth-alert d-none" role="alert"></div>
                            <button type="submit" class="btn-auth-primary" id="btnModalSubmitLogin">Ingresar al Sistema</button>
                            <div class="text-center mt-3">
                                <a href="/pages/recuperar-contrasena.html" class="auth-link-secondary">¿Olvidaste tu contraseña?</a>
                            </div>
                        </form>
                        <div class="auth-card-footer">
                            ¿No tenés cuenta? <button type="button" class="auth-link-btn fw-bold" id="linkGoToRegister">Registrate acá</button>
                        </div>
                    </div>

                    <!-- VISTA: CREAR CUENTA -->
                    <div id="viewRegister" class="auth-view d-none">
                        <form id="modalFormRegister" novalidate>
                            <div class="mb-3 text-start">
                                <label for="modalRegNombre" class="form-label-custom">Nombre Completo</label>
                                <input type="text" class="form-control auth-input" id="modalRegNombre" required placeholder="Ej: Juan Pérez" autocomplete="name">
                            </div>
                            <div class="mb-3 text-start">
                                <label for="modalRegEmail" class="form-label-custom">Correo Electrónico</label>
                                <input type="email" class="form-control auth-input" id="modalRegEmail" required placeholder="ejemplo@correo.com" autocomplete="email">
                            </div>
                            <div class="mb-3 text-start">
                                <label for="modalRegPassword" class="form-label-custom">Contraseña</label>
                                <input type="password" class="form-control auth-input" id="modalRegPassword" minlength="6" required placeholder="Mínimo 6 caracteres" autocomplete="new-password">
                            </div>
                            <div id="modalRegMessage" class="alert auth-alert d-none" role="alert"></div>
                            <button type="submit" class="btn-auth-primary" id="btnModalSubmitRegister">Crear Cuenta</button>
                        </form>
                        <div class="auth-card-footer">
                            ¿Ya tenés cuenta? <button type="button" class="auth-link-btn fw-bold" id="linkGoToLogin">Iniciá sesión</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML("beforeend", modalHtml);

        const modalOverlay = document.getElementById("authModal");
        const modalClose = document.getElementById("authModalClose");
        const tabBtnLogin = document.getElementById("tabBtnLogin");
        const tabBtnRegister = document.getElementById("tabBtnRegister");
        const viewLogin = document.getElementById("viewLogin");
        const viewRegister = document.getElementById("viewRegister");
        const linkGoToRegister = document.getElementById("linkGoToRegister");
        const linkGoToLogin = document.getElementById("linkGoToLogin");
        const authModalSubtitle = document.getElementById("authModalSubtitle");

        const formLogin = document.getElementById("modalFormLogin");
        const loginError = document.getElementById("modalLoginError");
        const btnSubmitLogin = document.getElementById("btnModalSubmitLogin");

        const formRegister = document.getElementById("modalFormRegister");
        const regMsg = document.getElementById("modalRegMessage");
        const btnSubmitRegister = document.getElementById("btnModalSubmitRegister");

        function switchView(view) {
            if (view === "login") {
                tabBtnLogin.classList.add("active");
                tabBtnLogin.setAttribute("aria-selected", "true");
                tabBtnRegister.classList.remove("active");
                tabBtnRegister.setAttribute("aria-selected", "false");
                viewLogin.classList.remove("d-none");
                viewRegister.classList.add("d-none");
                authModalSubtitle.textContent = "Ingresá a tu cuenta";
                setTimeout(() => document.getElementById("modalLoginEmail")?.focus(), 80);
            } else {
                tabBtnRegister.classList.add("active");
                tabBtnRegister.setAttribute("aria-selected", "true");
                tabBtnLogin.classList.remove("active");
                tabBtnLogin.setAttribute("aria-selected", "false");
                viewRegister.classList.remove("d-none");
                viewLogin.classList.add("d-none");
                authModalSubtitle.textContent = "Unite a la plataforma";
                setTimeout(() => document.getElementById("modalRegNombre")?.focus(), 80);
            }
        }

        tabBtnLogin.addEventListener("click", () => switchView("login"));
        tabBtnRegister.addEventListener("click", () => switchView("register"));
        linkGoToRegister.addEventListener("click", () => switchView("register"));
        linkGoToLogin.addEventListener("click", () => switchView("login"));

        modalClose.addEventListener("click", () => window.closeAuthModal());
        modalOverlay.addEventListener("click", (e) => {
            if (e.target === modalOverlay) window.closeAuthModal();
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && modalOverlay.classList.contains("active")) {
                window.closeAuthModal();
            }
        });

        // Enviar Formulario de Login
        formLogin.addEventListener("submit", async (e) => {
            e.preventDefault();
            loginError.classList.add("d-none");
            const email = document.getElementById("modalLoginEmail").value.trim();
            const password = document.getElementById("modalLoginPassword").value.trim();

            if (!email || !password) {
                loginError.textContent = "Por favor completá todos los campos.";
                loginError.classList.remove("d-none");
                return;
            }

            try {
                btnSubmitLogin.disabled = true;
                btnSubmitLogin.textContent = "Verificando...";

                const base = (typeof API_BASE_URL !== "undefined") ? API_BASE_URL : "";
                const response = await fetch(`${base}/api/v1/tokens`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ email, password })
                });

                if (!response.ok) {
                    let errorMessage = "Ocurrió un error en el servidor.";
                    try {
                        const errData = await response.json();
                        errorMessage = errData.error || errData.mensaje || errorMessage;
                    } catch (_) {}
                    throw new Error(errorMessage);
                }

                const data = await response.json();
                if (!data?.id || !data?.token) throw new Error("Respuesta de autenticación inválida.");

                sessionStorage.setItem("subastaya_user_id", String(data.id));
                sessionStorage.setItem("subastaya_user_name", data.nombre || "Usuario");
                localStorage.setItem("token", data.token);

                window.closeAuthModal();
                window.updateAuthHeader();
                window.dispatchEvent(new CustomEvent("subastaya:login", { detail: data }));
            } catch (err) {
                loginError.textContent = err.message;
                loginError.classList.remove("d-none");
            } finally {
                btnSubmitLogin.disabled = false;
                btnSubmitLogin.textContent = "Ingresar al Sistema";
            }
        });

        // Enviar Formulario de Registro
        formRegister.addEventListener("submit", async (e) => {
            e.preventDefault();
            regMsg.classList.add("d-none");
            regMsg.classList.remove("alert-danger", "alert-success");

            const nombre = document.getElementById("modalRegNombre").value.trim();
            const email = document.getElementById("modalRegEmail").value.trim();
            const password = document.getElementById("modalRegPassword").value.trim();

            if (!nombre || !email || !password) {
                regMsg.textContent = "Por favor completá todos los campos.";
                regMsg.classList.add("alert-danger");
                regMsg.classList.remove("d-none");
                return;
            }

            try {
                btnSubmitRegister.disabled = true;
                btnSubmitRegister.textContent = "Creando cuenta...";

                const base = (typeof API_BASE_URL !== "undefined") ? API_BASE_URL : "";
                const response = await fetch(`${base}/api/v1/users`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ nombre, email, password })
                });

                let data = {};
                try { data = await response.json(); } catch (_) {}

                if (!response.ok) {
                    throw new Error(data.mensaje || data.error || "No se pudo crear la cuenta.");
                }

                regMsg.textContent = "¡Cuenta creada con éxito! Ingresá tus datos para ingresar.";
                regMsg.classList.add("alert-success");
                regMsg.classList.remove("d-none", "alert-danger");

                setTimeout(() => {
                    switchView("login");
                    document.getElementById("modalLoginEmail").value = email;
                    document.getElementById("modalLoginPassword").value = "";
                    document.getElementById("modalLoginPassword").focus();
                }, 1400);
            } catch (err) {
                regMsg.textContent = err.message;
                regMsg.classList.add("alert-danger");
                regMsg.classList.remove("d-none", "alert-success");
            } finally {
                btnSubmitRegister.disabled = false;
                btnSubmitRegister.textContent = "Crear Cuenta";
            }
        });
    }

    window.openAuthModal = (view = "login") => {
        ensureAuthModal();
        const modal = document.getElementById("authModal");
        if (!modal) return;

        // Limpiar errores previos
        document.getElementById("modalLoginError")?.classList.add("d-none");
        document.getElementById("modalRegMessage")?.classList.add("d-none");

        const tabBtnLogin = document.getElementById("tabBtnLogin");
        const tabBtnRegister = document.getElementById("tabBtnRegister");
        const viewLogin = document.getElementById("viewLogin");
        const viewRegister = document.getElementById("viewRegister");
        const authModalSubtitle = document.getElementById("authModalSubtitle");

        if (view === "register") {
            tabBtnRegister?.classList.add("active");
            tabBtnRegister?.setAttribute("aria-selected", "true");
            tabBtnLogin?.classList.remove("active");
            tabBtnLogin?.setAttribute("aria-selected", "false");
            viewRegister?.classList.remove("d-none");
            viewLogin?.classList.add("d-none");
            if (authModalSubtitle) authModalSubtitle.textContent = "Unite a la plataforma";
            setTimeout(() => document.getElementById("modalRegNombre")?.focus(), 100);
        } else {
            tabBtnLogin?.classList.add("active");
            tabBtnLogin?.setAttribute("aria-selected", "true");
            tabBtnRegister?.classList.remove("active");
            tabBtnRegister?.setAttribute("aria-selected", "false");
            viewLogin?.classList.remove("d-none");
            viewRegister?.classList.add("d-none");
            if (authModalSubtitle) authModalSubtitle.textContent = "Ingresá a tu cuenta";
            setTimeout(() => document.getElementById("modalLoginEmail")?.focus(), 100);
        }

        modal.classList.add("active");
        modal.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    };

    window.closeAuthModal = () => {
        const modal = document.getElementById("authModal");
        if (!modal) return;
        modal.classList.remove("active");
        modal.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
    };

    // ==========================================================================
    // CICLO DE VIDA Y CABECERA
    // ==========================================================================
    document.addEventListener("DOMContentLoaded", () => {
        ensureAuthModal();

        const page = window.location.pathname.toLowerCase();
        const isProtected = ["crear-subasta", "billetera", "panel", "perfil"].some((route) => page.includes(route));
        const themeToggle = document.getElementById("themeToggle");
        const authActions = document.querySelector(".auth-actions");

        const applyTheme = (theme) => {
            const dark = theme === "dark";
            document.body.dataset.theme = dark ? "dark" : "light";
            if (themeToggle) { 
                themeToggle.setAttribute("aria-pressed", String(dark)); 
                themeToggle.setAttribute("aria-label", dark ? "Activar modo claro" : "Activar modo oscuro"); 
                themeToggle.querySelector(".theme-toggle__label").textContent = dark ? "Claro" : "Oscuro"; 
            }
        };

        applyTheme(localStorage.getItem("subastaya_theme") || "light");
        themeToggle?.addEventListener("click", () => { 
            const next = document.body.dataset.theme === "dark" ? "light" : "dark"; 
            localStorage.setItem("subastaya_theme", next); 
            applyTheme(next); 
        });

        window.getAuthenticatedUser = getAuthenticatedUser;

        window.updateAuthHeader = () => {
            const user = getAuthenticatedUser();
            if (!user && isProtected) { 
                window.location.replace("/pages/login.html"); 
                return; 
            }
            if (!authActions) return;

            if (!user) { 
                authActions.innerHTML = `
                    <button type="button" class="auth-link" id="btnHeaderLogin">Ingresar</button>
                    <button type="button" class="auth-button" id="btnHeaderRegister">Crear cuenta</button>
                `;
                document.getElementById("btnHeaderLogin")?.addEventListener("click", () => window.openAuthModal("login"));
                document.getElementById("btnHeaderRegister")?.addEventListener("click", () => window.openAuthModal("register"));
                return; 
            }

            authActions.innerHTML = `
                <div class="user-menu">
                    <span class="user-menu__name">Hola, ${escapeHtml(user.name)}</span>
                    <a class="user-menu__settings" href="/pages/perfil.html#configuracion" aria-label="Configuración" title="Configuración">
                        <svg viewBox="0 0 24 24" aria-hidden="true">
                            <path d="M19.14 12.94c.04-.31.06-.62.06-.94s-.02-.63-.07-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.61-.22l-2.39.96a7.2 7.2 0 0 0-1.62-.94L14.38 2.8A.5.5 0 0 0 13.89 2h-3.84a.5.5 0 0 0-.49.41l-.36 2.54c-.59.24-1.14.55-1.62.93l-2.39-.96a.5.5 0 0 0-.61.22L2.66 8.46a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.07.64-.07.96s.03.64.07.94L2.78 14.16a.5.5 0 0 0-.12.64l1.92 3.32a.5.5 0 0 0 .61.22l2.39-.96c.48.38 1.03.69 1.62.94l.36 2.54a.5.5 0 0 0 .49.41h3.84a.5.5 0 0 0 .49-.41l.36-2.54c.59-.24 1.14-.55 1.62-.94l2.39.96a.5.5 0 0 0 .61-.22l1.92-3.32a.5.5 0 0 0-.12-.64l-2.02-1.58ZM12 15.5A3.5 3.5 0 1 1 12 8a3.5 3.5 0 0 1 0 7.5Z"/>
                        </svg>
                    </a>
                    <button id="btnCerrarSesion" class="user-menu__logout" type="button">Cerrar sesión</button>
                </div>
            `;

            document.getElementById("btnCerrarSesion")?.addEventListener("click", () => {
                if (!window.confirm("¿Querés cerrar sesión?")) return;
                clearSession();
                window.location.assign("/index.html");
            });
        };

        window.updateAuthHeader();

        // Interceptar cualquier enlace hacia login.html o registro.html en la página
        document.addEventListener("click", (e) => {
            const link = e.target.closest("a");
            if (!link) return;
            const href = link.getAttribute("href") || "";
            if (href.includes("login.html")) {
                e.preventDefault();
                window.openAuthModal("login");
            } else if (href.includes("registro.html")) {
                e.preventDefault();
                window.openAuthModal("register");
            }
        });

        // Apertura automática si la URL contiene parámetro de acción
        const urlParams = new URLSearchParams(window.location.search);
        const authAction = urlParams.get("action");
        if (authAction === "login" || authAction === "register") {
            const cleanUrl = window.location.pathname;
            window.history.replaceState({}, document.title, cleanUrl);
            setTimeout(() => window.openAuthModal(authAction), 150);
        }
    });
})();
