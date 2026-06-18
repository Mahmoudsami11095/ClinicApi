# Full-Stack Deployment Guide

## Overview
Complete guide for deploying the Clinic App stack to the cloud using free tiers:
- **Frontend (Angular)**: Vercel (free)
- **Backend (.NET 9 API)**: Azure App Service (free F1 tier)
- **Database**: Azure SQL (free tier — 100k vCore seconds/month)
- **WhatsApp Bot (Node.js)**: Render.com (free) or local with ngrok

---

## Step 1: Host the Database (Azure SQL)

1. Go to the [Azure Portal](https://portal.azure.com/) and create a free account.
2. Search for **Azure SQL** → Click **Create**.
3. Select **SQL Database** → Choose **Apply offer** under free tier (100k vCore seconds).
4. Create a new SQL Server:
   - Choose a server name (e.g., `clinic-app-server-123`)
   - Set admin login and password (e.g., `clinic_admin` / `Clinic@2024!`)
   - Select a region close to you
5. Go to the database **Networking** tab:
   - ✅ Enable "Allow Azure services and resources to access this server"
   - ✅ Add your client IPv4 address
6. Copy the **ADO.NET Connection String** and replace `{your_password}` with your actual password.

> [!IMPORTANT]
> The free Azure SQL tier will pause the database after inactivity. This causes a `Database is not currently available` error on first reconnect. The fix is `EnableRetryOnFailure` (see Troubleshooting section).

---

## Step 2: Host the Backend (.NET Web API on Azure App Service)

1. In Azure Portal → **App Services** → **Create → Web App**.
2. Settings:
   - **Publish**: Code
   - **Runtime stack**: .NET 9
   - **Pricing plan**: Free F1
3. After creation → **Deployment Center** → Link GitHub → Select `ClinicApi` repo → `master` branch.
4. Go to **Settings → Environment Variables** and add these App Settings:

| Name (use DOUBLE underscores `__`) | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | Your Azure SQL connection string |
| `Jwt__Secret` | `YourSuperSecretKeyForClinicApi2026!AtLeast32CharsLong` |
| `Jwt__Issuer` | `ClinicApi` |
| `Jwt__Audience` | `ClinicApp` |
| `Jwt__ExpiryHours` | `24` |
| `Smtp__Server` | `smtp.gmail.com` |
| `Smtp__Port` | `587` |
| `Smtp__SenderEmail` | `your-email@gmail.com` |
| `Smtp__SenderName` | `Clinic Support` |
| `Smtp__Username` | `your-email@gmail.com` |
| `Smtp__Password` | `your-app-password` |
| `Smtp__EnableSsl` | `true` |
| `WhatsAppOtp__OpenWaApiUrl` | Your WhatsApp bot URL |
| `Cors__AllowedOrigins__0` | `http://localhost:4200` |
| `Cors__AllowedOrigins__1` | `https://your-vercel-app.vercel.app` |
| `Authentication__Google__ClientId` | Your Google OAuth Client ID |

5. Click **Apply** → App restarts automatically.

> [!CAUTION]
> **CRITICAL**: Azure uses DOUBLE underscores (`__`) to represent nested JSON keys.  
> `Jwt:Secret` in appsettings.json → `Jwt__Secret` in Azure Environment Variables.  
> Using a single underscore (`Jwt_Secret`) will NOT work and will cause 500 Internal Server errors on login/OTP verification!

---

## Step 3: Host the Frontend (Angular on Vercel)

1. Go to [Vercel](https://vercel.com/) and sign in with GitHub.
2. Import the `Clinic` (Angular) repository.
3. Set Build Settings:
   - **Framework Preset**: Other
   - **Build Command**: `ng build --configuration production`
   - **Output Directory**: `dist/clinic-app/browser`
4. Add Environment Variable:
   - `API_URL` = `https://your-azure-app.azurewebsites.net`
5. Deploy.
6. After deployment, copy the Vercel URL and add it to your Azure Backend's CORS settings AND the `Cors__AllowedOrigins__X` environment variable.

### Angular Production Environment (`environment.prod.ts`)
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-azure-backend.azurewebsites.net'
};
```

---

## Step 4: Host the WhatsApp Bot (Render.com)

1. Go to [Render.com](https://render.com/) and sign in with GitHub.
2. Click **New → Web Service**.
3. Connect the repository that contains your WhatsApp bot code.
4. Render will automatically detect Node.js. Select the **Free** instance type.
5. Once deployed, Render gives you a URL (e.g., `https://my-whatsapp-bot.onrender.com`).
6. Update your Azure Backend App Service **Environment Variables** with this new URL (`WhatsAppOtp__OpenWaApiUrl`).

> [!NOTE]
> **Why Render instead of Azure for the WhatsApp Bot?**
> - WhatsApp bots use headless Chromium (Puppeteer) which Azure App Service sandboxes block.
> - Render provides native Linux containers that support headless browsers perfectly.
> - Render's free tier gives 512MB dedicated RAM, sufficient for the browser process.

### Alternative: Run Locally with ngrok
If you can't use Render (requires credit card):
1. Run the bot locally: `npm start`
2. Install ngrok: `npm install -g ngrok`
3. Run: `ngrok http 3000`
4. Use the ngrok URL in your Azure `WhatsAppOtp__OpenWaApiUrl` environment variable.

---

## Step 5: Google OAuth Configuration

For social login to work on your deployed frontend:

1. Go to [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials.
2. Edit your OAuth 2.0 Client ID.
3. Add your Vercel URL to **Authorized JavaScript origins**:
   - `https://your-app.vercel.app`
4. Add to **Authorized redirect URIs**:
   - `https://your-app.vercel.app`
   - `https://your-app.vercel.app/login`

> [!WARNING]
> If you see `redirect_uri_mismatch` error, the origin URL in Google Console must match EXACTLY (including `https://` and NO trailing slash).

---

## Codebase Preparation Changes

### Files Modified for Production
| File | Change |
|---|---|
| `Clinic/src/environments/environment.prod.ts` | **[NEW]** Production API URL |
| `ClinicApi/src/Clinic.Infrastructure/DependencyInjection.cs` | Added `EnableRetryOnFailure` to SQL Server config |
| `ClinicApi/src/Clinic.API/Program.cs` | Added `/api/debug-error` diagnostic endpoint |
| `ClinicApi/src/Clinic.API/appsettings.json` | Added Vercel URL to CORS, added WhatsApp config |
| `ClinicApi/.github/workflows/master_clinic-api-123.yml` | **[NEW]** GitHub Actions CI/CD for Azure deployment |
