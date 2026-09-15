#!/usr/bin/env bash

# ==============================================================================
# Script de Prueba de Concurrencia - SubastaYa
# Envia dos ofertas de puja simultaneas a la misma subasta en el mismo instante.
# Demuestra la proteccion por control de concurrencia optimista (Optimistic Locking):
#   - Peticion ganadora: HTTP 201 Created
#   - Peticion perdedora: HTTP 409 Conflict
# ==============================================================================

# 1. Configuracion por defecto: Subasta #1 (Activa estandar) y monto superior a $45.000
SUBASTA_ID="${SUBASTA_ID:-1}"
MONTO="${MONTO:-55000}"
API_URL="${API_URL:-http://localhost:5000/api/v1/auctions/${SUBASTA_ID}/bids}"

# 2. Tokens JWT de compradores habilitados (sobreescribibles via variables de entorno)
TOKEN_POSTOR1="${TOKEN_POSTOR1:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQ29tcHJhZG9yIEzDrWRlciIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImNvbXByYWRvcjFAdGVzdC5jb20iLCJleHAiOjE3OTAwMjIzMjQsImlzcyI6IlN1YmFzdGFZYSIsImF1ZCI6IlN1YmFzdGFZYS5Gcm9udGVuZCJ9.FyWoLpOBvVKR6VPNXRTM3P4AXAxQuXHRsLQhcwb3jUg}"
TOKEN_POSTOR2="${TOKEN_POSTOR2:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjMiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQ29tcHJhZG9yIERvcyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImNvbXByYWRvcjJAdGVzdC5jb20iLCJleHAiOjE3OTAwMjIzODQsImlzcyI6IlN1YmFzdGFZYSIsImF1ZCI6IlN1YmFzdGFZYS5Gcm9udGVuZCJ9.MKWlp7S34-flnQFvjQgrYgYEtj8mbeVjNEQuQ3x2jtg}"

JSON_BODY="{\"monto\": ${MONTO}}"

echo "=========================================================="
echo " Disparando 2 pujas simultaneas contra la subasta #${SUBASTA_ID}"
echo " Endpoint: ${API_URL}"
echo " Monto: ${MONTO}"
echo " Postor 1 y Postor 2 concurrentes"
echo "=========================================================="

# Disparar peticiones asincronas en subshells paralelas
curl -s -o /tmp/bid_res1.txt -w "\nPeticion 1 (Postor 1) - HTTP Status: %{http_code}\n" \
  -X POST "${API_URL}" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${TOKEN_POSTOR1}" \
  -d "${JSON_BODY}" &
PID1=$!

curl -s -o /tmp/bid_res2.txt -w "\nPeticion 2 (Postor 2) - HTTP Status: %{http_code}\n" \
  -X POST "${API_URL}" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${TOKEN_POSTOR2}" \
  -d "${JSON_BODY}" &
PID2=$!

# Esperar la finalizacion de ambos procesos concurrentes
wait $PID1
wait $PID2

echo ""
echo "--- Resultado Peticion 1 (Postor 1) ---"
cat /tmp/bid_res1.txt 2>/dev/null || true
echo ""
echo "--- Resultado Peticion 2 (Postor 2) ---"
cat /tmp/bid_res2.txt 2>/dev/null || true
echo ""
echo "Prueba de concurrencia completada."