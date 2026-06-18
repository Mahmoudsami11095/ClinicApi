$body1 = @{ email = "msami11095@gmail.com" } | ConvertTo-Json
$response1 = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/send-otp" -Method Post -ContentType "application/json" -Body $body1
Write-Host "OTP Sent: $($response1.otp)"

$body2 = @{ email = "msami11095@gmail.com"; code = $response1.otp } | ConvertTo-Json
try {
    $response2 = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/verify-otp" -Method Post -ContentType "application/json" -Body $body2
    Write-Host "Verify Response: $($response2 | ConvertTo-Json -Depth 5)"
} catch {
    Write-Host "Error: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        Write-Host "Response Body: $($reader.ReadToEnd())"
    }
}
