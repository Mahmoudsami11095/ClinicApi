$email = "test777@gmail.com"
$body1 = @{ email = $email } | ConvertTo-Json
$response1 = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/register-send-otp" -Method Post -ContentType "application/json" -Body $body1
$otp = $response1.emailOtp

$body2 = @{ name = "Test User"; email = $email; role = "Patient"; password = "Password123!"; otpCode = $otp } | ConvertTo-Json
try {
    $response2 = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/register" -Method Post -ContentType "application/json" -Body $body2
    Write-Host "Register Response: $($response2 | ConvertTo-Json -Depth 5)"
} catch {
    Write-Host "Register Error: $_"
}

$body3 = @{ email = $email; password = "Password123!" } | ConvertTo-Json
try {
    $response3 = Invoke-RestMethod -Uri "https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/auth/login" -Method Post -ContentType "application/json" -Body $body3
    Write-Host "Login Response: $($response3 | ConvertTo-Json -Depth 5)"
} catch {
    Write-Host "Login Error: $_"
}
