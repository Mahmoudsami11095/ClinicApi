# Azure Environment Variables Reference

Complete list of all environment variables needed in Azure App Service for the Clinic API.

> **CRITICAL RULE**: Azure uses **DOUBLE underscores** (`__`) to represent nested JSON keys.  
> Example: `Jwt:Secret` in `appsettings.json` → `Jwt__Secret` in Azure.

---

## Required Variables

### Connection Strings
| Name | Example Value | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:clinic-app-server-123.database.windows.net,1433;Initial Catalog=free-sql-db;Persist Security Info=False;User ID=clinic_admin;Password=Clinic@2024!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` | Azure SQL connection string |

### JWT Authentication
| Name | Value | Notes |
|---|---|---|
| `Jwt__Secret` | `YourSuperSecretKeyForClinicApi2026!AtLeast32CharsLong` | Must be ≥32 characters |
| `Jwt__Issuer` | `ClinicApi` | Must match frontend expectations |
| `Jwt__Audience` | `ClinicApp` | Must match frontend expectations |
| `Jwt__ExpiryHours` | `24` | Token lifetime in hours |

### SMTP (Email)
| Name | Value | Notes |
|---|---|---|
| `Smtp__Server` | `smtp.gmail.com` | Gmail SMTP server |
| `Smtp__Port` | `587` | TLS port |
| `Smtp__SenderEmail` | `your-email@gmail.com` | "From" address |
| `Smtp__SenderName` | `Clinic Support` | Display name |
| `Smtp__Username` | `your-email@gmail.com` | SMTP auth username |
| `Smtp__Password` | `xxxx xxxx xxxx xxxx` | Gmail App Password (NOT your regular password) |
| `Smtp__EnableSsl` | `true` | Enable TLS |

### CORS
| Name | Value | Notes |
|---|---|---|
| `Cors__AllowedOrigins__0` | `http://localhost:4200` | Local Angular dev |
| `Cors__AllowedOrigins__1` | `https://clinic-app-ten-topaz.vercel.app` | Production frontend URL |

### Google OAuth
| Name | Value | Notes |
|---|---|---|
| `Authentication__Google__ClientId` | `933605871994-xxx.apps.googleusercontent.com` | From Google Cloud Console |

### WhatsApp OTP
| Name | Value | Notes |
|---|---|---|
| `WhatsAppOtp__OpenWaApiUrl` | `https://your-bot.onrender.com/api/sessions/{session-id}/messages/send-text` | OpenWA API endpoint |
| `WhatsAppOtp__ApiKey` | `YOUR_API_KEY` | OpenWA auth key |
| `WhatsAppOtp__SessionId` | `your-session-uuid` | WhatsApp session ID |
| `WhatsAppOtp__RateLimitSeconds` | `60` | Cooldown between OTP requests |
| `WhatsAppOtp__OtpExpiryMinutes` | `5` | OTP validity period |

---

## Gmail App Password Setup

To use Gmail for sending OTP emails:

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable **2-Step Verification** (required)
3. Go to [App Passwords](https://myaccount.google.com/apppasswords)
4. Select "Mail" and "Windows Computer"
5. Click "Generate"
6. Use the 16-character password (format: `xxxx xxxx xxxx xxxx`) as `Smtp__Password`

> **Note**: Regular Gmail passwords will NOT work. You must use an App Password.

---

## How to Add Variables in Azure Portal

1. Azure Portal → Your App Service → **Settings** → **Environment variables**
2. Click **+ Add** for each variable
3. Enter the Name and Value
4. Leave "Deployment slot setting" **unchecked**
5. After adding all variables, click **Apply**
6. The app will restart automatically

### Advanced Edit (Faster Method)
1. Click **Advanced edit** button
2. Paste JSON array format:
```json
[
  { "name": "Jwt__Secret", "value": "YourSuperSecretKeyForClinicApi2026!AtLeast32CharsLong", "slotSetting": false },
  { "name": "Jwt__Issuer", "value": "ClinicApi", "slotSetting": false },
  { "name": "Jwt__Audience", "value": "ClinicApp", "slotSetting": false },
  { "name": "Jwt__ExpiryHours", "value": "24", "slotSetting": false }
]
```
3. Click OK → Apply
