$baseUrl = "http://localhost:5267"
$eventName = "OrderFailed"
$services = @("Checkout", "Payment")
$regions = @("US-East", "US-West")
$errorCodes = @("500", "404", "503")

Write-Host "=========================================================="
Write-Host " SQE Load Test - Advanced Filtering Simulator"
Write-Host " Target Filter: errorCode == '500'"
Write-Host " Target Alert Rule: gte 10 in 60s (15s cooldown)"
Write-Host "=========================================================="

Function Send-Events {
    param([int]$count)
    
    Write-Host "Sending $count events rapidly..."
    $filteredCount = 0
    
    for ($i = 0; $i -lt $count; $i++) {
        $service = $services[$i % $services.Length]
        $region = $regions[$i % $regions.Length]
        
        # 1/3 chance of being 500, 404, or 503
        $code = $errorCodes[$i % $errorCodes.Length]
        
        if ($code -eq "500") {
            $filteredCount++
        }

        $body = @{
            eventName = $eventName
            service = $service
            region = $region
            durationMs = (Get-Random -Minimum 10 -Maximum 500)
            properties = @{
                userId = "user_$(Get-Random -Minimum 1000 -Maximum 9999)"
                errorCode = $code
            }
        } | ConvertTo-Json
        
        Invoke-RestMethod -Method Post -Uri "$baseUrl/api/events" -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue | Out-Null
    }
    
    Write-Host "-> Sent $count total events (approx $filteredCount should pass the filter)."
}

# Sending 30 events total (approx 10 will be 500s).
Write-Host "`n[Phase 1] Sending mixed events (500s, 404s, 503s)."
Send-Events -count 30

Write-Host "`nWaiting 2 seconds..."
Start-Sleep -Seconds 2

$alerts = Invoke-RestMethod "$baseUrl/api/alerts" -ErrorAction SilentlyContinue
Write-Host "`n--- Generated Alerts ---"
if ($alerts.count -gt 0) {
    $alerts.alerts | Select-Object QueryName, Message, CurrentValue, Timestamp | Format-Table -AutoSize
} else {
    Write-Host "No alerts yet. (This is expected if threshold 10 per dimension hasn't been breached)"
}

Write-Host "`n[Phase 2] Sending 60 more events to breach the threshold."
Send-Events -count 60
Start-Sleep -Seconds 2

$alerts = Invoke-RestMethod "$baseUrl/api/alerts" -ErrorAction SilentlyContinue
Write-Host "`n--- Final Alerts ---"
if ($alerts.count -gt 0) {
    $alerts.alerts | Select-Object QueryName, Message, CurrentValue, Timestamp | Format-Table -AutoSize
} else {
    Write-Host "No alerts!"
}

$metrics = Invoke-RestMethod "$baseUrl/api/metrics" -ErrorAction SilentlyContinue
Write-Host "`n--- Aggregated Metrics ---"
if ($metrics.count -gt 0) {
    $metrics.metrics | Select-Object Name, Value, Timestamp | Format-Table -AutoSize
}
