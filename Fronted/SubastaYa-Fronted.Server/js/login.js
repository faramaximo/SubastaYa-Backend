import { API_URL } from './config.js';

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

            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.mensaje || "Ocurrió un error en el servidor.");
            }

            const data = await response.json();

            // Guardamos los datos de la sesión exitosa
       

            sessionStorage.setItem("subastaya_user_id", data.id); // o data.usuarioId según tu DTO
            sessionStorage.setItem("subastaya_user_name", data.nombre); // o data.nombreUsuario

            // Redirigimos al catálogo

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