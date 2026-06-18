$body = @{ email = "msami11095@gmail.com"; password = "SomePassword123" } | ConvertTo-Json
try {
    $response = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/login" -Method Post -ContentType "application/json" -Body $body
    Write-Host "Login Response: $($response | ConvertTo-Json -Depth 5)"
} catch {
    Write-Host "Error: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        Write-Host "Response Body: $($reader.ReadToEnd())"
    }
}
