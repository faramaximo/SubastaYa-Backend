#!/usr/bin/env bash

# ==============================================================================
# Script de Prueba de Concurrencia - SubastaYa
# Envia dos ofertas de puja simultaneas a la misma subasta en el mismo instante.
# Demuestra la proteccion por control de concurrencia optimista / locking:
#   - Peticion ganadora: HTTP 200 OK
#   - Peticion perdedora: HTTP 409 Conflict
# ==============================================================================

API_URL="${API_URL:-http://localhost:5216/api/v1/bids}"
SUBASTA_ID="${SUBASTA_ID:-1}"
MONTO="${MONTO:-150000}"

# Token JWT (puede pasarse como variable de entorno TOKEN o reemplazarse aqui)
TOKEN="${TOKEN:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...}"

JSON_BODY="{\"subastaId\": ${SUBASTA_ID}, \"monto\": ${MONTO}}"

echo "=========================================================="
echo " Disparando 2 pujas simultaneas contra la subasta #${SUBASTA_ID}"
echo " Endpoint: ${API_URL}"
echo " Monto: ${MONTO}"
echo "=========================================================="

# Disparar peticiones asincronas en subshells paralelas
curl -s -o /tmp/bid_res1.txt -w "\nPeticion 1 - HTTP Status: %{http_code}\n" \
  -X POST "${API_URL}" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${TOKEN}" \
  -d "${JSON_BODY}" &
PID1=$!

curl -s -o /tmp/bid_res2.txt -w "\nPeticion 2 - HTTP Status: %{http_code}\n" \
  -X POST "${API_URL}" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${TOKEN}" \
  -d "${JSON_BODY}" &
PID2=$!

# Esperar la finalizacion de ambos procesos concurrentes
wait $PID1
wait $PID2

echo ""
echo "--- Resultado Peticion 1 ---"
cat /tmp/bid_res1.txt 2>/dev/null || true
echo ""
echo "--- Resultado Peticion 2 ---"
cat /tmp/bid_res2.txt 2>/dev/null || true
echo ""
echo "Prueba de concurrencia completada."
