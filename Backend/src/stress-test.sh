#!/bin/bash

# Subasta ID y Endpoint RESTful anidado
SUBASTA_ID="${SUBASTA_ID:-1}"
API_URL="${API_URL:-http://localhost:5216/api/v1/auctions/${SUBASTA_ID:-1}/bids}"
MONTO="${MONTO:-150000}"

# Tokens para dos postores distintos
TOKEN_POSTOR1="${TOKEN_POSTOR1:-${TOKEN1:-${TOKEN:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...}}}"
TOKEN_POSTOR2="${TOKEN_POSTOR2:-${TOKEN2:-${TOKEN:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...}}}"

JSON_BODY="{\"monto\": ${MONTO:-150000}}"

echo "Disparando 2 pujas concurrentes para subasta #${SUBASTA_ID}..."

# Peticiones en paralelo simulando dos postores distintos
curl -i -X POST "$API_URL" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN_POSTOR1" -d "$JSON_BODY" &
curl -i -X POST "$API_URL" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN_POSTOR2" -d "$JSON_BODY" &

wait
echo -e "\nPrueba finalizada."