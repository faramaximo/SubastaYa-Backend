// wallet.js - Lógica pura de comunicación con el Backend de la Billetera

function getAuthData() {
    const token = localStorage.getItem('token');
    if (!token) return { token: null, userId: null };

    let userId = sessionStorage.getItem('subastaya_user_id');
    if (!userId) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
                || payload.nameid
                || payload.sub
                || payload.id
                || payload.UserId
                || null;
        } catch (e) {
            userId = null;
        }
    }
    return { token, userId };
}

function getWalletApiUrl() {
    return `${API_BASE_URL}/api/v1/wallets`;
}

document.addEventListener('DOMContentLoaded', () => {
    const depositForm = document.getElementById('deposit-form');

    if (depositForm) {
        depositForm.addEventListener('submit', handleDepositSubmit);
    }

    // Cargar los datos de la billetera automáticamente al abrir la página
    loadWalletData();
});

// Cargar saldos e historial al iniciar
async function loadWalletData() {
    const { token, userId } = getAuthData();
    if (!token || !userId) {
        console.warn('Usuario no autenticado para ver la billetera.');
        return;
    }

    await Promise.all([
        fetchWalletBalance(),
        fetchWalletTransactions()
    ]);
}

// Obtener Saldos desde la Web API: GET /api/v1/users/${usuarioId}/wallet
async function fetchWalletBalance() {
    try {
        const { token } = getAuthData();
        const response = await fetch(`${getWalletApiUrl()}/balance`, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        if (!response.ok) throw new Error('Error al consultar saldo');

        const data = await response.json();

        const totalEl = document.getElementById('saldo-total');
        const retenidoEl = document.getElementById('saldo-retenido');
        const disponibleEl = document.getElementById('saldo-disponible');

        if (totalEl) totalEl.textContent = `$${data.saldoTotal.toFixed(2)}`;
        if (retenidoEl) retenidoEl.textContent = `$${data.saldoRetenido.toFixed(2)}`;
        if (disponibleEl) disponibleEl.textContent = `$${data.saldoDisponible.toFixed(2)}`;
    } catch (err) {
        console.error('Error cargando saldos:', err);
    }
}

// Obtener Transacciones del Ledger: GET /api/v1/users/${usuarioId}/wallet/transactions
async function fetchWalletTransactions() {
    try {
        const { token } = getAuthData();
        const response = await fetch(`${getWalletApiUrl()}/transactions`, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        if (!response.ok) throw new Error('Error al consultar historial');

        const transactions = await response.json();
        const tbody = document.getElementById('ledger-table-body');

        if (!transactions || transactions.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: #a1a5b7;">No hay transacciones registradas.</td></tr>';
            return;
        }

        tbody.innerHTML = transactions.map(t => `
            <tr>
                <td>#${t.id}</td>
                <td><span class="badge-tipo ${getBadgeClass(t.tipo)}">${t.tipo}</span></td>
                <td><strong>$${t.monto.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</strong></td>
                <td style="color: #a1a5b7;">${new Date(t.fecha).toLocaleString('es-AR')}</td>
            </tr>
        `).join('');
    } catch (err) {
        console.error('Error cargando transacciones:', err);
    }
}

// Procesar el depósito en la API: POST /api/v1/users/${usuarioId}/wallet/deposits
async function handleDepositSubmit(e) {
    e.preventDefault();
    const amountInput = document.getElementById('deposit-amount');
    const msgDiv = document.getElementById('deposit-message');
    const montoNum = parseFloat(amountInput.value);

    if (isNaN(montoNum) || montoNum <= 0) {
        msgDiv.textContent = 'Ingrese un monto válido mayor a 0.';
        msgDiv.style.color = '#f1416c';
        return;
    }

    msgDiv.textContent = 'Procesando...';
    msgDiv.style.color = '#a1a5b7';

    try {
        const { token } = getAuthData();
        const response = await fetch(`${getWalletApiUrl()}/deposits`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({ monto: montoNum })
        });

        if (response.ok) {
            msgDiv.textContent = '¡Depósito realizado exitosamente!';
            msgDiv.style.color = '#50cd89';
            amountInput.value = '';

            await fetchWalletBalance();
            await fetchWalletTransactions();
        } else {
            const errorText = await response.text();
            console.warn('Respuesta del servidor:', errorText);
            msgDiv.textContent = errorText || 'Error al procesar el depósito.';
            msgDiv.style.color = '#f1416c';
        }
    } catch (err) {
        console.error('Error de red:', err);
        msgDiv.textContent = 'Error de conexión con el servidor.';
        msgDiv.style.color = '#f1416c';
    }
}

function getBadgeClass(tipo) {
    switch (tipo.toLowerCase()) {
        case 'deposito': return 'badge-deposito';
        case 'retencion': return 'badge-retencion';
        case 'liberacion': return 'badge-liberacion';
        default: return '';
    }
}
