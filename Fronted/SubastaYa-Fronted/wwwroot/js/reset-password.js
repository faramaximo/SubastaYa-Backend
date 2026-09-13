document.getElementById("resetPasswordForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const password = document.getElementById("password").value;
    const message = document.getElementById("message");
    if (password !== document.getElementById("confirmPassword").value) { message.textContent = "Las contraseñas no coinciden."; message.className = "error"; return; }
    const token = new URLSearchParams(window.location.search).get("token");
    if (!token) { message.textContent = "El enlace de recuperación es inválido."; message.className = "error"; return; }
    const button = event.currentTarget.querySelector("button"); button.disabled = true;
    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/reset-password`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token, password }) });
        const data = await response.json();
        if (!response.ok) throw new Error(data.error);
        message.textContent = data.mensaje; message.className = "success";
        setTimeout(() => window.location.assign("/pages/login.html"), 1500);
    } catch (error) { message.textContent = error.message || "No pudimos actualizar la contraseña."; message.className = "error"; } finally { button.disabled = false; }
});
