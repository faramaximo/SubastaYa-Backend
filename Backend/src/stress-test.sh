#!/bin/bash

# URL apuntando a tu puerto 5216
API_URL="http://localhost:5216/api/bids"

# Aquí deberás pegar un token real generado al iniciar sesión
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjkiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiMSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Ijk4N2FndXN0aW45ODdAZ21haWwuY29tIiwiZXhwIjoxNzg5ODI4NTUzLCJpc3MiOiJTdWJhc3RhWWEiLCJhdWQiOiJTdWJhc3RhWWEuRnJvbnRlbmQifQ.3NPmvf1T7p2FwYkB4bglPn-ieEQ3yzSq34jDmmGutMQ"

# Monto válido para pujar en la subasta con ID 1
JSON_BODY='{"subastaId": 11, "monto": 1000}'

echo "Disparando pujas concurrentes..."

# Peticiones en paralelo
curl -i -X POST $API_URL -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d "$JSON_BODY" &
curl -i -X POST $API_URL -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d "$JSON_BODY" &

wait
echo -e "\nPrueba finalizada."