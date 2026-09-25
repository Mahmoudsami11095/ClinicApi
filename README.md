# ClinicApi - Smart Clinic Backend REST API

ASP.NET Core Web API built on **Clean Architecture** principles serving the Smart Clinic Management System.

> 📚 **Complete Documentation Portal:** Complete architecture specifications, ER diagrams, REST API contracts, and UAT test plans are maintained in the **[clinic-docs](https://github.com/Mahmoudsami11095/clinic-docs)** repository.

---

## 🌐 Live Production Endpoints

- **Live Azure API Host:** [`https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api`](https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api)
- **Health Check Probe:** [`/api/health`](https://clinic-api-123-a0ghf9aeb5ccawha.swedencentral-01.azurewebsites.net/api/health)
- **Frontend App:** [https://clinic-app-ten-topaz.vercel.app/login](https://clinic-app-ten-topaz.vercel.app/login)

---

## 🏛️ Architecture Overview

The solution adheres to Clean Architecture with 4 distinct layers:
1. **`Clinic.API`**: REST controllers, SignalR hubs, JWT authentication middleware, and action filters.
2. **`Clinic.Application`**: Application use cases, DTOs, business interfaces, and validators.
3. **`Clinic.Domain`**: Core enterprise domain entities (`Patient`, `Doctor`, `DentalLog`, `Appointment`, `Prescription`, `BillingRecord`) and domain enums.
4. **`Clinic.Infrastructure`**: Entity Framework Core persistence with Azure SQL, database seeders, and SignalR notification dispatchers.
