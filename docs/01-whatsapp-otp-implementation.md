# WhatsApp OTP Verification Flow

## Overview
Phone Number Verification via WhatsApp OTP integrated into the Clinic API (.NET 9).
Uses the open-source [OpenWA](https://github.com/rmyndharis/OpenWA) gateway to send WhatsApp messages via REST API.

---

## Architecture

### New Interface
**`IWhatsAppOtpService`** — defined in `Clinic.Application/Interfaces/IServices.cs`

```csharp
public interface IWhatsAppOtpService
{
    Task<(bool Success, string Message, string? Code)> RequestOtpAsync(string phoneNumber);
    bool VerifyOtp(string phoneNumber, string code);
    void RemoveOtp(string phoneNumber);
}
```

### Implementation
**`WhatsAppOtpService`** — in `Clinic.Infrastructure/Services/WhatsAppOtpService.cs`

- Uses `HttpClient` to call the OpenWA REST API.
- In-memory OTP store (`ConcurrentDictionary`) with 5-minute expiration.
- In-memory rate-limiter store (`ConcurrentDictionary`) to prevent spam — enforces a configurable cooldown period (default 60 seconds) between OTP requests per phone number.
- Normalizes phone numbers by stripping `+`, `-`, spaces and comparing the last 9 digits.

### DTOs Modified
**`AuthDtos.cs`** — added:
- `WhatsAppOtpRequest` — contains `PhoneNumber`.
- Updated `VerifyOtpRequest` — added optional `PhoneNumber` field.
- Updated `OtpRequest` — added optional `Phone` field.

### API Endpoints
| Endpoint | Method | Description |
|---|---|---|
| `/api/auth/request-otp` | POST | Generate & send OTP via WhatsApp |
| `/api/auth/send-otp` | POST | Smart dispatch: email or WhatsApp based on `@` in input |
| `/api/auth/verify-otp` | POST | Verify OTP — supports both email and WhatsApp flows |
| `/api/auth/register-send-otp` | POST | Send dual OTPs (email + WhatsApp) during registration |

### Configuration (`appsettings.json`)
```json
{
  "WhatsAppOtp": {
    "OpenWaApiUrl": "http://localhost:3000/api/sessions/{session-id}/messages/send-text",
    "ApiKey": "YOUR_API_KEY",
    "SessionId": "YOUR_SESSION_ID",
    "RateLimitSeconds": 60,
    "OtpExpiryMinutes": 5
  }
}
```

### DI Registration (`DependencyInjection.cs`)
```csharp
services.AddHttpClient<IWhatsAppOtpService, WhatsAppOtpService>();
```

---

## Key Design Decisions
1. **In-Memory OTP Storage**: Suitable for single-instance deployments. For multi-instance, migrate to Redis or database.
2. **Rate Limiting**: Per-phone-number cooldown to prevent WhatsApp account bans by Meta.
3. **Phone Normalization**: All phone comparisons strip formatting and compare last 9 digits for reliable matching across international formats.
4. **Dual OTP Flow**: Registration supports both email OTP and WhatsApp OTP simultaneously for enhanced security.

## Files Changed
- `Clinic.Application/Interfaces/IServices.cs` — Added `IWhatsAppOtpService` interface
- `Clinic.Application/DTOs/AuthDtos.cs` — Added `WhatsAppOtpRequest`, updated `VerifyOtpRequest` and `OtpRequest`
- `Clinic.Infrastructure/Services/WhatsAppOtpService.cs` — **[NEW]** Full implementation
- `Clinic.Infrastructure/DependencyInjection.cs` — Registered new service
- `Clinic.API/Controllers/AuthController.cs` — Added WhatsApp OTP endpoints
- `Clinic.API/appsettings.json` — Added WhatsApp configuration section
