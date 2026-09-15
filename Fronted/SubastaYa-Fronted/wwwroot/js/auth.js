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
    // VENTANA FLOTANTE DE AUTENTICACIÓN (LOGIN, REGISTRO Y RECUPERACIÓN)
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
                    <div class="auth-tabs" id="authModalTabs" role="tablist">
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
                                <button type="button" class="auth-link-btn text-decoration-underline" id="btnGoToForgotPassword">¿Olvidaste tu contraseña?</button>
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

                    <!-- VISTA: RECUPERAR CONTRASEÑA -->
                    <div id="viewForgotPassword" class="auth-view d-none">
                        <form id="modalFormForgotPassword" novalidate>
                            <div class="mb-3 text-start">
                                <label for="modalForgotEmail" class="form-label-custom">Correo Electrónico</label>
                                <input type="email" class="form-control auth-input" id="modalForgotEmail" required placeholder="ejemplo@correo.com" autocomplete="email">
                            </div>
                            <div id="modalForgotMessage" class="alert auth-alert d-none" role="alert"></div>
                            <button type="submit" class="btn-auth-primary" id="btnModalSubmitForgot">Enviar enlace de recuperación</button>
                        </form>
                        <div class="auth-card-footer text-center">
                            <button type="button" class="auth-link-btn fw-bold" id="linkForgotBackToLogin">← Volver al inicio de sesión</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML("beforeend", modalHtml);

        const modalOverlay = document.getElementById("authModal");
        const modalClose = document.getElementById("authModalClose");
        const modalTabs = document.getElementById("authModalTabs");
        const tabBtnLogin = document.getElementById("tabBtnLogin");
        const tabBtnRegister = document.getElementById("tabBtnRegister");
        const viewLogin = document.getElementById("viewLogin");
        const viewRegister = document.getElementById("viewRegister");
        const viewForgotPassword = document.getElementById("viewForgotPassword");
        const linkGoToRegister = document.getElementById("linkGoToRegister");
        const linkGoToLogin = document.getElementById("linkGoToLogin");
        const btnGoToForgotPassword = document.getElementById("btnGoToForgotPassword");
        const linkForgotBackToLogin = document.getElementById("linkForgotBackToLogin");
        const authModalSubtitle = document.getElementById("authModalSubtitle");

        const formLogin = document.getElementById("modalFormLogin");
        const loginError = document.getElementById("modalLoginError");
        const btnSubmitLogin = document.getElementById("btnModalSubmitLogin");

        const formRegister = document.getElementById("modalFormRegister");
        const regMsg = document.getElementById("modalRegMessage");
        const btnSubmitRegister = document.getElementById("btnModalSubmitRegister");

        const formForgot = document.getElementById("modalFormForgotPassword");
        const forgotMsg = document.getElementById("modalForgotMessage");
        const btnSubmitForgot = document.getElementById("btnModalSubmitForgot");

        function switchView(view) {
            loginError?.classList.add("d-none");
            regMsg?.classList.add("d-none");
            forgotMsg?.classList.add("d-none");

            if (view === "register") {
                modalTabs?.classList.remove("d-none");
                tabBtnRegister?.classList.add("active");
                tabBtnRegister?.setAttribute("aria-selected", "true");
                tabBtnLogin?.classList.remove("active");
                tabBtnLogin?.setAttribute("aria-selected", "false");
                viewRegister?.classList.remove("d-none");
                viewLogin?.classList.add("d-none");
                viewForgotPassword?.classList.add("d-none");
                if (authModalSubtitle) authModalSubtitle.textContent = "Unite a la plataforma";
                setTimeout(() => document.getElementById("modalRegNombre")?.focus(), 80);
            } else if (view === "forgot") {
                modalTabs?.classList.add("d-none");
                viewForgotPassword?.classList.remove("d-none");
                viewLogin?.classList.add("d-none");
                viewRegister?.classList.add("d-none");
                if (authModalSubtitle) authModalSubtitle.textContent = "Recuperá tu contraseña";
                setTimeout(() => document.getElementById("modalForgotEmail")?.focus(), 80);
            } else {
                modalTabs?.classList.remove("d-none");
                tabBtnLogin?.classList.add("active");
                tabBtnLogin?.setAttribute("aria-selected", "true");
                tabBtnRegister?.classList.remove("active");
                tabBtnRegister?.setAttribute("aria-selected", "false");
                viewLogin?.classList.remove("d-none");
                viewRegister?.classList.add("d-none");
                viewForgotPassword?.classList.add("d-none");
                if (authModalSubtitle) authModalSubtitle.textContent = "Ingresá a tu cuenta";
                setTimeout(() => document.getElementById("modalLoginEmail")?.focus(), 80);
            }
        }

        tabBtnLogin?.addEventListener("click", () => switchView("login"));
        tabBtnRegister?.addEventListener("click", () => switchView("register"));
        linkGoToRegister?.addEventListener("click", () => switchView("register"));
        linkGoToLogin?.addEventListener("click", () => switchView("login"));
        btnGoToForgotPassword?.addEventListener("click", () => switchView("forgot"));
        linkForgotBackToLogin?.addEventListener("click", () => switchView("login"));

        modalClose?.addEventListener("click", () => window.closeAuthModal());
        modalOverlay?.addEventListener("click", (e) => {
            if (e.target === modalOverlay) window.closeAuthModal();
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && modalOverlay.classList.contains("active")) {
                window.closeAuthModal();
            }
        });

        // Enviar Formulario de Login
        formLogin?.addEventListener("submit", async (e) => {
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
        formRegister?.addEventListener("submit", async (e) => {
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

                if (!response.ok) {
                    let errorMessage = "Ocurrió un error al registrarse.";
                    try {
                        const errData = await response.json();
                        errorMessage = errData.error || errData.mensaje || errorMessage;
                    } catch (_) {}
                    throw new Error(errorMessage);
                }

                regMsg.textContent = "¡Cuenta creada con éxito! Iniciando sesión...";
                regMsg.classList.add("alert-success");
                regMsg.classList.remove("d-none");

                setTimeout(async () => {
                    try {
                        const loginResp = await fetch(`${base}/api/v1/tokens`, {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
                            body: JSON.stringify({ email, password })
                        });
                        if (loginResp.ok) {
                            const loginData = await loginResp.json();
                            sessionStorage.setItem("subastaya_user_id", String(loginData.id));
                            sessionStorage.setItem("subastaya_user_name", loginData.nombre || nombre);
                            localStorage.setItem("token", loginData.token);
                            window.closeAuthModal();
                            window.updateAuthHeader();
                            window.dispatchEvent(new CustomEvent("subastaya:login", { detail: loginData }));
                        } else {
                            switchView("login");
                        }
                    } catch (_) {
                        switchView("login");
                    }
                }, 900);
            } catch (err) {
                regMsg.textContent = err.message;
                regMsg.classList.add("alert-danger");
                regMsg.classList.remove("d-none");
            } finally {
                btnSubmitRegister.disabled = false;
                btnSubmitRegister.textContent = "Crear Cuenta";
            }
        });

        // Enviar Formulario de Recuperación de Contraseña
        formForgot?.addEventListener("submit", async (e) => {
            e.preventDefault();
            forgotMsg.classList.add("d-none");
            forgotMsg.classList.remove("alert-danger", "alert-success");

            const email = document.getElementById("modalForgotEmail").value.trim();
            if (!email) {
                forgotMsg.textContent = "Por favor ingresá tu correo electrónico.";
                forgotMsg.classList.add("alert-danger");
                forgotMsg.classList.remove("d-none");
                return;
            }

            try {
                btnSubmitForgot.disabled = true;
                btnSubmitForgot.textContent = "Enviando enlace...";

                const base = (typeof API_BASE_URL !== "undefined") ? API_BASE_URL : "";
                const response = await fetch(`${base}/api/v1/password-resets`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ email })
                });

                let mensaje = "Si el correo está registrado, recibirás un enlace con instrucciones.";
                try {
                    const data = await response.json();
                    if (data?.mensaje) mensaje = data.mensaje;
                } catch (_) {}

                forgotMsg.textContent = mensaje;
                forgotMsg.classList.add("alert-success");
                forgotMsg.classList.remove("d-none");
            } catch (err) {
                forgotMsg.textContent = "No pudimos procesar la solicitud. Intentá nuevamente.";
                forgotMsg.classList.add("alert-danger");
                forgotMsg.classList.remove("d-none");
            } finally {
                btnSubmitForgot.disabled = false;
                btnSubmitForgot.textContent = "Enviar enlace de recuperación";
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
        document.getElementById("modalForgotMessage")?.classList.add("d-none");

        const modalTabs = document.getElementById("authModalTabs");
        const tabBtnLogin = document.getElementById("tabBtnLogin");
        const tabBtnRegister = document.getElementById("tabBtnRegister");
        const viewLogin = document.getElementById("viewLogin");
        const viewRegister = document.getElementById("viewRegister");
        const viewForgotPassword = document.getElementById("viewForgotPassword");
        const authModalSubtitle = document.getElementById("authModalSubtitle");

        if (view === "register") {
            modalTabs?.classList.remove("d-none");
            tabBtnRegister?.classList.add("active");
            tabBtnRegister?.setAttribute("aria-selected", "true");
            tabBtnLogin?.classList.remove("active");
            tabBtnLogin?.setAttribute("aria-selected", "false");
            viewRegister?.classList.remove("d-none");
            viewLogin?.classList.add("d-none");
            viewForgotPassword?.classList.add("d-none");
            if (authModalSubtitle) authModalSubtitle.textContent = "Unite a la plataforma";
            setTimeout(() => document.getElementById("modalRegNombre")?.focus(), 100);
        } else if (view === "forgot") {
            modalTabs?.classList.add("d-none");
            viewForgotPassword?.classList.remove("d-none");
            viewLogin?.classList.add("d-none");
            viewRegister?.classList.add("d-none");
            if (authModalSubtitle) authModalSubtitle.textContent = "Recuperá tu contraseña";
            setTimeout(() => document.getElementById("modalForgotEmail")?.focus(), 100);
        } else {
            modalTabs?.classList.remove("d-none");
            tabBtnLogin?.classList.add("active");
            tabBtnLogin?.setAttribute("aria-selected", "true");
            tabBtnRegister?.classList.remove("active");
            tabBtnRegister?.setAttribute("aria-selected", "false");
            viewLogin?.classList.remove("d-none");
            viewRegister?.classList.add("d-none");
            viewForgotPassword?.classList.add("d-none");
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
    // MODAL ELEGANTE DE CONFIRMACIÓN PARA CERRAR SESIÓN
    // ==========================================================================
    function ensureLogoutModal() {
        if (document.getElementById("logoutConfirmModal")) return;

        const logoutModalHtml = `
            <div id="logoutConfirmModal" class="auth-modal-overlay" aria-hidden="true">
                <div class="auth-modal-card logout-modal-card" role="dialog" aria-modal="true" aria-labelledby="logoutModalTitle">
                    <button type="button" class="auth-modal-close" id="logoutModalClose" aria-label="Cerrar modal">&times;</button>
                    
                    <div class="logout-modal-icon" aria-hidden="true">
                        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
                            <path fill-rule="evenodd" d="M10 12.5a.5.5 0 0 1-.5.5h-8a.5.5 0 0 1-.5-.5v-9a.5.5 0 0 1 .5-.5h8a.5.5 0 0 1 .5.5v2a.5.5 0 0 0 1 0v-2A1.5 1.5 0 0 0 9.5 2h-8A1.5 1.5 0 0 0 0 3.5v9A1.5 1.5 0 0 0 1.5 14h8a1.5 1.5 0 0 0 1.5-1.5v-2a.5.5 0 0 0-1 0z"/>
                            <path fill-rule="evenodd" d="M15.854 8.354a.5.5 0 0 0 0-.708l-3-3a.5.5 0 0 0-.708.708L14.293 7.5H5.5a.5.5 0 0 0 0 1h8.793l-2.147 2.146a.5.5 0 0 0 .708.708z"/>
                        </svg>
                    </div>

                    <h3 id="logoutModalTitle" class="auth-modal-title fs-4 mb-2">Cerrar Sesión</h3>
                    <p class="logout-modal-text">¿Estás seguro de que deseas salir de tu cuenta?</p>

                    <div class="logout-modal-actions">
                        <button type="button" class="btn-modal-cancel" id="btnLogoutCancel">Cancelar</button>
                        <button type="button" class="btn-modal-danger" id="btnLogoutConfirm">Cerrar Sesión</button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML("beforeend", logoutModalHtml);

        const overlay = document.getElementById("logoutConfirmModal");
        const closeBtn = document.getElementById("logoutModalClose");
        const cancelBtn = document.getElementById("btnLogoutCancel");
        const confirmBtn = document.getElementById("btnLogoutConfirm");

        const close = () => {
            overlay.classList.remove("active");
            overlay.setAttribute("aria-hidden", "true");
            document.body.style.overflow = "";
        };

        closeBtn?.addEventListener("click", close);
        cancelBtn?.addEventListener("click", close);
        overlay?.addEventListener("click", (e) => {
            if (e.target === overlay) close();
        });

        confirmBtn?.addEventListener("click", () => {
            close();
            clearSession();
            window.location.assign("/index.html");
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && overlay.classList.contains("active")) {
                close();
            }
        });
    }

    window.openLogoutModal = () => {
        ensureLogoutModal();
        const modal = document.getElementById("logoutConfirmModal");
        if (!modal) return;
        modal.classList.add("active");
        modal.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
        document.getElementById("btnLogoutCancel")?.focus();
    };

    // ==========================================================================
    // CICLO DE VIDA Y CABECERA
    // ==========================================================================
    document.addEventListener("DOMContentLoaded", () => {
        ensureAuthModal();
        ensureLogoutModal();

        const page = window.location.pathname.toLowerCase();
        const isProtected = ["crear-subasta", "billetera", "panel"].some((route) => page.includes(route));
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
                    <button id="btnCerrarSesion" class="user-menu__logout" type="button">Cerrar sesión</button>
                </div>
            `;

            document.getElementById("btnCerrarSesion")?.addEventListener("click", () => {
                window.openLogoutModal();
            });
        };

        window.updateAuthHeader();

        // Interceptar enlaces hacia login, registro o recuperar contraseña en la página
        document.addEventListener("click", (e) => {
            const link = e.target.closest("a");
            if (!link) return;
            const href = link.getAttribute("href") || "";
            if (href.includes("recuperar-contrasena.html")) {
                e.preventDefault();
                window.openAuthModal("forgot");
            } else if (href.includes("login.html")) {
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
        if (authAction === "login" || authAction === "register" || authAction === "forgot") {
            const cleanUrl = window.location.pathname;
            window.history.replaceState({}, document.title, cleanUrl);
            setTimeout(() => window.openAuthModal(authAction), 150);
        }
    });
})();
