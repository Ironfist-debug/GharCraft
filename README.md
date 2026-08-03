# GharCraft — Furniture Ecommerce Platform

GharCraft is a high-performance, single-branch furniture ecommerce platform designed with Clean Architecture principles. It features a catalog experience inspired by [Crush Outdoor](https://crushoutdoor.com/) and complete ecommerce workflows (cart, checkout, payments, accounts, tracking) inspired by [Furlenco](https://www.furlenco.com/).

## 🏗 Architecture Overview

- **Backend**: ASP.NET Core 8 Web API (.NET 8 LTS)
- **Architecture**: Modular Monolith following Clean Architecture principles (Domain, Application, Infrastructure, Api)
- **Database**: PostgreSQL 16+ (Entity Framework Core 8)
- **Cache**: In-memory (`IMemoryCache`) with HTTP Output Caching (scalable to Redis)
- **Storage**: Cloudflare R2 / AWS S3 compatible object storage for product imagery
- **Auth**: JWT Authentication with Refresh Token rotation (ASP.NET Core Identity)
- **Payments**: Razorpay Integration (UPI, Credit/Debit Cards, NetBanking) with Webhook confirmation

## 📁 Repository Layout

```
GharCraft/
├── backend/
│   ├── src/
│   │   ├── GharCraft.Domain/          # Enterprise Core Entities, Enums, Value Objects & Interfaces
│   │   ├── GharCraft.Application/     # Application Use Cases, DTOs, Services & Validators
│   │   ├── GharCraft.Infrastructure/  # EF Core DbContext, Storage, Payments & External Services
│   │   └── GharCraft.Api/             # ASP.NET Core Controllers, Middleware & Filters
│   └── tests/
│       ├── GharCraft.UnitTests/       # Domain & Service Unit Tests
│       └── GharCraft.IntegrationTests/# API & DB Integration Tests
├── frontend/                          # Single Page Application (React + Vite + TypeScript)
├── docker/                            # Local Docker Compose setup (PostgreSQL, Redis, Storage)
└── implementation_plan.md            # Software Architecture Document (v2.0)
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK (or later)
- PostgreSQL 16+
- Node.js 18+ (for frontend)

### Running Backend locally

```bash
cd backend
dotnet restore
dotnet run --project src/GharCraft.Api
```

API Documentation will be accessible at `http://localhost:5000/swagger`.

## 📜 Documentation

Detailed architectural specification, ER diagrams, caching policies, and phased development roadmap are documented in [`implementation_plan.md`](./implementation_plan.md).
