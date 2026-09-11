document.addEventListener("DOMContentLoaded", () => {
    const formLogin = document.getElementById("formLogin");
    const errorBox = document.getElementById("loginError");
    const btnSubmit = document.getElementById("btnSubmit");

    formLogin.addEventListener("submit", async (e) => {
        e.preventDefault();
        errorBox.classList.add("d-none");
        
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value.trim();

    try {
            btnSubmit.disabled = true;
            btnSubmit.innerText = "Verificando...";

            // 1. AQUI ESTÁ LA MAGIA: Le agregamos la variable API_BASE_URL
            const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            // 2. Manejo seguro de errores
            if (!response.ok) {
                // Intentamos leer el JSON, pero si falla (ej. error 404 o 405), no rompemos todo
                let errorMessage = "Ocurrió un error en el servidor.";
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.error || errorData.mensaje || errorMessage;
                } catch (e) {
                    // Si no era JSON, nos quedamos con el mensaje genérico
                    console.error("El servidor devolvió un error sin formato JSON");
                }
                throw new Error(errorMessage);
            }

            const data = await response.json();

            // Guardamos los datos de la sesión exitosa
            if (!data?.id || !data?.token) throw new Error("No se pudo iniciar una sesión válida.");
            sessionStorage.setItem("subastaya_user_id", String(data.id));
            sessionStorage.setItem("subastaya_user_name", data.nombre || "Usuario");
            localStorage.setItem('token', data.token);

            // Redirigimos al catálogo
            window.location.href = "/index.html";
            
        } catch (error) {
            errorBox.innerText = error.message;
            errorBox.classList.remove("d-none");
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.innerText = "Ingresar al Sistema";
        }
    });
});
