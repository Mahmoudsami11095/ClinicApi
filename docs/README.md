# Clinic App Documentation

All implementation plans, deployment guides, and troubleshooting documentation for the Clinic App.

## 📋 Documents

| # | Document | Description |
|---|---|---|
| 01 | [WhatsApp OTP Implementation](01-whatsapp-otp-implementation.md) | Architecture and implementation details for the WhatsApp OTP verification flow |
| 02 | [Deployment Guide](02-deployment-guide.md) | Complete step-by-step guide for deploying the full stack (Azure + Vercel + Render) |
| 03 | [Azure Troubleshooting](03-azure-troubleshooting.md) | All issues encountered during Azure deployment and their verified solutions |
| 04 | [Authentication Architecture](04-authentication-architecture.md) | Full authentication system design: Email/Password, OTP, WhatsApp, Google OAuth, JWT |
| 05 | [Azure Environment Variables](05-azure-environment-variables.md) | Complete reference for all required Azure App Service environment variables |

## 🏗️ Tech Stack

| Component | Technology | Hosting |
|---|---|---|
| Frontend | Angular 20+ (Standalone Components, Signals) | Vercel (free) |
| Backend API | .NET 9 / ASP.NET Core | Azure App Service (free F1) |
| Database | SQL Server / EF Core 9 | Azure SQL (free tier) |
| WhatsApp Bot | Node.js / OpenWA | Render.com (free) or Local+ngrok |

## 🔗 Key URLs

| Service | URL Pattern |
|---|---|
| Azure Backend | `https://clinic-api-123-*.azurewebsites.net` |
| Vercel Frontend | `https://clinic-app-*.vercel.app` |
| Azure SQL Server | `clinic-app-server-123.database.windows.net` |
| Debug Endpoint | `https://your-backend/api/debug-error` |
| Swagger (dev only) | `https://your-backend/swagger` |
