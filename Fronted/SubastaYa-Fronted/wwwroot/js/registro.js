document.addEventListener("DOMContentLoaded", () => {
    const formRegistro = document.getElementById("formRegistro");
    const msjBox = document.getElementById("registroMensaje");
    const btnSubmit = document.getElementById("btnSubmit");

    formRegistro.addEventListener("submit", async (e) => {
        e.preventDefault();
        
        // Limpiamos mensajes anteriores
        msjBox.classList.add("d-none");
        msjBox.classList.remove("alert-danger", "alert-success");
        
        const nombre = document.getElementById("nombre").value.trim();
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value.trim();

        try {
            btnSubmit.disabled = true;
            btnSubmit.innerText = "Creando cuenta...";

            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ nombre, email, password })
            });

            // Intentamos leer la respuesta del servidor de forma segura
            let data = {};
            try {
                data = await response.json();
            } catch (err) {
                // Si entra acá, es porque C# no devolvió un JSON (ej: un error de compilación)
                console.error("Respuesta no JSON del servidor");
            }

            // Verificamos si el servidor nos dio un código de error (400, 404, 409, 500)
            if (!response.ok) {
                throw new Error(data.mensaje || "Error de conexión. Revisá si el backend está corriendo sin errores.");
            }

            // Registro exitoso
            msjBox.innerText = "Cuenta creada con éxito. Redirigiendo al ingreso...";
            msjBox.classList.add("alert-success");
            msjBox.classList.remove("d-none", "alert-danger");

            // Esperamos 2 segundos para que el usuario lea el cartel y lo mandamos a loguearse
            setTimeout(() => {
                window.location.href = "/pages/login.html";
            }, 2000);
            
        } catch (error) {
            // Mostramos el error (Ej: El correo ya existe o el servidor está apagado)
            msjBox.innerText = error.message;
            msjBox.classList.add("alert-danger");
            msjBox.classList.remove("d-none", "alert-success");
            btnSubmit.disabled = false;
            btnSubmit.innerText = "Crear Cuenta";
        }
    });
});
