# GharCraft Environment Setup & Deployment Guide

This document explains the differences between running the application locally on your Mac vs. deploying it to Railway (API) and Neon (PostgreSQL), and how the application seamlessly transitions between both environments.

## 1. Zero-Code-Change Architecture
We have configured the application so that **you do not need to change any C# code** when switching between Local and Remote environments. 

The differences are handled entirely by:
1. **Environment Variables** (set in Railway vs. `launchSettings.json`)
2. **appsettings JSON files** (`appsettings.json` vs. `appsettings.Local.json`)

---

## 2. Local Environment (Mac)

### How it works
When you run `dotnet run` locally:
- Visual Studio / Rider / .NET CLI uses the profile defined in `backend/src/GharCraft.Api/Properties/launchSettings.json`.
- The `ASPNETCORE_ENVIRONMENT` is set to `Development`.
- The application binds to `http://localhost:5062` (as defined in `launchSettings.json`).
- It loads `appsettings.json` and overrides it with `appsettings.Local.json` (if it exists).

### Local Database
In `appsettings.Local.json`, you define your local PostgreSQL connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gharcraft;Username=postgres;Password=postgres"
  }
}
```
*Note: `appsettings.Local.json` is in `.gitignore` to prevent committing your local passwords.*

---

## 3. Remote Environment (Railway + Neon DB)

### How it works
When you push to GitHub, Railway builds the Docker image and starts the container.
- Railway automatically injects a `PORT` environment variable (e.g., `PORT=8080`).
- Railway sets `ASPNETCORE_ENVIRONMENT=Staging` (or Production), which we defined in Railway's Variable settings.
- The `Program.cs` detects the `PORT` variable and dynamically overrides the URL to bind to it:
  ```csharp
  var port = Environment.GetEnvironmentVariable("PORT");
  if (!string.IsNullOrEmpty(port))
  {
      builder.WebHost.UseUrls($"http://+:{port}");
  }
  ```

### Remote Database (Neon)
Instead of a file, we provide the connection string directly in the **Railway Variables Dashboard**:
- **Key:** `ConnectionStrings__DefaultConnection`
- **Value:** `postgresql://neondb_owner:npg_c7S2wOYpLQTR@ep-twilight-cake-azlgm650.c-3.ap-southeast-1.aws.neon.tech/neondb?sslmode=require`

Our `DependencyInjection.cs` detects if the connection string is a URI (`postgresql://...`) and converts it to the proper format for `Npgsql`.

### Required Railway Variables
If you ever deploy a new Railway instance, these exact variables must be added:
```text
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__DefaultConnection=postgresql://... (your neon db url)
Jwt__Audience=GharCraft.Client
Jwt__Issuer=GharCraft.Api
Jwt__Secret=YourSuperSecretKeyMin32Characters!
```

---

## 4. Key Differences Summary

| Feature | Local (Mac) | Remote (Railway + Neon) |
|---|---|---|
| **Port Binding** | `localhost:5062` (from `launchSettings.json`) | `0.0.0.0:8080` (from Railway `$PORT`) |
| **Config Source** | `appsettings.Local.json` | Railway Variables Dashboard |
| **Database** | Local PostgreSQL (`Host=localhost`) | Neon DB (`postgresql://...`) |
| **Health Checks** | Returns `{"status": "Healthy"}` instantly | Railway polls `/healthz`, needs `200 OK` |
| **HTTPS** | N/A (We run HTTP locally) | Handled by Railway's Load Balancer (Proxy) |

## 5. If it stops working on Railway...
If you ever encounter an issue with the deployment:
1. **Check the logs:** Open the Railway Dashboard → Deployments → View Logs.
2. **Verify Variables:** Ensure there are no trailing newlines or spaces in your Railway Variables.
3. **Database Connectivity:** If it crashes on startup, check if the Neon connection string is correct and includes `?sslmode=require`.
