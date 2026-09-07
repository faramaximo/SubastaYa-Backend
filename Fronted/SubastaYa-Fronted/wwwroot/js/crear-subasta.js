document.addEventListener("DOMContentLoaded", () => {

    // ==========================================
    // PREVISUALIZACIÓN EN VIVO DE LA IMAGEN
    // ==========================================
    const urlImagenInput = document.getElementById("urlImagen");
    const previewImagen = document.getElementById("previewImagen");
    const imagenPreview = document.getElementById("imagenPreview");

    urlImagenInput.addEventListener("input", () => {
        const url = urlImagenInput.value.trim();

        if (!url) {
            previewImagen.classList.add("d-none");
            return;
        }

        imagenPreview.src = url;
        previewImagen.classList.remove("d-none");
    });

    imagenPreview.addEventListener("error", () => {
        previewImagen.classList.add("d-none");
    });
    // ==========================================

    const form = document.getElementById("formCrearSubasta");
    const alerta = document.getElementById("alertaFormulario");
    const btnSubmit = document.getElementById("btnSubmit");

    form.addEventListener("submit", async (e) => {
        e.preventDefault(); 
        ocultarAlerta();

        // 1. Capturar datos del DOM
        const titulo = document.getElementById("titulo").value;
        const urlImagen = document.getElementById("urlImagen").value;
        const descripcion = document.getElementById("descripcion").value;
        const categoriaId = parseInt(document.getElementById("categoriaId").value);
        const precioBase = parseFloat(document.getElementById("precioBase").value);
        const incrementoMinimo = parseFloat(document.getElementById("incrementoMinimo").value);
        
        const fechaInicioStr = document.getElementById("fechaInicio").value;
        const fechaFinStr = document.getElementById("fechaFin").value;

        // 2. Validaciones de Negocio (Módulo 2)
        if (!titulo.trim() || !urlImagen.trim() || !descripcion.trim() || !fechaInicioStr || !fechaFinStr || isNaN(categoriaId) || isNaN(precioBase) || isNaN(incrementoMinimo)) {
            mostrarAlerta("Error: Por favor completá todos los campos marcados con asterisco (*).", "danger");
            return;
        }

        if (descripcion.trim().length < 10) {
            mostrarAlerta("Error: La descripción del artículo debe tener al menos 10 caracteres.", "danger");
            return;
        }

        if (precioBase <= 0 || incrementoMinimo <= 0) {
            mostrarAlerta("Error: El precio base y el incremento deben ser mayores a 0.", "danger");
            return;
        }

        // 👇 CREAMOS LAS VARIABLES DE FECHA ANTES DE USARLAS 👇
        const inicio = new Date(fechaInicioStr);
        const fin = new Date(fechaFinStr);
        const ahora = new Date(); 
        
        if (inicio < ahora) {
            mostrarAlerta("Error: La fecha de inicio no puede estar en el pasado.", "danger");
            return;
        }

        if (fin <= inicio) {
            mostrarAlerta("Error: La fecha de cierre debe ser posterior a la fecha de inicio.", "danger");
            return;
        }
        // 👆 FIN DE LAS VALIDACIONES TEMPORALES 👆

        // 3. Armar el Data Transfer Object (DTO)
        const nuevaSubasta = {
            titulo: titulo,
            descripcion: descripcion,
            urlImagen: urlImagen,
            categoriaId: categoriaId,
            vendedorId: parseInt(sessionStorage.getItem("subastaya_user_id")),
            precioBase: precioBase,
            incrementoMinimo: incrementoMinimo,
            fechaInicio: inicio.toISOString(), 
            fechaFin: fin.toISOString()
        };

        // 4. Enviar a la API (Capa de Red)
        try {
            btnSubmit.disabled = true;
            btnSubmit.innerText = "Publicando...";

            const response = await fetch(`${API_BASE_URL}/api/auctions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(nuevaSubasta)
            });

            if (!response.ok) throw new Error("Ocurrió un error al intentar publicar la subasta en el servidor.");

            mostrarAlerta("¡Subasta publicada con éxito! Ya está disponible en el catálogo.", "success");
            form.reset(); 
            previewImagen.classList.add("d-none");
            
        } catch (error) {
            mostrarAlerta(error.message, "danger");
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.innerText = "Publicar Subasta";
        }
    });

    // Funciones de UI
    function mostrarAlerta(mensaje, tipo) {
        alerta.className = `alert alert-${tipo} mb-4 shadow-sm`;
        alerta.innerText = mensaje;
        alerta.classList.remove("d-none");
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function ocultarAlerta() {
        alerta.classList.add("d-none");
    }
});