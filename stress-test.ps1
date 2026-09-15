# ==============================================================================
# Script de Prueba de Concurrencia (PowerShell) - SubastaYa
# Envia dos ofertas de puja simultaneas a la misma subasta en el mismo instante
# simulando dos postores distintos.
# Demuestra la proteccion por control de concurrencia optimista / locking:
#   - Peticion ganadora: HTTP 201 Created
#   - Peticion perdedora: HTTP 409 Conflict
# ==============================================================================

$SubastaId = if ($env:SUBASTA_ID) { $env:SUBASTA_ID } else { "1" }
$ApiUrl = if ($env:API_URL) { $env:API_URL } else { "http://localhost:5216/api/v1/auctions/$SubastaId/bids" }
$Monto = if ($env:MONTO) { [decimal]$env:MONTO } else { 150000 }

$Token1 = if ($env:TOKEN_POSTOR1) { $env:TOKEN_POSTOR1 } elseif ($env:TOKEN1) { $env:TOKEN1 } else { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
$Token2 = if ($env:TOKEN_POSTOR2) { $env:TOKEN_POSTOR2 } elseif ($env:TOKEN2) { $env:TOKEN2 } else { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }

$jsonBody = @{ monto = $Monto } | ConvertTo-Json

Write-Host "=========================================================="
Write-Host " Disparando 2 pujas simultaneas contra la subasta #$SubastaId"
Write-Host " Endpoint: $ApiUrl"
Write-Host " Monto: $Monto"
Write-Host " Postor 1 y Postor 2 concurrentes"
Write-Host "=========================================================="

$scriptBlock = {
    param($url, $token, $body, $bidderName)
    $headers = @{
        "Content-Type"  = "application/json"
        "Authorization" = "Bearer $token"
    }
    try {
        $response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body $body -UseBasicParsing -TimeoutSec 10
        [PSCustomObject]@{
            Bidder     = $bidderName
            StatusCode = [int]$response.StatusCode
            Content    = $response.Content
        }
    }
    catch {
        $resp = $_.Exception.Response
        $status = if ($resp) { [int]$resp.StatusCode } else { 0 }
        $stream = if ($resp) { $resp.GetResponseStream() } else { $null }
        $content = ""
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $content = $reader.ReadToEnd()
        }
        [PSCustomObject]@{
            Bidder     = $bidderName
            StatusCode = $status
            Content    = $content
            Error      = $_.Exception.Message
        }
    }
}

$job1 = Start-Job -ScriptBlock $scriptBlock -ArgumentList $ApiUrl, $Token1, $jsonBody, "Postor 1"
$job2 = Start-Job -ScriptBlock $scriptBlock -ArgumentList $ApiUrl, $Token2, $jsonBody, "Postor 2"

$results = Wait-Job -Job $job1, $job2 | Receive-Job
Remove-Job -Job $job1, $job2

foreach ($res in $results) {
    Write-Host "`n--- Resultado $($res.Bidder) ---"
    Write-Host "HTTP Status: $($res.StatusCode)"
    Write-Host "Respuesta: $($res.Content)"
}

Write-Host "`nPrueba de concurrencia completada."
