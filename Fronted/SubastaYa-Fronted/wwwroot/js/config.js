// La interfaz se sirve desde WebApi, por lo que API y sitio comparten origen.
// Evita depender de un puerto fijo al ejecutar con F5 o al publicar.
const API_BASE_URL = window.location.origin;
