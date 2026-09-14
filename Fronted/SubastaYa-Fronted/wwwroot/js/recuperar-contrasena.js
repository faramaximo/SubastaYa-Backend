document.getElementById("forgotPasswordForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const message = document.getElementById("message");
    const button = event.currentTarget.querySelector("button");
    button.disabled = true;
    try {
        const response = await fetch(`${API_BASE_URL}/api/v1/auth/password-resets`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: document.getElementById("email").value.trim() }) });
        const data = await response.json();
        message.textContent = data.mensaje || "Si el correo está registrado, recibirás instrucciones.";
        message.className = "success";
    } catch { message.textContent = "No pudimos procesar la solicitud. Intentá nuevamente."; message.className = "error"; } finally { button.disabled = false; }
});
