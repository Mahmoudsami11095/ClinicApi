# Azure Troubleshooting Guide

A collection of all issues encountered during deployment and their verified solutions.

---

## 1. Database Transient Failure (Sleep/Pause)

### Error
```
System.InvalidOperationException: An exception has been raised that is likely due to a transient failure.
Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.
---> Microsoft.Data.SqlClient.SqlException: Database 'free-sql-db' on server '...' is not currently available.
```

### Cause
The Azure SQL free tier automatically pauses the database after ~1 hour of inactivity to save resources. When a request comes in after pausing, the database needs a few seconds to "wake up", causing the first connection attempt to fail.

### Solution
Add `EnableRetryOnFailure` to the SQL Server configuration in `DependencyInjection.cs`:

```csharp
services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    )
);
```

### Impact
None for production use. EF Core will transparently retry failed connections. The first request after a long idle period may take 5-15 seconds, but subsequent requests will be instant.

---

## 2. JWT Token 500 Internal Server Error

### Error
Login, OTP verification, and social login all return `500 Internal Server Error` after successful authentication.

### Cause
Azure App Service environment variables were set with **single underscores** instead of **double underscores**.

```
❌ Jwt_Secret       → Azure cannot map this to Jwt:Secret
✅ Jwt__Secret      → Azure correctly maps this to Jwt:Secret
```

Without the correct JWT configuration, the `JwtService.GenerateToken()` method crashes when trying to create the HMAC-SHA256 signing key.

### Solution
In Azure Portal → App Service → Settings → Environment Variables, ensure ALL nested config keys use **double underscores** (`__`):

| ❌ Wrong | ✅ Correct |
|---|---|
| `Jwt_Secret` | `Jwt__Secret` |
| `Jwt_Issuer` | `Jwt__Issuer` |
| `Jwt_Audience` | `Jwt__Audience` |
| `Jwt_ExpiryHours` | `Jwt__ExpiryHours` |

### How We Diagnosed This
The Angular frontend displayed "Invalid verification code" even though the OTP was correct. By testing the API endpoints directly with PowerShell:
1. `POST /api/auth/send-otp` → returned 200 OK with valid OTP code
2. `POST /api/auth/verify-otp` with the same OTP → returned 500 Internal Server Error

This proved the OTP verification itself worked, but the JWT token generation step that follows it was crashing. The registration endpoint worked because it doesn't generate a token.

---

## 3. Firewall / IP Access Denied

### Error
```
Microsoft.Data.SqlClient.SqlException: Cannot open server 'clinic-app-server-123' requested by the login.
Client with IP address 'X.X.X.X' is not allowed to access the server.
```

### Cause
Azure SQL Server has a firewall that blocks all connections by default. Your development machine's IP and Azure App Service's IPs need to be explicitly allowed.

### Solution
In Azure Portal → SQL Server → Networking:
1. ✅ Check "Allow Azure services and resources to access this server"
2. Click "Add your client IPv4 address" to allow your development machine
3. Click Save

---

## 4. Google Social Login `redirect_uri_mismatch`

### Error
When clicking "Sign in with Google" on the deployed app, Google shows:
```
Error 400: redirect_uri_mismatch
```

### Cause
The deployed frontend URL (e.g., `https://clinic-app-ten-topaz.vercel.app`) is not registered in Google Cloud Console as an authorized origin/redirect URI.

### Solution
1. Go to [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials
2. Edit your OAuth 2.0 Client ID
3. Add to **Authorized JavaScript origins**:
   - `https://your-app.vercel.app`
4. Add to **Authorized redirect URIs**:
   - `https://your-app.vercel.app`
   - `https://your-app.vercel.app/login`

> **Note**: Changes may take 5-10 minutes to propagate in Google's systems.

---

## 5. CORS Errors

### Error
```
Access to XMLHttpRequest at 'https://...' from origin 'https://...' has been blocked by CORS policy
```

### Cause
The frontend URL is not listed in the backend's allowed CORS origins.

### Solution
Two places to update:

**1. `appsettings.json` (for local development):**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://your-vercel-app.vercel.app"
    ]
  }
}
```

**2. Azure Environment Variables (for production):**
```
Cors__AllowedOrigins__0 = http://localhost:4200
Cors__AllowedOrigins__1 = https://your-vercel-app.vercel.app
```

**3. Azure Portal → App Service → API → CORS:**
Add your frontend URL to the allowed origins list.

---

## 6. Azure App Service Deployment Slot Setting

### Question
"Should I select deployment slot setting?" when adding environment variables in Azure.

### Answer
**No.** Leave "Deployment slot setting" unchecked for all variables.

Deployment slot settings are only needed when you have multiple deployment slots (e.g., staging vs production). On the free F1 tier, you only have one slot, so this setting has no effect. Checking it won't cause harm but is unnecessary.

---

## 7. Debug Endpoint

A diagnostic endpoint was added to `Program.cs` to check for startup errors when the app is deployed:

```csharp
app.MapGet("/api/debug-error", () => 
{
    if (System.IO.File.Exists("startup-error.txt"))
        return Results.Text(System.IO.File.ReadAllText("startup-error.txt"));
    return Results.Ok("No startup errors found!");
});
```

Access it at: `https://your-app.azurewebsites.net/api/debug-error`

To view real-time logs: Azure Portal → App Service → **Log stream** (under Monitoring section).
