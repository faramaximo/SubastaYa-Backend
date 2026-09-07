// Configuración de la API
const API_CONFIG = {
    // En desarrollo, apunta a tu backend
    baseURL: 'https://localhost:5001', // o el puerto donde corre tu WebApi
    // En producción, usaría la misma URL
};

// Exportar para usar en otros archivos
export const API_URL = API_CONFIG.baseURL;

// También puedes crear funciones helper
export const endpoints = {
    auth: {
        login: `${API_URL}/api/auth/login`,
        register: `${API_URL}/api/auth/register`,
    },
    subastas: {
        getAll: `${API_URL}/api/subastas`,
        create: `${API_URL}/api/subastas`,
    },
    // Agrega más endpoints según necesites
};>>>>>>>>>>>>>