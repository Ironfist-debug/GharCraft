# GharCraft — Software Architecture Document (SAD)

**Version:** 1.0  
**Date:** August 3, 2026  
**Author:** Principal Software Architect  
**Status:** Draft — Pending Stakeholder Approval  
**Project:** GharCraft Furniture Ecommerce Platform  

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)  
2. [Business Context & Requirements Analysis](#2-business-context--requirements-analysis)  
3. [Technology Stack Evaluation & Recommendation](#3-technology-stack-evaluation--recommendation)  
4. [Architecture Overview](#4-architecture-overview)  
5. [Backend Architecture — Clean Architecture](#5-backend-architecture--clean-architecture)  
6. [Project Structure](#6-project-structure)  
7. [Database Architecture](#7-database-architecture)  
8. [Database Schema Design](#8-database-schema-design)  
9. [Authentication & Authorization](#9-authentication--authorization)  
10. [Payment Architecture](#10-payment-architecture)  
11. [API Organization](#11-api-organization)  
12. [Caching Strategy](#12-caching-strategy)  
13. [Cloud & Deployment Architecture](#13-cloud--deployment-architecture)  
14. [Security Plan](#14-security-plan)  
15. [Scalability Roadmap](#15-scalability-roadmap)  
16. [DevOps & CI/CD](#16-devops--cicd)  
17. [Service Boundaries](#17-service-boundaries)  
18. [Development Roadmap & MVP Plan](#18-development-roadmap--mvp-plan)  
19. [Risks & Mitigation](#19-risks--mitigation)  
20. [Appendix — Architecture Diagrams (Text)](#20-appendix--architecture-diagrams-text)  

---

## 1. Executive Summary

GharCraft is a furniture ecommerce platform inspired by [Crush Outdoor](https://crushoutdoor.com/) for catalog experience and [Furlenco](https://www.furlenco.com/) for ecommerce workflows (cart, checkout, payments, orders, accounts). It is a single-branch, direct-to-consumer operation where an admin manages the entire product catalog, pricing, inventory, and content.

### Key Design Principles

| Principle | Rationale |
|-----------|-----------|
| **Read-optimized** | 95%+ traffic is browse/search; order conversion is < 5% |
| **Modular monolith** | Single developer; must be deployable, debuggable, and maintainable by one person |
| **Clean Architecture** | Testable, framework-agnostic domain; easy to swap infrastructure |
| **Cloud-native** | Containerized, auto-scalable, CDN-fronted |
| **Security-first** | PCI-compliant payment flow; OWASP Top 10 coverage |
| **Progressive complexity** | Start simple (monolith), grow into bounded contexts / microservices only when needed |

### Traffic Profile & Optimization Targets

```
┌─────────────────────────────────────────────────┐
│  Homepage & Catalog Browsing    ██████████  95%  │
│  Search & Filter                ████████    80%  │
│  Product Detail Page            ███████     70%  │
│  Add to Cart                    ██          10%  │
│  Checkout                       █            3%  │
│  Order Completion               ▍            1%  │
└─────────────────────────────────────────────────┘
```

**Optimize for:** Response caching, CDN delivery, database read replicas, Redis, full-text search, image CDN, precomputed aggregates.

---

## 2. Business Context & Requirements Analysis

### 2.1 Reference Site Analysis

**Crush Outdoor (crushoutdoor.com) — Catalog Inspiration:**
- WordPress + WooCommerce stack (Avada theme)
- Product categories: Outdoor Furniture, Dining, Lounge, Deep Seating, Umbrellas, Awnings
- Hierarchical categories with sub-categories
- Product pages with image galleries, specifications tables, fabric/finish selectors
- Blog/content marketing integration
- Store locator (not needed for GharCraft — single branch, online only)
- No cart/checkout/payment (inquiry-based model)

**Furlenco (furlenco.com) — Ecommerce Workflow Inspiration:**
- Next.js frontend with MUI components
- Full ecommerce: search, cart, checkout, payments, order tracking
- City-based delivery (GharCraft: India-wide or configurable)
- Wishlist, product comparison
- Customer accounts with order history
- Chat support (future for GharCraft)

### 2.2 Feature Matrix

| Domain | Customer Features | Admin Features |
|--------|------------------|----------------|
| **Catalog** | Browse, categories, collections, search, filters, product variants, gallery, related products, recently viewed | Product CRUD, category CRUD, collection CRUD, inventory management, media library |
| **Shopping** | Wishlist, compare, cart, checkout, coupons | — |
| **Orders** | Order placement, tracking, history | Order management, status updates |
| **Payments** | Razorpay, Stripe (pluggable) | Payment reconciliation, refund management |
| **Accounts** | Registration, login, profile, addresses, notifications | Customer management, roles & permissions |
| **Content** | Blogs, CMS pages, banners | CMS editor, blog editor, banner management, SEO management |
| **Analytics** | — | Dashboard, reports, analytics |

### 2.3 Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Page load (homepage) | < 1.5s (cached) |
| API response (catalog) | < 200ms (cached), < 500ms (uncached) |
| API response (checkout) | < 1s |
| Uptime | 99.9% |
| Concurrent users | 1,000 (MVP) → 100,000/day (scale) |
| Image delivery | CDN, WebP, responsive sizes |
| SEO | SSR/SSG-ready API responses, structured data |
| Security | OWASP Top 10, PCI DSS Level 4 (tokenized payments) |

---

## 3. Technology Stack Evaluation & Recommendation

### 3.1 Backend Framework Comparison

| Criteria | ASP.NET Core (.NET 9) | Java Spring Boot | Node.js (NestJS) |
|----------|----------------------|------------------|-------------------|
| **Performance** | ⭐⭐⭐⭐⭐ Exceptional. Kestrel is among the fastest web servers. Benchmark leader on TechEmpower. | ⭐⭐⭐⭐ Very good. JIT + GraalVM excellent, but higher baseline latency than .NET. | ⭐⭐⭐ Good for I/O-bound. Single-threaded event loop caps CPU-bound tasks. |
| **Scalability** | ⭐⭐⭐⭐⭐ Async/await first-class. Excellent horizontal + vertical scaling. | ⭐⭐⭐⭐⭐ Mature scaling with Spring Cloud, reactive stack. | ⭐⭐⭐⭐ Scales horizontally well. CPU-bound bottleneck requires worker threads. |
| **Maintainability** | ⭐⭐⭐⭐⭐ Strong typing (C#), compile-time safety, excellent refactoring tooling. | ⭐⭐⭐⭐ Strong typing (Java), but verbose. Annotation-heavy. | ⭐⭐⭐ TypeScript helps, but runtime type issues persist. Less mature tooling. |
| **Learning curve** | ⭐⭐⭐⭐ Moderate. C# is elegant, .NET CLI is productive. | ⭐⭐⭐ Steep. XML configs, annotation complexity, Maven/Gradle build system. | ⭐⭐⭐⭐ Low barrier (JavaScript familiarity), but NestJS patterns add learning. |
| **Development speed** | ⭐⭐⭐⭐⭐ Scaffolding, EF Core migrations, hot reload, minimal APIs. Fast iteration. | ⭐⭐⭐ Slower. Boilerplate-heavy, longer compile times. | ⭐⭐⭐⭐ Fast prototyping, but complex business logic is harder to maintain. |
| **Cloud deployment** | ⭐⭐⭐⭐⭐ First-class on Azure, excellent on AWS/GCP. Native Docker, Azure App Service. | ⭐⭐⭐⭐⭐ Excellent everywhere. Spring Cloud Native, Kubernetes support. | ⭐⭐⭐⭐ Good everywhere. Lightweight containers. |
| **Memory usage** | ⭐⭐⭐⭐ ~50-80MB base. AOT compilation available for even smaller footprint. | ⭐⭐⭐ ~150-300MB base. JVM overhead. GraalVM native-image improves this. | ⭐⭐⭐⭐⭐ ~30-50MB base. Smallest footprint. |
| **Ecosystem** | ⭐⭐⭐⭐ NuGet: 400K+ packages. EF Core, Identity, Aspire, MediatR, FluentValidation. | ⭐⭐⭐⭐⭐ Maven Central: largest ecosystem. Every integration imaginable. | ⭐⭐⭐⭐⭐ npm: largest registry. But quality varies wildly. |
| **Security** | ⭐⭐⭐⭐⭐ Built-in Identity, AntiForgery, CORS, data protection APIs. Security-hardened by default. | ⭐⭐⭐⭐⭐ Spring Security is the gold standard. Comprehensive but complex. | ⭐⭐⭐ Passport.js works but requires careful configuration. More manual security. |
| **Single developer suitability** | ⭐⭐⭐⭐⭐ One person can own the entire stack. Excellent CLI, scaffolding, and IDE support. | ⭐⭐⭐ Enterprise-grade but overhead is high for one person. | ⭐⭐⭐⭐ Fast for small features, but maintaining large NestJS apps solo is harder. |
| **Long-term maintenance** | ⭐⭐⭐⭐⭐ Predictable annual release cadence. LTS support. Microsoft-backed. | ⭐⭐⭐⭐⭐ Spring is battle-tested over 20+ years. VMware/Broadcom-backed. | ⭐⭐⭐ npm ecosystem churn. Frequent breaking changes in dependencies. |

### 3.2 Verdict & Recommendation

> [!IMPORTANT]
> **Recommended Technology: ASP.NET Core (.NET 9) with C#**

**Justification:**

1. **Performance leadership** — Kestrel consistently tops TechEmpower benchmarks. For a read-heavy catalog site, raw throughput matters. .NET 9 with minimal APIs and output caching delivers sub-millisecond cached responses.

2. **Single developer productivity** — C# is the most productive strongly-typed language for one person:
   - Entity Framework Core handles migrations, queries, and relationships with minimal boilerplate
   - Built-in Identity scaffolds authentication in minutes
   - MediatR + FluentValidation provide clean CQRS patterns
   - `dotnet new`, `dotnet ef`, and hot reload accelerate iteration
   - Rider / Visual Studio provide world-class refactoring

3. **Clean Architecture fit** — .NET's dependency injection is first-class (built into the framework). The ecosystem has mature patterns for Clean Architecture: MediatR for CQRS, FluentValidation for input validation, Mapster/AutoMapper for DTOs.

4. **Cloud economics** — .NET 9 containers are small (~80MB), start fast, and Azure offers generous free tiers for App Service, SQL, and Redis. Can also deploy on AWS/GCP with zero lock-in.

5. **Security hardening** — ASP.NET Core is secure by default: anti-forgery tokens, CORS policies, data protection APIs, and ASP.NET Core Identity handle 90% of security concerns out of the box.

6. **Long-term stability** — Microsoft's .NET release cadence is predictable (annual releases, LTS every 2 years). The ecosystem doesn't suffer from npm-style churn.

### 3.3 Final Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 9 (LTS track) |
| **Framework** | ASP.NET Core | 9 |
| **Language** | C# | 13 |
| **ORM** | Entity Framework Core | 9 |
| **Database (primary)** | PostgreSQL | 16+ |
| **Cache** | Redis (via StackExchange.Redis) | 7+ |
| **Search** | PostgreSQL Full-Text Search → OpenSearch (scale) | — |
| **Object Storage** | Azure Blob Storage / AWS S3 | — |
| **CDN** | Azure CDN / Cloudflare | — |
| **Image Processing** | ImageSharp / CDN transforms | — |
| **Authentication** | JWT + ASP.NET Core Identity | — |
| **Validation** | FluentValidation | — |
| **CQRS** | MediatR | 12+ |
| **Logging** | Serilog → Seq/ELK | — |
| **Observability** | OpenTelemetry | — |
| **Background Jobs** | Hangfire / .NET BackgroundService | — |
| **Containerization** | Docker + Docker Compose | — |
| **CI/CD** | GitHub Actions | — |
| **IaC** | Terraform (optional) | — |

---

## 4. Architecture Overview

### 4.1 High-Level Architecture (C4 Level 1 — System Context)

```
                    ┌──────────────┐
                    │   Customer   │
                    │   (Browser)  │
                    └──────┬───────┘
                           │ HTTPS
                    ┌──────▼───────┐
                    │     CDN      │
                    │ (Cloudflare) │
                    └──────┬───────┘
                           │
                    ┌──────▼───────┐
                    │Load Balancer │
                    │   (Nginx /   │
                    │  Cloud LB)   │
                    └──────┬───────┘
                           │
              ┌────────────▼────────────┐
              │    GharCraft API        │
              │  (ASP.NET Core 9)       │
              │                         │
              │  ┌──────────────────┐   │
              │  │ Customer API     │   │
              │  │ Admin API        │   │
              │  │ Webhook API      │   │
              │  └──────────────────┘   │
              └───┬─────┬─────┬────────┘
                  │     │     │
        ┌─────────▼┐ ┌─▼────┐ ┌▼──────────┐
        │PostgreSQL│ │Redis │ │Blob Storage│
        │  (Read   │ │Cache │ │  (Images)  │
        │ Replica) │ │      │ │            │
        └──────────┘ └──────┘ └────────────┘
              │
        ┌─────▼──────┐
        │ Background  │
        │  Workers    │
        │ (Hangfire)  │
        └─────────────┘
```

### 4.2 Architecture Style: Modular Monolith

```
┌──────────────────────────────────────────────────────────────┐
│                      GharCraft API                           │
│                                                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐    │
│  │ Catalog  │ │ Shopping │ │ Identity │ │   Content    │    │
│  │ Module   │ │ Module   │ │ Module   │ │   Module     │    │
│  │          │ │          │ │          │ │              │    │
│  │•Products │ │•Cart     │ │•Auth     │ │•CMS Pages   │    │
│  │•Category │ │•Checkout │ │•Users    │ │•Blogs       │    │
│  │•Search   │ │•Orders   │ │•Roles    │ │•Banners     │    │
│  │•Reviews  │ │•Payments │ │•JWT      │ │•SEO         │    │
│  │•Inventory│ │•Coupons  │ │          │ │•Media       │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘    │
│                                                              │
│  ┌──────────────────┐  ┌────────────────────────────────┐    │
│  │  Notifications   │  │         Analytics              │    │
│  │  Module          │  │         Module                  │    │
│  │                  │  │                                 │    │
│  │  •Email          │  │  •Dashboard                    │    │
│  │  •Push (future)  │  │  •Reports                     │    │
│  │  •SMS (future)   │  │  •Audit Logs                  │    │
│  └──────────────────┘  └────────────────────────────────┘    │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │              Shared Kernel                            │    │
│  │  •Base entities •Value objects •Domain events         │    │
│  │  •Result types  •Pagination   •Specifications        │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

> [!NOTE]
> The modular monolith approach means each module has its own domain, application services, and infrastructure — but they share a single database and deployment unit. This gives you microservices-level separation of concerns without the operational complexity. When traffic demands it, any module can be extracted into a standalone service.

---

## 5. Backend Architecture — Clean Architecture

### 5.1 Layer Dependency Flow

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │         Presentation Layer              │    │
│  │     (Controllers, Middleware, DTOs)      │    │
│  └───────────────┬─────────────────────────┘    │
│                  │ depends on                    │
│  ┌───────────────▼─────────────────────────┐    │
│  │         Application Layer               │    │
│  │  (Use Cases, CQRS Handlers, Services)   │    │
│  │  (FluentValidation, MediatR, Mapster)   │    │
│  └───────────────┬─────────────────────────┘    │
│                  │ depends on                    │
│  ┌───────────────▼─────────────────────────┐    │
│  │          Domain Layer                   │    │
│  │   (Entities, Value Objects, Enums,      │    │
│  │    Domain Events, Interfaces)           │    │
│  │         ** ZERO DEPENDENCIES **         │    │
│  └───────────────▲─────────────────────────┘    │
│                  │ implements                    │
│  ┌───────────────┴─────────────────────────┐    │
│  │       Infrastructure Layer              │    │
│  │  (EF Core, Redis, Blob, Email, APIs)    │    │
│  │  (Repository implementations)           │    │
│  └─────────────────────────────────────────┘    │
│                                                 │
└─────────────────────────────────────────────────┘

Dependency Rule: Inner layers NEVER depend on outer layers.
Domain is the innermost layer with ZERO external dependencies.
```

### 5.2 Layer Responsibilities

#### Domain Layer
- **Entities**: `Product`, `Category`, `Order`, `User`, `Cart`, `Payment`, etc.
- **Value Objects**: `Money`, `Address`, `Slug`, `SKU`, `EmailAddress`, `PhoneNumber`
- **Enums**: `OrderStatus`, `PaymentStatus`, `ProductStatus`, `UserRole`
- **Domain Events**: `OrderPlacedEvent`, `PaymentCompletedEvent`, `ProductCreatedEvent`
- **Interfaces**: `IProductRepository`, `IOrderRepository`, `IPaymentGateway`
- **Specifications**: `ActiveProductsSpec`, `OrdersByCustomerSpec`
- **Domain Services**: `PricingService`, `InventoryService`, `CouponValidationService`
- **No dependencies** on any framework, ORM, or external library.

#### Application Layer
- **CQRS with MediatR**:
  - **Commands**: `CreateProductCommand`, `PlaceOrderCommand`, `ApplyCouponCommand`
  - **Queries**: `GetProductBySlugQuery`, `SearchProductsQuery`, `GetOrderHistoryQuery`
  - **Handlers**: One handler per command/query, orchestrates domain logic
- **Validators**: FluentValidation rules for every command
- **Pipeline Behaviors**: Logging, validation, caching, transaction management
- **DTOs / ViewModels**: Response models that never expose domain entities
- **Interfaces**: `ICurrentUserService`, `IEmailService`, `IFileStorageService`
- **Mapping profiles**: Entity ↔ DTO mapping configurations

#### Infrastructure Layer
- **Persistence**: EF Core DbContext, migrations, entity configurations (Fluent API)
- **Repositories**: Concrete implementations of domain repository interfaces
- **External Services**: Payment gateways, email providers, file storage, search
- **Caching**: Redis implementation of `ICacheService`
- **Identity**: JWT token generation, refresh token management, password hashing

#### Presentation Layer (API)
- **Controllers**: Thin controllers that dispatch MediatR commands/queries
- **Middleware**: Exception handling, request logging, correlation IDs, rate limiting
- **Filters**: Authentication, authorization, model validation
- **API versioning**: URL-based versioning (`/api/v1/...`)
- **Swagger/OpenAPI**: Auto-generated API documentation

### 5.3 CQRS Pattern with MediatR

```
┌───────────┐     ┌──────────┐     ┌────────────────┐     ┌──────────┐
│Controller │────▶│ MediatR  │────▶│ Pipeline       │────▶│ Handler  │
│           │     │ Send()   │     │ Behaviors      │     │          │
└───────────┘     └──────────┘     │                │     └────┬─────┘
                                   │ 1. Logging     │          │
                                   │ 2. Validation  │          │
                                   │ 3. Caching     │          ▼
                                   │ 4. Transaction │     ┌──────────┐
                                   └────────────────┘     │ Domain / │
                                                          │ Repo     │
                                                          └──────────┘
```

**Why CQRS here?**

- Catalog reads (95% of traffic) can be heavily cached, use read-optimized DTOs, and bypass complex domain logic
- Cart/order writes (5% of traffic) go through full domain validation
- Commands and queries have separate models — reads never load full aggregate graphs
- Each handler is a small, focused, testable unit — perfect for a single developer

### 5.4 Cross-Cutting Concerns

| Concern | Implementation |
|---------|---------------|
| **Dependency Injection** | .NET built-in DI container; register services per layer via extension methods |
| **Validation** | FluentValidation + MediatR `ValidationBehavior` pipeline; returns structured errors |
| **Logging** | Serilog with structured logging; correlation IDs via middleware; sinks: Console + Seq/ELK |
| **Exception Handling** | Global exception middleware; maps domain exceptions → HTTP status codes; ProblemDetails RFC 7807 |
| **Caching** | MediatR `CachingBehavior` for queries; Redis + IMemoryCache hybrid |
| **Rate Limiting** | ASP.NET Core built-in rate limiter; fixed window for APIs, sliding window for auth endpoints |
| **Transactions** | MediatR `TransactionBehavior`; wraps commands in `IDbContextTransaction` |
| **Audit Logging** | EF Core interceptors; auto-record `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` |

---

## 6. Project Structure

```
GharCraft/
│
├── src/
│   ├── GharCraft.Domain/                          # Domain Layer (innermost)
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── AuditableEntity.cs
│   │   │   ├── ISoftDeletable.cs
│   │   │   ├── DomainEvent.cs
│   │   │   └── Result.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── Address.cs
│   │   │   ├── Slug.cs
│   │   │   ├── SKU.cs
│   │   │   ├── EmailAddress.cs
│   │   │   └── PhoneNumber.cs
│   │   ├── Enums/
│   │   │   ├── OrderStatus.cs
│   │   │   ├── PaymentStatus.cs
│   │   │   ├── ProductStatus.cs
│   │   │   └── CouponType.cs
│   │   ├── Entities/
│   │   │   ├── Catalog/
│   │   │   │   ├── Product.cs
│   │   │   │   ├── ProductVariant.cs
│   │   │   │   ├── ProductImage.cs
│   │   │   │   ├── Category.cs
│   │   │   │   ├── Collection.cs
│   │   │   │   ├── ProductAttribute.cs
│   │   │   │   ├── ProductReview.cs
│   │   │   │   └── InventoryRecord.cs
│   │   │   ├── Shopping/
│   │   │   │   ├── Cart.cs
│   │   │   │   ├── CartItem.cs
│   │   │   │   ├── Wishlist.cs
│   │   │   │   ├── WishlistItem.cs
│   │   │   │   ├── Order.cs
│   │   │   │   ├── OrderItem.cs
│   │   │   │   ├── Payment.cs
│   │   │   │   └── Coupon.cs
│   │   │   ├── Identity/
│   │   │   │   ├── User.cs
│   │   │   │   ├── Role.cs
│   │   │   │   ├── Permission.cs
│   │   │   │   ├── RefreshToken.cs
│   │   │   │   └── UserAddress.cs
│   │   │   ├── Content/
│   │   │   │   ├── CmsPage.cs
│   │   │   │   ├── BlogPost.cs
│   │   │   │   ├── Banner.cs
│   │   │   │   ├── MediaFile.cs
│   │   │   │   └── SeoMetadata.cs
│   │   │   └── System/
│   │   │       ├── AuditLog.cs
│   │   │       ├── Notification.cs
│   │   │       └── ContactSubmission.cs
│   │   ├── Events/
│   │   │   ├── OrderPlacedEvent.cs
│   │   │   ├── PaymentCompletedEvent.cs
│   │   │   ├── ProductCreatedEvent.cs
│   │   │   └── InventoryUpdatedEvent.cs
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IProductRepository.cs
│   │   │   │   ├── ICategoryRepository.cs
│   │   │   │   ├── IOrderRepository.cs
│   │   │   │   ├── ICartRepository.cs
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   └── IGenericRepository.cs
│   │   │   └── Services/
│   │   │       ├── IPaymentGateway.cs
│   │   │       ├── IPricingService.cs
│   │   │       └── IInventoryService.cs
│   │   └── Specifications/
│   │       ├── ActiveProductsSpec.cs
│   │       ├── ProductsByCategorySpec.cs
│   │       └── OrdersByCustomerSpec.cs
│   │
│   ├── GharCraft.Application/                     # Application Layer
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   ├── IFileStorageService.cs
│   │   │   │   ├── ISearchService.cs
│   │   │   │   └── ITokenService.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── CachingBehavior.cs
│   │   │   │   ├── TransactionBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Models/
│   │   │   │   ├── PaginatedResult.cs
│   │   │   │   ├── ApiResponse.cs
│   │   │   │   └── FileUploadResult.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   ├── ValidationException.cs
│   │   │   │   ├── ForbiddenException.cs
│   │   │   │   └── ConflictException.cs
│   │   │   └── Mappings/
│   │   │       └── MappingConfig.cs
│   │   ├── Features/
│   │   │   ├── Catalog/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   │   │   ├── CreateProductHandler.cs
│   │   │   │   │   │   └── CreateProductValidator.cs
│   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   ├── DeleteProduct/
│   │   │   │   │   ├── CreateCategory/
│   │   │   │   │   ├── UpdateInventory/
│   │   │   │   │   └── SubmitReview/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetProductBySlug/
│   │   │   │       │   ├── GetProductBySlugQuery.cs
│   │   │   │       │   ├── GetProductBySlugHandler.cs
│   │   │   │       │   └── ProductDetailDto.cs
│   │   │   │       ├── SearchProducts/
│   │   │   │       ├── GetCategories/
│   │   │   │       ├── GetCollections/
│   │   │   │       ├── GetProductReviews/
│   │   │   │       └── GetRelatedProducts/
│   │   │   ├── Shopping/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AddToCart/
│   │   │   │   │   ├── UpdateCartItem/
│   │   │   │   │   ├── RemoveFromCart/
│   │   │   │   │   ├── PlaceOrder/
│   │   │   │   │   ├── ApplyCoupon/
│   │   │   │   │   ├── AddToWishlist/
│   │   │   │   │   └── InitiatePayment/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetCart/
│   │   │   │       ├── GetWishlist/
│   │   │   │       ├── GetOrderHistory/
│   │   │   │       ├── GetOrderDetail/
│   │   │   │       └── TrackOrder/
│   │   │   ├── Identity/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RegisterUser/
│   │   │   │   │   ├── LoginUser/
│   │   │   │   │   ├── RefreshToken/
│   │   │   │   │   ├── ResetPassword/
│   │   │   │   │   ├── VerifyEmail/
│   │   │   │   │   └── UpdateProfile/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetUserProfile/
│   │   │   │       └── GetUserAddresses/
│   │   │   ├── Content/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateBlogPost/
│   │   │   │   │   ├── UpdateCmsPage/
│   │   │   │   │   ├── UploadMedia/
│   │   │   │   │   └── ManageBanner/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetBlogPosts/
│   │   │   │       ├── GetCmsPage/
│   │   │   │       └── GetBanners/
│   │   │   ├── Admin/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── UpdateOrderStatus/
│   │   │   │   │   ├── ManageCoupon/
│   │   │   │   │   └── ManageRoles/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetDashboard/
│   │   │   │       ├── GetSalesReport/
│   │   │   │       ├── GetCustomerList/
│   │   │   │       └── GetAuditLogs/
│   │   │   └── Notifications/
│   │   │       ├── Commands/
│   │   │       │   └── SendNotification/
│   │   │       └── EventHandlers/
│   │   │           ├── OrderPlacedHandler.cs
│   │   │           └── PaymentCompletedHandler.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── GharCraft.Infrastructure/                  # Infrastructure Layer
│   │   ├── Persistence/
│   │   │   ├── GharCraftDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── ProductConfiguration.cs
│   │   │   │   ├── CategoryConfiguration.cs
│   │   │   │   ├── OrderConfiguration.cs
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   └── ... (one per entity)
│   │   │   ├── Repositories/
│   │   │   │   ├── GenericRepository.cs
│   │   │   │   ├── ProductRepository.cs
│   │   │   │   ├── CategoryRepository.cs
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── CartRepository.cs
│   │   │   │   └── UserRepository.cs
│   │   │   ├── Migrations/
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditableEntityInterceptor.cs
│   │   │   │   └── SoftDeleteInterceptor.cs
│   │   │   └── Seed/
│   │   │       ├── DataSeeder.cs
│   │   │       └── SeedData/
│   │   ├── Services/
│   │   │   ├── CacheService.cs                    # Redis implementation
│   │   │   ├── TokenService.cs                    # JWT generation
│   │   │   ├── CurrentUserService.cs              # Extract user from HttpContext
│   │   │   ├── EmailService.cs                    # SMTP / SendGrid
│   │   │   ├── FileStorageService.cs              # Azure Blob / S3
│   │   │   └── SearchService.cs                   # PG Full-Text / OpenSearch
│   │   ├── Payments/
│   │   │   ├── IPaymentGatewayFactory.cs
│   │   │   ├── PaymentGatewayFactory.cs
│   │   │   ├── RazorpayGateway.cs
│   │   │   ├── StripeGateway.cs
│   │   │   └── PaymentWebhookProcessor.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── OrderCleanupJob.cs
│   │   │   ├── InventorySyncJob.cs
│   │   │   └── CacheWarmupJob.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── GharCraft.Api/                             # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── V1/
│   │   │   │   ├── ProductsController.cs
│   │   │   │   ├── CategoriesController.cs
│   │   │   │   ├── CollectionsController.cs
│   │   │   │   ├── CartController.cs
│   │   │   │   ├── WishlistController.cs
│   │   │   │   ├── OrdersController.cs
│   │   │   │   ├── PaymentsController.cs
│   │   │   │   ├── AuthController.cs
│   │   │   │   ├── AccountController.cs
│   │   │   │   ├── SearchController.cs
│   │   │   │   ├── ReviewsController.cs
│   │   │   │   ├── ContentController.cs
│   │   │   │   └── ContactController.cs
│   │   │   └── Admin/
│   │   │       ├── AdminProductsController.cs
│   │   │       ├── AdminCategoriesController.cs
│   │   │       ├── AdminOrdersController.cs
│   │   │       ├── AdminCustomersController.cs
│   │   │       ├── AdminCouponsController.cs
│   │   │       ├── AdminContentController.cs
│   │   │       ├── AdminMediaController.cs
│   │   │       ├── AdminBannersController.cs
│   │   │       ├── AdminReportsController.cs
│   │   │       ├── AdminRolesController.cs
│   │   │       ├── AdminDashboardController.cs
│   │   │       ├── AdminInventoryController.cs
│   │   │       └── AdminSeoController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── PerformanceMiddleware.cs
│   │   ├── Filters/
│   │   │   └── ApiKeyAuthFilter.cs
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   ├── WebApplicationExtensions.cs
│   │   │   └── ClaimsPrincipalExtensions.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   └── Dockerfile
│   │
│   └── GharCraft.Shared/                          # Shared utilities
│       ├── Constants/
│       │   ├── CacheKeys.cs
│       │   ├── Roles.cs
│       │   └── Permissions.cs
│       ├── Extensions/
│       │   ├── StringExtensions.cs
│       │   ├── DateTimeExtensions.cs
│       │   └── QueryableExtensions.cs
│       └── Helpers/
│           ├── SlugGenerator.cs
│           ├── PaginationHelper.cs
│           └── ImageHelper.cs
│
├── tests/
│   ├── GharCraft.Domain.Tests/
│   │   ├── Entities/
│   │   └── ValueObjects/
│   ├── GharCraft.Application.Tests/
│   │   ├── Features/
│   │   │   ├── Catalog/
│   │   │   ├── Shopping/
│   │   │   └── Identity/
│   │   └── Common/
│   ├── GharCraft.Infrastructure.Tests/
│   │   ├── Persistence/
│   │   └── Services/
│   └── GharCraft.Api.Tests/
│       ├── Controllers/
│       └── Integration/
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── docker-compose.prod.yml
│   ├── nginx/
│   │   └── nginx.conf
│   └── postgres/
│       └── init.sql
│
├── infra/                                          # Terraform (optional)
│   ├── main.tf
│   ├── variables.tf
│   └── modules/
│
├── docs/
│   ├── architecture.md
│   ├── api-conventions.md
│   └── deployment.md
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── cd-staging.yml
│       └── cd-production.yml
│
├── GharCraft.sln
├── .editorconfig
├── .gitignore
├── Directory.Build.props
└── README.md
```

---

## 7. Database Architecture

### 7.1 Database Selection

> [!IMPORTANT]
> **Recommended: PostgreSQL 16+**

| Criteria | PostgreSQL | SQL Server | MySQL |
|----------|-----------|------------|-------|
| **Cost** | ⭐⭐⭐⭐⭐ Free, open source | ⭐⭐ Expensive licensing (or Express with limits) | ⭐⭐⭐⭐⭐ Free, open source |
| **JSON support** | ⭐⭐⭐⭐⭐ `jsonb` is best-in-class; indexable, queryable | ⭐⭐⭐ JSON support exists but less mature | ⭐⭐⭐ JSON support is functional but limited indexing |
| **Full-text search** | ⭐⭐⭐⭐ `tsvector/tsquery` — excellent for MVP; eliminates need for Elasticsearch early on | ⭐⭐⭐ Full-text exists but less flexible | ⭐⭐⭐ Full-text exists, works for basic needs |
| **EF Core support** | ⭐⭐⭐⭐⭐ Npgsql provider is mature and feature-rich | ⭐⭐⭐⭐⭐ First-class Microsoft support | ⭐⭐⭐⭐ Pomelo provider is good |
| **Cloud availability** | ⭐⭐⭐⭐⭐ Azure DB for PostgreSQL, AWS RDS, GCP Cloud SQL | ⭐⭐⭐⭐ Azure SQL, AWS RDS | ⭐⭐⭐⭐⭐ Everywhere |
| **Advanced features** | ⭐⭐⭐⭐⭐ CTEs, window functions, materialized views, LISTEN/NOTIFY, partitioning | ⭐⭐⭐⭐ Good but proprietary extensions | ⭐⭐⭐ Fewer advanced features |
| **Performance (read-heavy)** | ⭐⭐⭐⭐⭐ Excellent with proper indexing, read replicas, connection pooling (PgBouncer) | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐⭐⭐ Good |
| **Furniture catalog fit** | ⭐⭐⭐⭐⭐ `jsonb` for product attributes/specs, full-text for search, array types for tags | ⭐⭐⭐⭐ Works but less flexible for dynamic attributes | ⭐⭐⭐ Less suitable for flexible schemas |

**Why PostgreSQL wins for GharCraft:**

1. **Free** — No licensing cost. Single developer budget = $0 for database.
2. **`jsonb` for product attributes** — Furniture has highly variable attributes (dimensions, materials, colors, weight capacity). Storing these in `jsonb` avoids EAV anti-patterns while keeping them queryable and indexable.
3. **Built-in full-text search** — Eliminates the need for Elasticsearch/OpenSearch at MVP scale. PostgreSQL `tsvector` handles product search for up to ~500K products with sub-100ms response times.
4. **Materialized views** — Pre-compute category product counts, bestseller lists, and aggregate ratings for instant homepage loads.
5. **Read replicas** — Native streaming replication for scaling reads as traffic grows.

### 7.2 Supplementary Data Stores

| Store | Use Case | When to Introduce |
|-------|----------|-------------------|
| **Redis 7+** | Session cache, response cache, cart cache (guest users), rate limiting counters, distributed locks | **Day 1** — Essential for read performance |
| **PostgreSQL Full-Text Search** | Product search, blog search, CMS search | **Day 1** — Built into PostgreSQL, no extra infra |
| **OpenSearch / Elasticsearch** | Advanced search with facets, autocomplete, typo tolerance, synonyms | **Phase 3** (>50K products or when PG FTS isn't enough) |
| **Azure Blob Storage / AWS S3** | Product images, media library, user uploads, CMS assets | **Day 1** — Never store images in the database |
| **CDN (Cloudflare / Azure CDN)** | Static assets, product images, CSS/JS, cached API responses | **Day 1** — Critical for page load performance |

### 7.3 Image Strategy

```
┌─────────────┐     ┌──────────────┐     ┌───────────┐     ┌──────────┐
│ Admin Upload │────▶│ API (resize, │────▶│ Blob      │────▶│ CDN      │
│ (Original)  │     │ optimize,    │     │ Storage   │     │ (Edge    │
│             │     │ generate     │     │           │     │ cached)  │
│             │     │ variants)    │     │ /products │     │          │
│             │     │              │     │   /hero   │     │ Serve    │
│             │     │ • Thumbnail  │     │   /thumb  │     │ WebP/    │
│             │     │ • Medium     │     │   /medium │     │ AVIF     │
│             │     │ • Large      │     │   /large  │     │          │
│             │     │ • WebP       │     │   /orig   │     │          │
└─────────────┘     └──────────────┘     └───────────┘     └──────────┘
```

**Image variant strategy:**
- `thumb` — 150×150px, JPEG 80%, used in cart/lists
- `medium` — 600×600px, WebP 85%, used in product cards
- `large` — 1200×1200px, WebP 90%, used in product gallery
- `hero` — 1920×Auto, WebP 90%, used in banners
- `original` — Preserved for admin download

**CDN configuration:**
- Cache-Control: `public, max-age=31536000, immutable` for versioned images
- Image URLs contain content hash for cache busting: `/images/products/{hash}-{size}.webp`
- Cloudflare Polish or Azure CDN image optimization for automatic WebP/AVIF conversion

---

## 8. Database Schema Design

### 8.1 Entity Relationship Diagram (Text)

```
┌──────────────────────────────────────────────────────────────────┐
│                        IDENTITY                                  │
│                                                                  │
│  ┌────────────┐  1    M  ┌──────────────┐                       │
│  │   Users    │─────────▶│ UserAddresses│                       │
│  │            │          └──────────────┘                       │
│  │ •Id (GUID) │  M    M  ┌──────────┐                          │
│  │ •Email     │─────────▶│  Roles   │                          │
│  │ •PasswordH │          │          │  M    M  ┌────────────┐  │
│  │ •FirstName │          │ •Id      │─────────▶│Permissions │  │
│  │ •LastName  │          │ •Name    │          │            │  │
│  │ •Phone     │          └──────────┘          │ •Id        │  │
│  │ •IsActive  │  1    M  ┌──────────────┐      │ •Key       │  │
│  │ •AvatarUrl │─────────▶│RefreshTokens │      │ •Module    │  │
│  └────────────┘          └──────────────┘      └────────────┘  │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         CATALOG                                  │
│                                                                  │
│  ┌────────────────┐  1  M  ┌─────────────────┐                  │
│  │   Categories   │───────▶│    Products     │                  │
│  │                │        │                 │                  │
│  │ •Id            │        │ •Id (GUID)      │                  │
│  │ •Name          │        │ •Name           │                  │
│  │ •Slug          │        │ •Slug (unique)  │                  │
│  │ •ParentId (FK) │        │ •Description    │                  │
│  │ •ImageUrl      │        │ •ShortDesc      │                  │
│  │ •SortOrder     │        │ •BasePrice      │                  │
│  │ •IsActive      │        │ •SalePrice      │                  │
│  │ •SeoMeta (json)│        │ •CategoryId(FK) │                  │
│  └────────────────┘        │ •Status (enum)  │                  │
│                            │ •IsFeatured     │                  │
│  ┌────────────────┐  M  M  │ •Tags (text[])  │                  │
│  │  Collections   │───────▶│ •Attributes(jsb)│                  │
│  │                │        │ •Specifications  │                  │
│  │ •Id            │        │   (jsonb)       │                  │
│  │ •Name          │        │ •SeoMeta (json) │                  │
│  │ •Slug          │        │ •AvgRating      │                  │
│  │ •Description   │        │ •ReviewCount    │                  │
│  │ •ImageUrl      │        │ •SearchVector   │                  │
│  │ •IsActive      │        │   (tsvector)    │                  │
│  │ •SortOrder     │        └───┬──┬──┬───────┘                  │
│  └────────────────┘            │  │  │                          │
│       1  M  ┌──────────────────┘  │  └─────────────────┐       │
│       ┌─────▼──────────┐  ┌──────▼───────┐  ┌──────────▼──┐   │
│       │ProductVariants │  │ProductImages │  │ProductReview│   │
│       │                │  │              │  │             │   │
│       │ •Id            │  │ •Id          │  │ •Id         │   │
│       │ •ProductId(FK) │  │ •ProductId   │  │ •ProductId  │   │
│       │ •SKU (unique)  │  │ •Url         │  │ •UserId     │   │
│       │ •Name          │  │ •AltText     │  │ •Rating 1-5 │   │
│       │ •Price         │  │ •SortOrder   │  │ •Title      │   │
│       │ •SalePrice     │  │ •IsPrimary   │  │ •Comment    │   │
│       │ •Attributes    │  └──────────────┘  │ •IsApproved │   │
│       │  (jsonb)       │                    └─────────────┘   │
│       │ •StockQuantity │                                       │
│       │ •IsActive      │  ┌────────────────┐                   │
│       └────────────────┘  │InventoryRecord │                   │
│                           │                │                   │
│                           │ •Id            │                   │
│                           │ •VariantId(FK) │                   │
│                           │ •QuantityChange│                   │
│                           │ •Reason        │                   │
│                           │ •ReferenceId   │                   │
│                           │ •Timestamp     │                   │
│                           └────────────────┘                   │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                        SHOPPING                                  │
│                                                                  │
│  ┌──────────────┐  1  M  ┌────────────┐                         │
│  │    Carts     │───────▶│ CartItems  │                         │
│  │              │        │            │                         │
│  │ •Id (GUID)   │        │ •Id        │                         │
│  │ •UserId (FK) │        │ •CartId    │                         │
│  │  (nullable)  │        │ •VariantId │                         │
│  │ •SessionId   │        │ •Quantity  │                         │
│  │ •CouponId    │        │ •UnitPrice │                         │
│  │ •ExpiresAt   │        └────────────┘                         │
│  └──────────────┘                                               │
│                                                                  │
│  ┌──────────────┐  1  M  ┌────────────┐                         │
│  │   Wishlists  │───────▶│WishlistItem│                         │
│  │              │        │            │                         │
│  │ •Id          │        │ •Id        │                         │
│  │ •UserId (FK) │        │ •WishlistId│                         │
│  └──────────────┘        │ •ProductId │                         │
│                          │ •AddedAt   │                         │
│                          └────────────┘                         │
│                                                                  │
│  ┌──────────────────┐  1  M  ┌──────────────┐                   │
│  │     Orders       │───────▶│ OrderItems   │                   │
│  │                  │        │              │                   │
│  │ •Id (GUID)       │        │ •Id          │                   │
│  │ •OrderNumber     │        │ •OrderId     │                   │
│  │  (sequential)    │        │ •ProductId   │                   │
│  │ •UserId (FK)     │        │ •VariantId   │                   │
│  │ •Status (enum)   │        │ •ProductName │                   │
│  │ •SubTotal        │        │  (snapshot)  │                   │
│  │ •DiscountAmount  │        │ •SKU         │                   │
│  │ •TaxAmount       │        │ •Quantity    │                   │
│  │ •ShippingAmount  │        │ •UnitPrice   │                   │
│  │ •TotalAmount     │        │ •TotalPrice  │                   │
│  │ •CouponCode      │        └──────────────┘                   │
│  │ •ShippingAddress  │                                          │
│  │  (jsonb)         │  1  M  ┌──────────────┐                   │
│  │ •BillingAddress   │───────▶│  Payments    │                   │
│  │  (jsonb)         │        │              │                   │
│  │ •Notes           │        │ •Id          │                   │
│  │ •PlacedAt        │        │ •OrderId(FK) │                   │
│  │ •CompletedAt     │        │ •Gateway     │                   │
│  │                  │        │ •GatewayTxnId│                   │
│  └──────────────────┘        │ •Amount      │                   │
│                              │ •Currency    │                   │
│                              │ •Status      │                   │
│                              │ •RawResponse │                   │
│                              │  (jsonb)     │                   │
│                              │ •PaidAt      │                   │
│                              └──────────────┘                   │
│                                                                  │
│  ┌──────────────────┐                                            │
│  │     Coupons      │                                            │
│  │                  │                                            │
│  │ •Id              │                                            │
│  │ •Code (unique)   │                                            │
│  │ •Type (enum)     │     (Percentage / FixedAmount / FreeShip)  │
│  │ •Value           │                                            │
│  │ •MinOrderAmount  │                                            │
│  │ •MaxDiscount     │                                            │
│  │ •UsageLimit      │                                            │
│  │ •UsedCount       │                                            │
│  │ •ValidFrom       │                                            │
│  │ •ValidTo         │                                            │
│  │ •IsActive        │                                            │
│  │ •ApplicableCats  │     (int[] — category IDs, nullable)       │
│  │ •ApplicableProds │     (int[] — product IDs, nullable)        │
│  └──────────────────┘                                            │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         CONTENT                                  │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  CmsPages    │  │  BlogPosts   │  │   Banners    │           │
│  │              │  │              │  │              │           │
│  │ •Id          │  │ •Id          │  │ •Id          │           │
│  │ •Title       │  │ •Title       │  │ •Title       │           │
│  │ •Slug        │  │ •Slug        │  │ •ImageUrl    │           │
│  │ •Content     │  │ •Content     │  │ •MobileImgUrl│           │
│  │  (HTML/MD)   │  │ •Excerpt     │  │ •LinkUrl     │           │
│  │ •IsPublished │  │ •CoverImage  │  │ •Position    │           │
│  │ •SeoMeta     │  │ •AuthorId    │  │  (enum)      │           │
│  │  (jsonb)     │  │ •Tags (text[])│  │ •SortOrder   │           │
│  │ •SortOrder   │  │ •IsPublished │  │ •IsActive    │           │
│  └──────────────┘  │ •PublishedAt │  │ •StartsAt    │           │
│                    │ •SeoMeta     │  │ •EndsAt      │           │
│  ┌──────────────┐  │  (jsonb)     │  └──────────────┘           │
│  │  MediaFiles  │  └──────────────┘                             │
│  │              │                                               │
│  │ •Id          │  ┌──────────────────┐                         │
│  │ •FileName    │  │  SeoMetadata     │   (stored as jsonb      │
│  │ •OriginalName│  │                  │    on parent entities)  │
│  │ •ContentType │  │  •MetaTitle      │                         │
│  │ •Size (bytes)│  │  •MetaDescription│                         │
│  │ •Url         │  │  •MetaKeywords   │                         │
│  │ •ThumbnailUrl│  │  •CanonicalUrl   │                         │
│  │ •AltText     │  │  •OgImage        │                         │
│  │ •Folder      │  │  •StructuredData │                         │
│  │ •UploadedBy  │  └──────────────────┘                         │
│  └──────────────┘                                               │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         SYSTEM                                   │
│                                                                  │
│  ┌─────────────────┐  ┌───────────────────┐  ┌───────────────┐  │
│  │   AuditLogs     │  │  Notifications    │  │ContactSubmis. │  │
│  │                 │  │                   │  │               │  │
│  │ •Id             │  │ •Id               │  │ •Id           │  │
│  │ •EntityName     │  │ •UserId (FK)      │  │ •Name         │  │
│  │ •EntityId       │  │ •Title            │  │ •Email        │  │
│  │ •Action (enum)  │  │ •Message          │  │ •Phone        │  │
│  │  (Create/Update │  │ •Type (enum)      │  │ •Subject      │  │
│  │   /Delete)      │  │  (Order/Promo/    │  │ •Message      │  │
│  │ •OldValues(json)│  │   System)         │  │ •IsRead       │  │
│  │ •NewValues(json)│  │ •IsRead           │  │ •RepliedAt    │  │
│  │ •UserId         │  │ •ReadAt           │  │ •CreatedAt    │  │
│  │ •Timestamp      │  │ •CreatedAt        │  └───────────────┘  │
│  │ •IpAddress      │  └───────────────────┘                     │
│  └─────────────────┘                                            │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **GUIDs as primary keys** | Safe for distributed systems, no sequential ID guessing, merge-friendly. Use `uuid_generate_v7()` for time-ordered UUIDs (B-tree friendly). |
| **`jsonb` for product attributes/specs** | Furniture attributes vary wildly (dimensions, materials, weight, fabric options). JSONB avoids EAV pattern while remaining indexable via GIN indexes. |
| **`tsvector` search column** | Auto-populated trigger on product name + description + tags. Enables instant full-text search without external service. |
| **Denormalized order snapshots** | `OrderItem` stores `ProductName`, `SKU`, `UnitPrice` at time of order — never references current product data. Product changes don't corrupt order history. |
| **Address as jsonb on Order** | Shipping/billing addresses are snapshotted at order time. User can change their saved addresses later without affecting past orders. |
| **Soft deletes** | Products, categories, users use `IsDeleted` + `DeletedAt`. Orders and payments are never deleted. Audit logs are append-only. |
| **`SortOrder` columns** | Admin-controlled display ordering for categories, banners, images, collections. Integer-based for simple drag-and-drop reordering. |
| **Separate `ProductVariant`** | A sofa may come in different fabrics, sizes, or colors. Each variant has its own SKU, price, and stock. The `Product` is the parent aggregate. |

### 8.3 Indexing Strategy

```sql
-- Performance-critical indexes for read-heavy workload

-- Product catalog (most queried table)
CREATE INDEX idx_products_category_status ON products (category_id, status) WHERE is_deleted = false;
CREATE INDEX idx_products_slug ON products (slug) WHERE is_deleted = false;
CREATE INDEX idx_products_featured ON products (is_featured, sort_order) WHERE status = 'Active' AND is_deleted = false;
CREATE INDEX idx_products_search ON products USING GIN (search_vector);
CREATE INDEX idx_products_attributes ON products USING GIN (attributes);
CREATE INDEX idx_products_price ON products (sale_price NULLS LAST, base_price);

-- Categories
CREATE INDEX idx_categories_slug ON categories (slug) WHERE is_deleted = false;
CREATE INDEX idx_categories_parent ON categories (parent_id, sort_order);

-- Product variants
CREATE INDEX idx_variants_product ON product_variants (product_id) WHERE is_active = true;
CREATE INDEX idx_variants_sku ON product_variants (sku);

-- Orders (write-light, read for history)
CREATE INDEX idx_orders_user ON orders (user_id, placed_at DESC);
CREATE INDEX idx_orders_status ON orders (status) WHERE status NOT IN ('Completed', 'Cancelled');
CREATE INDEX idx_orders_number ON orders (order_number);

-- Cart (session-based, frequently accessed)
CREATE INDEX idx_carts_user ON carts (user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_carts_session ON carts (session_id) WHERE user_id IS NULL;

-- Full-text search trigger
CREATE OR REPLACE FUNCTION products_search_vector_update() RETURNS trigger AS $$
BEGIN
  NEW.search_vector :=
    setweight(to_tsvector('english', coalesce(NEW.name, '')), 'A') ||
    setweight(to_tsvector('english', coalesce(NEW.short_description, '')), 'B') ||
    setweight(to_tsvector('english', coalesce(array_to_string(NEW.tags, ' '), '')), 'B') ||
    setweight(to_tsvector('english', coalesce(NEW.description, '')), 'C');
  RETURN NEW;
END
$$ LANGUAGE plpgsql;

CREATE TRIGGER products_search_update
  BEFORE INSERT OR UPDATE ON products
  FOR EACH ROW EXECUTE FUNCTION products_search_vector_update();
```

### 8.4 Materialized Views (Pre-computed Aggregates)

```sql
-- Homepage: category product counts
CREATE MATERIALIZED VIEW mv_category_product_counts AS
SELECT
    c.id,
    c.name,
    c.slug,
    c.image_url,
    COUNT(p.id) AS product_count
FROM categories c
LEFT JOIN products p ON p.category_id = c.id
    AND p.status = 'Active' AND p.is_deleted = false
WHERE c.is_deleted = false AND c.is_active = true
GROUP BY c.id;

-- Refresh every 15 minutes via background job
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_category_product_counts;

-- Bestsellers (top products by order count)
CREATE MATERIALIZED VIEW mv_bestsellers AS
SELECT
    p.id, p.name, p.slug, p.base_price, p.sale_price,
    (SELECT url FROM product_images pi WHERE pi.product_id = p.id AND pi.is_primary = true LIMIT 1) as image_url,
    COUNT(DISTINCT oi.order_id) as order_count
FROM products p
JOIN product_variants pv ON pv.product_id = p.id
JOIN order_items oi ON oi.variant_id = pv.id
JOIN orders o ON o.id = oi.order_id AND o.status IN ('Completed', 'Delivered')
WHERE p.is_deleted = false AND p.status = 'Active'
GROUP BY p.id
ORDER BY order_count DESC
LIMIT 50;
```

---

## 9. Authentication & Authorization

### 9.1 Authentication Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Authentication Flow                        │
│                                                             │
│  Customer Login                 Admin Login                  │
│  ──────────────                ────────────                  │
│                                                             │
│  POST /api/v1/auth/login       POST /api/v1/auth/admin/login│
│  { email, password }           { email, password, 2FA? }    │
│         │                              │                    │
│         ▼                              ▼                    │
│  ┌──────────────┐             ┌──────────────┐              │
│  │ Validate     │             │ Validate     │              │
│  │ Credentials  │             │ Credentials  │              │
│  │ (Identity)   │             │ + Role Check │              │
│  └──────┬───────┘             └──────┬───────┘              │
│         │                            │                      │
│         ▼                            ▼                      │
│  ┌──────────────────────────────────────┐                   │
│  │       Generate JWT Token Pair        │                   │
│  │                                      │                   │
│  │  Access Token  (15 min expiry)       │                   │
│  │  ┌────────────────────────────┐      │                   │
│  │  │ Header: { alg, typ }      │      │                   │
│  │  │ Payload:                   │      │                   │
│  │  │   sub: userId (GUID)      │      │                   │
│  │  │   email: user@email.com   │      │                   │
│  │  │   role: "Customer"|"Admin"│      │                   │
│  │  │   permissions: [...]      │      │                   │
│  │  │   iat, exp, jti           │      │                   │
│  │  │ Signature: HMAC-SHA256    │      │                   │
│  │  └────────────────────────────┘      │                   │
│  │                                      │                   │
│  │  Refresh Token (7 days expiry)       │                   │
│  │  ┌────────────────────────────┐      │                   │
│  │  │ Stored in DB (hashed)     │      │                   │
│  │  │ One-time use              │      │                   │
│  │  │ Rotated on each refresh   │      │                   │
│  │  │ Device/IP fingerprinting  │      │                   │
│  │  └────────────────────────────┘      │                   │
│  └──────────────────────────────────────┘                   │
│                                                             │
│  Token Refresh: POST /api/v1/auth/refresh                   │
│  { refreshToken } → new access + refresh tokens             │
│                                                             │
│  Password Reset: POST /api/v1/auth/forgot-password          │
│  { email } → sends reset link with time-limited token       │
│                                                             │
│  Email Verify: POST /api/v1/auth/verify-email               │
│  { token } → marks email as verified                        │
│                                                             │
│  Google Login (future):                                      │
│  GET /api/v1/auth/google → OAuth2 redirect flow             │
│  Callback creates/links user account, issues JWT            │
└─────────────────────────────────────────────────────────────┘
```

### 9.2 Authorization Model

```
Roles & Permissions (RBAC)
──────────────────────────

Roles:
  ├── SuperAdmin     (full access, can manage other admins)
  ├── Admin          (manage products, orders, content)
  ├── ContentEditor  (manage blogs, CMS, banners only)
  └── Customer       (shop, order, review)

Permissions (granular):
  ├── Catalog
  │   ├── catalog:products:read
  │   ├── catalog:products:create
  │   ├── catalog:products:update
  │   ├── catalog:products:delete
  │   ├── catalog:categories:manage
  │   ├── catalog:collections:manage
  │   └── catalog:inventory:manage
  ├── Orders
  │   ├── orders:read
  │   ├── orders:update-status
  │   └── orders:refund
  ├── Customers
  │   ├── customers:read
  │   └── customers:manage
  ├── Content
  │   ├── content:cms:manage
  │   ├── content:blog:manage
  │   ├── content:banners:manage
  │   ├── content:media:manage
  │   └── content:seo:manage
  ├── Coupons
  │   └── coupons:manage
  ├── Reports
  │   ├── reports:dashboard
  │   └── reports:analytics
  └── System
      ├── system:roles:manage
      ├── system:audit:read
      └── system:settings:manage
```

Authorization is enforced at two levels:
1. **Controller level**: `[Authorize(Roles = "Admin")]` or custom `[RequirePermission("catalog:products:create")]` attribute
2. **Handler level**: MediatR pipeline behavior checks permissions before handler execution

### 9.3 Security Token Lifecycle

| Token | Storage | Expiry | Rotation |
|-------|---------|--------|----------|
| Access (JWT) | Client memory (never localStorage) | 15 minutes | On refresh |
| Refresh | HTTP-only, Secure, SameSite=Strict cookie + DB (hashed) | 7 days | Every use (rotation) |
| Email verification | DB | 24 hours | Single use |
| Password reset | DB | 1 hour | Single use |
| API Key (admin) | Environment variable / secrets | No expiry | Manual rotation |

---

## 10. Payment Architecture

### 10.1 Payment Gateway Abstraction

```
┌───────────────────────────────────────────────────────────────┐
│                   Payment Architecture                        │
│                                                               │
│  ┌──────────────┐                                            │
│  │ OrderService │                                            │
│  │ (Application)│                                            │
│  └──────┬───────┘                                            │
│         │ uses                                                │
│  ┌──────▼───────────────────┐                                │
│  │ IPaymentGateway          │  ← Domain Interface            │
│  │                          │                                │
│  │ + CreateOrder(amount,    │                                │
│  │     currency, metadata)  │                                │
│  │ + VerifyPayment(         │                                │
│  │     gatewayTxnId)        │                                │
│  │ + InitiateRefund(        │                                │
│  │     paymentId, amount)   │                                │
│  │ + GetPaymentStatus(      │                                │
│  │     gatewayTxnId)        │                                │
│  └──────────────────────────┘                                │
│         ▲                                                     │
│         │ implements                                          │
│  ┌──────┴──────────────────────────────────────────────┐     │
│  │                                                      │     │
│  │  ┌──────────────────┐    ┌──────────────────┐       │     │
│  │  │ RazorpayGateway  │    │ StripeGateway    │       │     │
│  │  │                  │    │                  │       │     │
│  │  │ • Uses Razorpay  │    │ • Uses Stripe    │       │     │
│  │  │   .NET SDK       │    │   .NET SDK       │       │     │
│  │  │ • INR focused    │    │ • Multi-currency │       │     │
│  │  │ • UPI support    │    │ • Card focused   │       │     │
│  │  └──────────────────┘    └──────────────────┘       │     │
│  │                                                      │     │
│  │  ┌──────────────────┐                               │     │
│  │  │PaymentGateway    │  ← Factory pattern             │     │
│  │  │Factory           │                               │     │
│  │  │                  │  Selects gateway based on:     │     │
│  │  │ + Create(gateway)│  • Config / admin setting      │     │
│  │  │   → IGateway     │  • Currency                    │     │
│  │  └──────────────────┘  • Customer preference         │     │
│  └──────────────────────────────────────────────────────┘     │
│                                                               │
│  Webhook Processing:                                          │
│  ──────────────────                                          │
│  POST /api/v1/webhooks/razorpay  → RazorpayWebhookProcessor │
│  POST /api/v1/webhooks/stripe    → StripeWebhookProcessor   │
│                                                               │
│  Both verify signatures, update Payment status,              │
│  trigger domain events (PaymentCompletedEvent)               │
└───────────────────────────────────────────────────────────────┘
```

### 10.2 Payment Flow

```
Customer                  API                  Payment Gateway        Webhook
   │                       │                        │                    │
   │ 1. Place Order        │                        │                    │
   │──────────────────────▶│                        │                    │
   │                       │ 2. Create Payment      │                    │
   │                       │   (Pending status)     │                    │
   │                       │ 3. Create Gateway Order│                    │
   │                       │───────────────────────▶│                    │
   │                       │                        │                    │
   │                       │ 4. Return gateway      │                    │
   │  5. Gateway order ID  │    order ID            │                    │
   │◀──────────────────────│◀───────────────────────│                    │
   │                       │                        │                    │
   │ 6. Complete payment   │                        │                    │
   │   (client-side SDK)   │                        │                    │
   │──────────────────────────────────────────────▶│                    │
   │                       │                        │                    │
   │                       │                        │ 7. Webhook         │
   │                       │                        │   payment.success  │
   │                       │◀───────────────────────────────────────────│
   │                       │ 8. Verify signature    │                    │
   │                       │ 9. Update Payment      │                    │
   │                       │    (Completed)         │                    │
   │                       │ 10. Update Order       │                    │
   │                       │    (Confirmed)         │                    │
   │                       │ 11. Deduct inventory   │                    │
   │                       │ 12. Send confirmation  │                    │
   │  13. Order confirmed  │    email               │                    │
   │◀──────────────────────│                        │                    │
```

### 10.3 Payment Security

- **Never handle raw card data** — Use Razorpay Checkout / Stripe Elements (client-side tokenization)
- **Webhook signature verification** — Every webhook is verified using gateway-specific HMAC signatures
- **Idempotency keys** — Prevent duplicate payments on retries
- **Payment amount verification** — Backend re-calculates total; never trusts client-sent amounts
- **PCI DSS Level 4** — Since we use hosted checkouts, we only need SAQ-A (simplest compliance level)
- **Refund authorization** — Refunds require admin role + audit logging

---

## 11. API Organization

### 11.1 API Routes

```
Customer APIs (Public + Authenticated)
──────────────────────────────────────

Catalog (Public — heavily cached)
  GET    /api/v1/products                    # List products (paginated, filtered, sorted)
  GET    /api/v1/products/{slug}             # Product detail by slug
  GET    /api/v1/products/{id}/reviews       # Product reviews
  GET    /api/v1/products/{id}/related       # Related products
  GET    /api/v1/categories                  # Category tree
  GET    /api/v1/categories/{slug}/products  # Products by category
  GET    /api/v1/collections                 # All collections
  GET    /api/v1/collections/{slug}          # Collection detail with products
  GET    /api/v1/search?q=sofa&category=...  # Full-text search with filters
  GET    /api/v1/homepage                    # Pre-aggregated homepage data

Authentication (Public)
  POST   /api/v1/auth/register               # Customer registration
  POST   /api/v1/auth/login                  # Customer login → JWT
  POST   /api/v1/auth/admin/login            # Admin login → JWT
  POST   /api/v1/auth/refresh                # Refresh access token
  POST   /api/v1/auth/forgot-password        # Request password reset
  POST   /api/v1/auth/reset-password         # Reset with token
  POST   /api/v1/auth/verify-email           # Verify email address

Account (Authenticated — Customer)
  GET    /api/v1/account/profile             # Get profile
  PUT    /api/v1/account/profile             # Update profile
  PUT    /api/v1/account/change-password     # Change password
  GET    /api/v1/account/addresses           # List saved addresses
  POST   /api/v1/account/addresses           # Add address
  PUT    /api/v1/account/addresses/{id}      # Update address
  DELETE /api/v1/account/addresses/{id}      # Delete address
  GET    /api/v1/account/notifications       # User notifications

Shopping (Authenticated or Session-based)
  GET    /api/v1/cart                        # Get current cart
  POST   /api/v1/cart/items                  # Add item to cart
  PUT    /api/v1/cart/items/{id}             # Update cart item quantity
  DELETE /api/v1/cart/items/{id}             # Remove cart item
  POST   /api/v1/cart/coupon                 # Apply coupon
  DELETE /api/v1/cart/coupon                 # Remove coupon

  GET    /api/v1/wishlist                    # Get wishlist
  POST   /api/v1/wishlist/{productId}        # Add to wishlist
  DELETE /api/v1/wishlist/{productId}        # Remove from wishlist

Orders (Authenticated — Customer)
  POST   /api/v1/orders                      # Place order (from cart)
  GET    /api/v1/orders                      # Order history
  GET    /api/v1/orders/{id}                 # Order detail
  GET    /api/v1/orders/{id}/track           # Track order status

Payments (Authenticated)
  POST   /api/v1/payments/initiate           # Create payment for order
  POST   /api/v1/payments/verify             # Verify payment (client callback)

Webhooks (Public — signature verified)
  POST   /api/v1/webhooks/razorpay           # Razorpay webhook
  POST   /api/v1/webhooks/stripe             # Stripe webhook

Reviews (Authenticated — Customer)
  POST   /api/v1/products/{id}/reviews       # Submit review

Content (Public — cached)
  GET    /api/v1/pages/{slug}                # CMS page by slug
  GET    /api/v1/blog                        # Blog listing
  GET    /api/v1/blog/{slug}                 # Blog post detail
  GET    /api/v1/banners?position={position} # Active banners by position

Contact (Public — rate limited)
  POST   /api/v1/contact                     # Submit contact form

───────────────────────────────────────────────────────────────

Admin APIs (Authenticated — Admin role + permission checks)
──────────────────────────────────────────────────────────────

Dashboard
  GET    /api/v1/admin/dashboard              # KPIs, recent orders, stats

Products
  GET    /api/v1/admin/products               # List all (incl. draft/archived)
  GET    /api/v1/admin/products/{id}          # Detail for editing
  POST   /api/v1/admin/products               # Create product
  PUT    /api/v1/admin/products/{id}          # Update product
  DELETE /api/v1/admin/products/{id}          # Soft delete
  POST   /api/v1/admin/products/{id}/images   # Upload images
  PUT    /api/v1/admin/products/{id}/images/reorder  # Reorder images
  DELETE /api/v1/admin/products/{id}/images/{imgId}  # Delete image

Variants
  POST   /api/v1/admin/products/{id}/variants       # Add variant
  PUT    /api/v1/admin/products/{id}/variants/{vid}  # Update variant
  DELETE /api/v1/admin/products/{id}/variants/{vid}  # Delete variant

Categories
  GET    /api/v1/admin/categories             # Full category tree
  POST   /api/v1/admin/categories             # Create
  PUT    /api/v1/admin/categories/{id}        # Update
  DELETE /api/v1/admin/categories/{id}        # Delete
  PUT    /api/v1/admin/categories/reorder     # Reorder

Collections
  CRUD   /api/v1/admin/collections            # Standard CRUD
  POST   /api/v1/admin/collections/{id}/products  # Add products to collection

Inventory
  GET    /api/v1/admin/inventory              # Stock levels overview
  PUT    /api/v1/admin/inventory/{variantId}  # Update stock
  GET    /api/v1/admin/inventory/low-stock    # Low stock alerts

Orders
  GET    /api/v1/admin/orders                 # All orders (filtered)
  GET    /api/v1/admin/orders/{id}            # Order detail
  PUT    /api/v1/admin/orders/{id}/status     # Update status
  POST   /api/v1/admin/orders/{id}/refund     # Initiate refund

Customers
  GET    /api/v1/admin/customers              # Customer list
  GET    /api/v1/admin/customers/{id}         # Customer detail + orders

Coupons
  CRUD   /api/v1/admin/coupons               # Standard CRUD

Content
  CRUD   /api/v1/admin/cms-pages             # CMS pages
  CRUD   /api/v1/admin/blog-posts            # Blog posts
  CRUD   /api/v1/admin/banners               # Banners

Media
  GET    /api/v1/admin/media                  # Media library
  POST   /api/v1/admin/media/upload           # Upload file(s)
  DELETE /api/v1/admin/media/{id}             # Delete file

SEO
  GET    /api/v1/admin/seo/{entityType}/{id}  # Get SEO metadata
  PUT    /api/v1/admin/seo/{entityType}/{id}  # Update SEO metadata

Roles & Permissions
  GET    /api/v1/admin/roles                  # List roles
  POST   /api/v1/admin/roles                  # Create role
  PUT    /api/v1/admin/roles/{id}             # Update role permissions
  DELETE /api/v1/admin/roles/{id}             # Delete role

Reports
  GET    /api/v1/admin/reports/sales          # Sales report (date range)
  GET    /api/v1/admin/reports/products       # Product performance
  GET    /api/v1/admin/reports/customers      # Customer analytics

Audit
  GET    /api/v1/admin/audit-logs             # Audit log (paginated)
```

### 11.2 API Conventions

| Convention | Standard |
|-----------|----------|
| **Versioning** | URL prefix: `/api/v1/` |
| **Response format** | `{ "success": true, "data": {...}, "errors": [...], "meta": { "page", "pageSize", "totalCount" } }` |
| **Error format** | RFC 7807 ProblemDetails: `{ "type", "title", "status", "detail", "errors": {} }` |
| **Pagination** | Query params: `?page=1&pageSize=20&sortBy=price&sortOrder=asc` |
| **Filtering** | Query params: `?category=sofas&minPrice=10000&maxPrice=50000&material=wood` |
| **Status codes** | 200 (OK), 201 (Created), 204 (No Content), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 409 (Conflict), 422 (Validation), 429 (Rate Limited), 500 (Server Error) |
| **CORS** | Allow specific frontend origin(s) only |
| **Content-Type** | `application/json` (default), `multipart/form-data` (uploads) |
| **Rate limits** | Public: 100 req/min, Authenticated: 300 req/min, Auth endpoints: 10 req/min |

---

## 12. Caching Strategy

### 12.1 Multi-Layer Caching Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Caching Layers                             │
│                                                              │
│  Layer 1: CDN Cache (Cloudflare / Azure CDN)                │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Static assets (CSS, JS, fonts): 1 year, immutable   │  │
│  │ • Product images: 1 year, immutable (hash-based URLs) │  │
│  │ • API responses: selective, short TTL (60-300s)        │  │
│  │ • HTML pages (if SSR): 60s with stale-while-revalidate│  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Layer 2: Response/Output Cache (ASP.NET Core)              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Output caching middleware for GET endpoints          │  │
│  │ • Vary by: query params, Accept-Language               │  │
│  │ • Tag-based invalidation (e.g., "products", "cat:5")  │  │
│  │ • Backed by Redis for multi-instance consistency       │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Layer 3: Application Cache (Redis)                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • MediatR CachingBehavior for query handlers           │  │
│  │ • Serialized DTOs as cached values                     │  │
│  │ • Key pattern: "gharcraft:{module}:{entity}:{id|slug}" │  │
│  │ • TTL varies by entity type                            │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Layer 4: Database-Level Cache                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • PostgreSQL shared_buffers (25% of RAM)               │  │
│  │ • Materialized views for aggregates                    │  │
│  │ • Read replica for reporting queries                   │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 12.2 Cache TTL by Entity

| Cache Target | TTL | Invalidation Strategy |
|-------------|-----|----------------------|
| **Homepage data** (banners, featured, categories) | 5 min | Tag-based: invalidate on banner/product/category change |
| **Category tree** | 30 min | Invalidate on category CRUD |
| **Category products listing** | 5 min | Invalidate on product CRUD in that category |
| **Product detail** | 10 min | Invalidate on product update |
| **Search results** | 2 min | Time-based expiry only (too many combinations) |
| **Product reviews** | 5 min | Invalidate on new review approval |
| **Blog posts listing** | 15 min | Invalidate on blog CRUD |
| **CMS pages** | 30 min | Invalidate on page update |
| **User cart** | No cache | Always fresh (Redis-backed session) |
| **User profile** | 5 min | Invalidate on profile update |
| **Admin dashboard** | 1 min | Short TTL, near-real-time |

### 12.3 Cache Key Schema

```
gharcraft:homepage:data                          → aggregated homepage JSON
gharcraft:categories:tree                        → full category tree
gharcraft:categories:{slug}:products:p{page}     → paginated product list
gharcraft:products:{slug}                        → product detail DTO
gharcraft:products:{id}:reviews:p{page}          → paginated reviews
gharcraft:products:{id}:related                  → related products
gharcraft:collections:{slug}                     → collection with products
gharcraft:search:{queryHash}                     → search results (hashed query)
gharcraft:cms:{slug}                             → CMS page content
gharcraft:blog:list:p{page}                      → blog listing
gharcraft:blog:{slug}                            → blog post detail
gharcraft:banners:{position}                     → active banners
gharcraft:user:{userId}:cart                     → user cart (if Redis-backed)
gharcraft:user:{userId}:wishlist                 → user wishlist
```

### 12.4 Cache Invalidation Pattern

```csharp
// Example: When admin updates a product, invalidate related caches
// This is handled in the UpdateProductHandler via ICacheService

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result>
{
    // After successful update:
    // 1. Invalidate product detail cache
    await _cache.RemoveAsync($"gharcraft:products:{product.Slug}");
    
    // 2. Invalidate category products cache (all pages)
    await _cache.RemoveByPrefixAsync($"gharcraft:categories:{category.Slug}:products:");
    
    // 3. Invalidate homepage if product is featured
    if (product.IsFeatured)
        await _cache.RemoveAsync("gharcraft:homepage:data");
    
    // 4. Invalidate search cache (prefix-based)
    await _cache.RemoveByPrefixAsync("gharcraft:search:");
    
    // 5. Tag-based output cache invalidation
    await _outputCache.EvictByTagAsync("products");
}
```

---

## 13. Cloud & Deployment Architecture

### 13.1 Production Deployment Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                    PRODUCTION ARCHITECTURE                           │
│                                                                      │
│  ┌─────────────┐                                                    │
│  │   Internet   │                                                    │
│  └──────┬───────┘                                                    │
│         │                                                            │
│  ┌──────▼──────────────────┐                                        │
│  │     Cloudflare CDN      │  • SSL termination                     │
│  │                         │  • DDoS protection                     │
│  │  • Static assets        │  • WAF rules                           │
│  │  • Image optimization   │  • Edge caching                        │
│  │  • Bot protection       │  • Rate limiting (L7)                  │
│  └──────┬──────────────────┘                                        │
│         │                                                            │
│  ┌──────▼──────────────────┐                                        │
│  │  Cloud Load Balancer    │  • Health checks                       │
│  │  (Azure LB / AWS ALB)   │  • SSL passthrough                     │
│  │                         │  • Sticky sessions (disabled)          │
│  └──────┬──────────────────┘                                        │
│         │                                                            │
│  ┌──────▼──────────────────────────────────────────────┐            │
│  │        App Service / Container Instances              │            │
│  │                                                      │            │
│  │  ┌──────────────┐  ┌──────────────┐                  │            │
│  │  │ API Instance │  │ API Instance │  (auto-scaled)   │            │
│  │  │     #1       │  │     #2       │   1–4 instances  │            │
│  │  │              │  │              │                  │            │
│  │  │ ASP.NET Core │  │ ASP.NET Core │                  │            │
│  │  │ + Hangfire   │  │              │  (Hangfire on    │            │
│  │  │   Server     │  │              │   instance #1    │            │
│  │  └──────────────┘  └──────────────┘   only)          │            │
│  └─────┬──────┬──────┬──────────────────────────────────┘            │
│        │      │      │                                               │
│   ┌────▼──┐ ┌─▼────┐ ┌▼──────────────────┐                          │
│   │Postgre│ │Redis │ │Azure Blob Storage │                          │
│   │SQL    │ │Cache │ │                   │                          │
│   │       │ │      │ │ /products/        │                          │
│   │Primary│ │6.x   │ │ /media/           │                          │
│   │  +    │ │      │ │ /cms/             │                          │
│   │Read   │ │      │ │ /banners/         │                          │
│   │Replica│ └──────┘ └───────────────────┘                          │
│   │(scale)│                                                          │
│   └───────┘                                                          │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │                     MONITORING STACK                      │        │
│  │                                                          │        │
│  │  ┌─────────────┐  ┌────────────┐  ┌──────────────────┐  │        │
│  │  │ Application │  │ Seq / ELK  │  │ Azure Monitor /  │  │        │
│  │  │ Insights /  │  │ (Logs)     │  │ Grafana +        │  │        │
│  │  │ OpenTelemetry│  │            │  │ Prometheus       │  │        │
│  │  │ (Traces +   │  │ Serilog    │  │ (Metrics)        │  │        │
│  │  │  Metrics)   │  │ sinks      │  │                  │  │        │
│  │  └─────────────┘  └────────────┘  └──────────────────┘  │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │                     SECRETS & CONFIG                      │        │
│  │                                                          │        │
│  │  Azure Key Vault / AWS Secrets Manager                   │        │
│  │  • Database connection strings                           │        │
│  │  • Redis connection strings                              │        │
│  │  • JWT signing keys                                      │        │
│  │  • Payment gateway API keys                              │        │
│  │  • Email provider credentials                            │        │
│  │  • Blob storage connection strings                       │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │                     BACKUP & DR                           │        │
│  │                                                          │        │
│  │  • PostgreSQL: automated daily backups (35-day retention)│        │
│  │  • Point-in-time restore (PITR) — up to 5 min RPO       │        │
│  │  • Blob storage: geo-redundant replication (GRS)         │        │
│  │  • Redis: AOF persistence + RDB snapshots                │        │
│  │  • Cross-region replication (future, for DR)             │        │
│  │  • Infrastructure as Code (Terraform) — reproducible     │        │
│  └──────────────────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────────────────┘
```

### 13.2 Estimated Cloud Costs (Azure, MVP tier)

| Service | Tier | Estimated Monthly Cost |
|---------|------|----------------------|
| App Service | B2 (2 vCPU, 3.5 GB) | ~₹3,500 ($42) |
| PostgreSQL Flexible Server | Burstable B1ms | ~₹2,500 ($30) |
| Azure Cache for Redis | Basic C0 (250MB) | ~₹1,500 ($18) |
| Blob Storage | LRS, 50GB | ~₹100 ($1.2) |
| Cloudflare CDN | Free tier | ₹0 |
| Azure Key Vault | Standard | ~₹50 ($0.6) |
| Application Insights | Free tier (5GB/month) | ₹0 |
| **Total (MVP)** | | **~₹7,650/month (~$92)** |

> [!TIP]
> These costs scale linearly. At 100K users/day you'd move to Standard tier (≈₹25,000-35,000/month). Still very economical.

---

## 14. Security Plan

### 14.1 OWASP Top 10 Coverage

| # | Vulnerability | Mitigation |
|---|--------------|------------|
| A01 | **Broken Access Control** | RBAC with permission checks at controller + handler level. Resource-level authorization (user can only access own orders). Admin routes behind `[Authorize(Roles = "Admin")]`. |
| A02 | **Cryptographic Failures** | All traffic over HTTPS (HSTS enforced). Passwords hashed with bcrypt (ASP.NET Identity default). JWT signed with HMAC-SHA256. Secrets in Key Vault, never in code. |
| A03 | **Injection (SQL, NoSQL)** | EF Core parameterized queries by default. No raw SQL without parameterization. Input validation via FluentValidation on every command. |
| A04 | **Insecure Design** | Clean Architecture separates concerns. Domain validation prevents invalid states. Threat modeling during design phase. |
| A05 | **Security Misconfiguration** | Remove default error pages. Custom exception middleware returns ProblemDetails. CORS restricted to known origins. X-Content-Type-Options, X-Frame-Options headers. |
| A06 | **Vulnerable Components** | Dependabot alerts enabled. Regular NuGet package audits (`dotnet list package --vulnerable`). Minimal dependencies. |
| A07 | **Auth Failures** | Refresh token rotation. Account lockout after 5 failed attempts. Rate limiting on auth endpoints (10 req/min). |
| A08 | **Data Integrity** | Webhook signature verification for payments. CSRF tokens for state-changing operations. SameSite cookies. |
| A09 | **Logging Failures** | Serilog structured logging with correlation IDs. Audit logs for all admin actions. Security events (login, password change, permission changes) always logged. PII redaction in logs. |
| A10 | **SSRF** | No user-controlled URLs in server-side requests. Image URLs validated against allowlist. |

### 14.2 Additional Security Measures

```
Authentication Security
──────────────────────
• Passwords: bcrypt with work factor 12 (ASP.NET Identity default)
• JWT: RS256 or HMAC-SHA256 with rotatable keys
• Refresh tokens: one-time use, stored hashed, device fingerprinted
• Account lockout: 5 attempts → 15 min lockout
• Password policy: min 8 chars, 1 uppercase, 1 number, 1 special

Transport Security
──────────────────
• HTTPS everywhere (HSTS max-age=31536000)
• TLS 1.2+ only
• Certificate management: Let's Encrypt (auto-renewal) or Azure managed cert
• CDN SSL termination at edge

API Security
───────────
• Rate limiting:
    - Public APIs: 100 requests/minute per IP
    - Authenticated APIs: 300 requests/minute per user
    - Auth endpoints: 10 requests/minute per IP
    - Webhook endpoints: IP allowlist from payment gateways
• Request size limits: 10MB max (50MB for media uploads)
• Input validation on every endpoint (FluentValidation)
• Output encoding (automatic in ASP.NET Core)
• CORS: explicit origin allowlist

Data Security
─────────────
• PII encryption at rest (Azure/AWS managed encryption)
• Payment data: never stored (tokenized via Razorpay/Stripe)
• Soft deletes preserve audit trail
• Database connections over SSL
• Connection strings in Key Vault (never in appsettings.json)

Security Headers (Middleware)
─────────────────────────────
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 0  (deprecated, rely on CSP)
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; ...
Permissions-Policy: camera=(), microphone=(), geolocation=()
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

---

## 15. Scalability Roadmap

### 15.1 Scaling from 1,000 to 100,000 Users/Day

```
Phase 1: MVP (0 – 1,000 users/day)
════════════════════════════════════

Architecture:
  • Single API instance (App Service B2)
  • Single PostgreSQL instance (Burstable B1ms)
  • Redis (Basic C0, 250MB)
  • Cloudflare CDN (free tier)
  • Images on Blob Storage

Bottleneck: None at this scale.
Cost: ~₹7,650/month

────────────────────────────────────────────────────

Phase 2: Growth (1,000 – 10,000 users/day)
════════════════════════════════════════════

Changes:
  ✅ Scale API to 2 instances (horizontal)
  ✅ Add PostgreSQL read replica
  ✅ Upgrade Redis to Standard C1 (1GB)
  ✅ Enable output caching on all GET endpoints
  ✅ Add materialized views for homepage aggregates
  ✅ Background job for materialized view refresh
  ✅ Implement PgBouncer for connection pooling

Why no redesign needed:
  • Stateless API (JWT auth, no server sessions) → horizontal scaling works
  • Redis handles distributed cache + rate limiting across instances
  • Read replica offloads reporting and complex queries
  • Output caching eliminates 80%+ of database reads

Cost: ~₹15,000/month

────────────────────────────────────────────────────

Phase 3: Scale (10,000 – 50,000 users/day)
═══════════════════════════════════════════

Changes:
  ✅ Scale to 3-4 API instances with auto-scaling rules
  ✅ Upgrade PostgreSQL to General Purpose (4 vCPU)
  ✅ Add OpenSearch for catalog search (replaces PG FTS)
  ✅ Implement CDN-level API response caching for catalog endpoints
  ✅ Move Hangfire to dedicated worker instance
  ✅ Add Application Insights alerting

Why no redesign needed:
  • CQRS pattern means read paths can be independently optimized
  • OpenSearch integration is behind ISearchService interface — swap implementation
  • Worker isolation prevents background jobs from impacting API performance

Cost: ~₹25,000-35,000/month

────────────────────────────────────────────────────

Phase 4: High Scale (50,000 – 100,000 users/day)
═════════════════════════════════════════════════

Changes:
  ✅ Move to Kubernetes (AKS) or Azure Container Apps
  ✅ PostgreSQL with 2 read replicas
  ✅ Redis Cluster (Premium tier)
  ✅ Consider extracting Catalog module as standalone service (optional)
  ✅ Implement event-driven patterns for order processing (Azure Service Bus)
  ✅ Add cross-region CDN POP configuration
  ✅ Database table partitioning for orders (by year)

Why no redesign needed:
  • Clean Architecture means infrastructure swaps don't affect domain
  • Modular monolith boundaries make extraction straightforward
  • Container-first design from day 1

Cost: ~₹50,000-80,000/month
```

### 15.2 What Changes at Each Scale

| Scale | Database | Cache | Compute | Search | Workers |
|-------|----------|-------|---------|--------|---------|
| 1K/day | Single PG | Redis Basic | 1 instance | PG FTS | In-process |
| 10K/day | PG + read replica | Redis Standard | 2 instances | PG FTS | In-process |
| 50K/day | PG GP + read replica | Redis Standard | 3-4 instances (auto-scale) | OpenSearch | Dedicated instance |
| 100K/day | PG GP + 2 read replicas | Redis Premium | AKS (3-6 pods) | OpenSearch cluster | Dedicated pods |

---

## 16. DevOps & CI/CD

### 16.1 Local Development Environment

```yaml
# docker-compose.yml
services:
  api:
    build:
      context: .
      dockerfile: src/GharCraft.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;...
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - postgres
      - redis

  postgres:
    image: postgres:16-alpine
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: gharcraft
      POSTGRES_USER: gharcraft
      POSTGRES_PASSWORD: dev_password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  seq:   # optional: structured log viewer
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      ACCEPT_EULA: Y

volumes:
  postgres_data:
```

### 16.2 CI/CD Pipeline (GitHub Actions)

```
┌───────────────────────────────────────────────────────────┐
│                    CI/CD Pipeline                         │
│                                                           │
│  ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │
│  │  Push /  │  │  Build + │  │ Run      │  │ Security │  │
│  │  PR      │─▶│  Restore │─▶│ Tests    │─▶│ Scan     │  │
│  │          │  │          │  │          │  │          │  │
│  │ trigger  │  │ dotnet   │  │ Unit +   │  │ dotnet   │  │
│  │          │  │ build    │  │ Integ.   │  │ list pkg │  │
│  │          │  │          │  │          │  │ --vuln.  │  │
│  └─────────┘  └──────────┘  └──────────┘  └────┬─────┘  │
│                                                 │        │
│                   On merge to main:             │        │
│                                                 ▼        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │ Docker   │  │ Push to  │  │ Deploy   │  │ Run      │ │
│  │ Build    │─▶│ Registry │─▶│ Staging  │─▶│ Smoke    │ │
│  │          │  │ (ACR /   │  │          │  │ Tests    │ │
│  │          │  │  GHCR)   │  │          │  │          │ │
│  └──────────┘  └──────────┘  └──────────┘  └────┬─────┘ │
│                                                  │       │
│                   Manual approval:               ▼       │
│                                           ┌──────────┐   │
│                                           │ Deploy   │   │
│                                           │ Prod     │   │
│                                           │          │   │
│                                           │ Blue/    │   │
│                                           │ Green    │   │
│                                           └──────────┘   │
└───────────────────────────────────────────────────────────┘
```

### 16.3 Monitoring & Observability Stack

| Tool | Purpose | Integration |
|------|---------|-------------|
| **Serilog** | Structured logging | Console + Seq/ELK sink; correlation IDs; PII redaction |
| **OpenTelemetry** | Distributed tracing + metrics | ASP.NET Core instrumentation; EF Core instrumentation; Redis instrumentation |
| **Prometheus** | Metrics collection | `prometheus-net` for .NET metrics; custom business metrics (orders/min, cart abandonment) |
| **Grafana** | Dashboards & alerting | Pre-built dashboards for .NET, PostgreSQL, Redis; custom ecommerce dashboards |
| **Seq** | Log aggregation & search | Serilog sink; structured query language; alerting on error patterns |
| **Health Checks** | Readiness & liveness | ASP.NET Core HealthChecks: DB, Redis, Blob Storage; expose `/health` and `/health/ready` |

### 16.4 Dockerfile (Multi-stage Build)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY *.sln .
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do \
      mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; \
    done
RUN dotnet restore
COPY . .
RUN dotnet publish src/GharCraft.Api/GharCraft.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "GharCraft.Api.dll"]
```

---

## 17. Service Boundaries

### 17.1 Module Dependency Map

```
┌─────────────────────────────────────────────────────────────┐
│                    Module Dependencies                       │
│                                                             │
│                  ┌──────────┐                               │
│                  │ Shared   │ ← Used by ALL modules         │
│                  │ Kernel   │                               │
│                  └────┬─────┘                               │
│                       │                                      │
│         ┌─────────────┼────────────────┐                    │
│         │             │                │                    │
│    ┌────▼─────┐  ┌────▼──────┐  ┌─────▼──────┐            │
│    │ Identity │  │  Catalog  │  │  Content   │            │
│    │ Module   │  │  Module   │  │  Module    │            │
│    │          │  │           │  │            │            │
│    │ No deps  │  │ Depends:  │  │ Depends:   │            │
│    │ on other │  │ Identity  │  │ Identity   │            │
│    │ modules  │  │ (userId   │  │ (authorId) │            │
│    │          │  │  for      │  │            │            │
│    │          │  │  reviews) │  │            │            │
│    └────┬─────┘  └────┬──────┘  └────────────┘            │
│         │             │                                     │
│         │        ┌────▼──────┐                              │
│         │        │ Shopping  │                              │
│         └───────▶│ Module    │                              │
│                  │           │                              │
│                  │ Depends:  │                              │
│                  │ Identity  │                              │
│                  │ Catalog   │                              │
│                  │ (product  │                              │
│                  │  data for │                              │
│                  │  cart/    │                              │
│                  │  orders)  │                              │
│                  └────┬──────┘                              │
│                       │                                     │
│                  ┌────▼──────┐                              │
│                  │Analytics  │                              │
│                  │Module     │                              │
│                  │           │                              │
│                  │ Depends:  │                              │
│                  │ ALL (read │                              │
│                  │  only)    │                              │
│                  └───────────┘                              │
│                                                             │
│  ┌──────────────┐                                           │
│  │Notifications │  Cross-cutting: triggered by domain       │
│  │Module        │  events from any module                   │
│  └──────────────┘                                           │
└─────────────────────────────────────────────────────────────┘
```

### 17.2 Communication Between Modules

| Type | Mechanism | Example |
|------|----------|---------|
| **Synchronous (same process)** | Direct interface call via DI | CartService calls `IProductRepository` to get current price |
| **Domain Events (same process)** | MediatR notifications | `OrderPlacedEvent` → NotificationModule sends email |
| **Future (if extracted)** | Message queue (Azure Service Bus) | OrderService publishes event → InventoryService subscribes |

### 17.3 Future Extraction Path

If any module needs to become a standalone microservice:

1. **Extract shared contracts** (interfaces, DTOs, events) into a NuGet package
2. **Replace direct calls** with HTTP calls or message queue
3. **Give each service its own database** (data ownership)
4. **Deploy independently** as a separate container

The Clean Architecture + modular boundaries make this extraction mechanical, not architectural.

---

## 18. Development Roadmap & MVP Plan

### 18.1 Phase 0: Foundation (Weeks 1–2)

```
[ ] Project scaffolding (solution structure, projects, references)
[ ] Clean Architecture layer setup
[ ] Docker Compose (PostgreSQL + Redis + Seq)
[ ] EF Core DbContext + initial migrations
[ ] Base entity classes, value objects, Result type
[ ] MediatR + FluentValidation + pipeline behaviors
[ ] Global exception handling middleware
[ ] Serilog configuration
[ ] Health checks
[ ] Swagger/OpenAPI setup
[ ] CI pipeline (GitHub Actions: build + test)
```

### 18.2 Phase 1: MVP — Catalog + Auth (Weeks 3–6)

```
[ ] Identity module: User entity, registration, login, JWT, refresh tokens
[ ] Password reset + email verification (email service stub)
[ ] Admin login with role checks
[ ] Category CRUD (admin) + public API
[ ] Product CRUD (admin) with variants, images, attributes
[ ] Product listing API with pagination, sorting, filtering
[ ] Product detail API by slug
[ ] Full-text search (PostgreSQL tsvector)
[ ] Image upload to Blob Storage + CDN
[ ] Media library (admin)
[ ] Redis caching for catalog queries
[ ] Output caching middleware
[ ] Seed data (sample categories + products)
```

### 18.3 Phase 2: Shopping — Cart, Checkout, Orders (Weeks 7–10)

```
[ ] Cart module: add, update, remove items
[ ] Guest cart (session-based) → merge on login
[ ] Wishlist module
[ ] Coupon management (admin) + apply/remove in cart
[ ] Checkout flow: cart → order creation
[ ] Order entity with order number generation
[ ] Razorpay payment integration
[ ] Payment webhook processing
[ ] Order confirmation email
[ ] Order history (customer)
[ ] Order tracking (status updates)
[ ] Order management (admin): view, update status
[ ] Inventory deduction on order completion
[ ] Inventory management (admin)
```

### 18.4 Phase 3: Content + Polish (Weeks 11–14)

```
[ ] CMS pages (admin CRUD + public API)
[ ] Blog posts (admin CRUD + public API)
[ ] Banner management (admin + public API)
[ ] SEO metadata management
[ ] Product reviews (submit + approve flow)
[ ] Related products (same category, tag-based)
[ ] Recently viewed (client-side with optional API)
[ ] Product comparison (client-side)
[ ] Contact form
[ ] Notifications system (order updates, welcome email)
[ ] Customer account: profile, addresses, order history
[ ] Saved addresses for checkout
[ ] Homepage API (aggregated data endpoint)
```

### 18.5 Phase 4: Admin Dashboard + Analytics (Weeks 15–18)

```
[ ] Admin dashboard API (KPIs, recent orders, low stock)
[ ] Sales reports (by date range, by category, by product)
[ ] Customer analytics
[ ] Audit logs viewer
[ ] Roles & permissions management
[ ] Stripe payment integration (second gateway)
[ ] Advanced coupons (category-specific, product-specific)
[ ] Promotions / sale pricing
[ ] Email templates (order confirmation, shipping, etc.)
[ ] Performance optimization pass
[ ] Load testing (k6 or bombardier)
[ ] Security audit
[ ] Production deployment setup (Terraform / Azure CLI)
```

### 18.6 Phase 5: Future Enhancements (Months 6–12)

```
[ ] Google OAuth login
[ ] OpenSearch for advanced search (faceted, autocomplete, typo tolerance)
[ ] Recommendation engine (related / "customers also bought")
[ ] AI chatbot integration
[ ] Mobile app API optimizations (BFF pattern)
[ ] ERP integration adapter
[ ] CRM integration adapter
[ ] Warehouse/shipping provider integration
[ ] Multiple payment gateway selector
[ ] Push notifications
[ ] SMS notifications (OTP for high-value orders)
[ ] A/B testing infrastructure
[ ] Multi-language support (i18n)
[ ] Multi-currency support
```

---

## 19. Risks & Mitigation

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| 1 | **Single developer bottleneck** | High | High | Clean Architecture enables focused work on one module at a time. Comprehensive tests reduce regression fear. CI/CD automates deployment. Prioritize MVP over features. |
| 2 | **Scope creep** | High | Medium | Strict phase-based roadmap. MVP first, then iterate. Say "no" to features not in current phase. Track progress with task.md. |
| 3 | **Payment integration complexity** | Medium | High | Use hosted checkout (Razorpay/Stripe client SDKs). Never handle raw card data. Thorough webhook testing with Razorpay/Stripe test modes. Idempotency keys for all payment operations. |
| 4 | **Database performance at scale** | Low | High | PostgreSQL handles 100K/day easily with proper indexing. Redis cache eliminates 80%+ of reads. Materialized views for aggregates. Read replicas for reporting. Monitor slow queries from day 1. |
| 5 | **Security breach** | Low | Critical | OWASP Top 10 coverage from day 1. Dependabot for CVE alerts. Never store secrets in code. Payment tokenization. Rate limiting. Regular security audits. |
| 6 | **Image storage costs** | Low | Low | Blob Storage is cheap (~₹1.5/GB/month). Image optimization reduces size. CDN caching reduces bandwidth. Lazy deletion of unused images. |
| 7 | **Third-party dependency failure** | Low | Medium | Payment: webhook retries + manual verification flow. Email: queue with retry. Search: fallback to PG FTS if OpenSearch is down. CDN: origin serves directly if CDN is unavailable. |
| 8 | **Data loss** | Very Low | Critical | Automated daily backups with 35-day retention. Point-in-time restore (5 min RPO). Blob storage geo-redundancy. Terraform for infrastructure reproducibility. |
| 9 | **Technology becoming obsolete** | Very Low | Medium | .NET is Microsoft's flagship platform with predictable LTS releases. PostgreSQL has been reliable for 25+ years. Redis is the industry standard cache. All are safe bets for 10+ years. |
| 10 | **Insufficient testing** | Medium | High | Write tests during development, not after. Domain layer: unit tests for all business logic. Application layer: handler tests with mocked repos. API: integration tests with WebApplicationFactory + Testcontainers (real PostgreSQL + Redis). Aim for 80%+ coverage on domain + application layers. |

---

## 20. Appendix — Architecture Diagrams (Text)

### 20.1 Request Flow (Read — Catalog)

```
Customer → CDN → (cache hit? return) → Load Balancer → API Instance
  → Middleware (logging, correlation ID)
    → Controller (GET /api/v1/products/modern-sofa)
      → MediatR.Send(new GetProductBySlugQuery("modern-sofa"))
        → ValidationBehavior (validate query)
          → CachingBehavior (check Redis → cache hit? return DTO)
            → Handler:
              → IProductRepository.GetBySlug("modern-sofa")
              → EF Core → PostgreSQL (read replica)
              → Map entity → ProductDetailDto
              → Store in Redis cache (TTL: 10 min)
              → Return DTO
          → Response
        → Output caching stores response (TTL: 5 min, tag: "products")
      → JSON response
    → CORS headers, security headers
  → 200 OK (< 200ms cached, < 500ms uncached)
```

### 20.2 Request Flow (Write — Place Order)

```
Customer → Load Balancer → API Instance
  → Middleware (auth, logging, correlation ID)
    → Controller (POST /api/v1/orders)
      → MediatR.Send(new PlaceOrderCommand(cartId, shippingAddr, ...))
        → ValidationBehavior (validate: cart not empty, address valid, ...)
          → TransactionBehavior (begin transaction)
            → Handler:
              → Load cart with items
              → Validate inventory availability
              → Validate coupon (if applied)
              → Calculate totals (subtotal, discount, tax, shipping)
              → Create Order entity (snapshot prices, addresses)
              → Create OrderItems (snapshot product data)
              → Deduct inventory (reservations)
              → Clear cart
              → Raise OrderPlacedEvent
              → Return OrderDto with payment initiation data
            → Commit transaction
          → Event handlers:
            → Send order confirmation email
            → Create notification
            → Log audit entry
      → JSON response (order ID + payment session info)
    → 201 Created
```

### 20.3 Complete System Context Diagram

```
                          ┌──────────────────────┐
                          │    Admin (Browser)    │
                          │                      │
                          │ • Product management │
                          │ • Order management   │
                          │ • Content editing    │
                          │ • Analytics          │
                          └──────────┬───────────┘
                                     │
                          ┌──────────▼───────────┐
                          │   Customer (Browser)  │
                          │                      │
                          │ • Browse catalog     │
                          │ • Search products    │
                          │ • Cart & checkout    │
                          │ • Account management │
                          └──────────┬───────────┘
                                     │
                          ┌──────────▼───────────┐
                          │     Cloudflare CDN    │
                          │  (SSL, WAF, Cache)    │
                          └──────────┬───────────┘
                                     │
              ┌──────────────────────▼──────────────────────┐
              │               GharCraft API                  │
              │          (ASP.NET Core 9 Monolith)           │
              │                                              │
              │  Identity │ Catalog │ Shopping │ Content     │
              │  Module   │ Module  │ Module   │ Module      │
              └─────┬─────────┬─────────┬──────────┬────────┘
                    │         │         │          │
         ┌──────────┘    ┌────┘    ┌────┘     ┌────┘
         │               │        │          │
    ┌────▼────┐    ┌─────▼──┐  ┌──▼───┐  ┌───▼────────┐
    │PostgreSQL│    │ Redis  │  │Email │  │Blob Storage│
    │ Primary  │    │ Cache  │  │(SMTP/│  │  (Images)  │
    │ + Replica│    │        │  │ SG)  │  │            │
    └─────────┘    └────────┘  └──────┘  └────────────┘
                                              │
                                         ┌────▼───┐
                                         │  CDN   │
                                         │(Images)│
                                         └────────┘

    ┌─────────────┐    ┌──────────────┐
    │ Razorpay    │    │   Stripe     │
    │ (Payments)  │    │  (Payments)  │
    └─────────────┘    └──────────────┘
```

---

## Open Questions

> [!IMPORTANT]
> The following decisions need your input before implementation begins:

1. **Cloud provider preference** — Azure is recommended (best .NET integration and Indian data centers for Razorpay), but AWS and GCP are equally viable. Which do you prefer?

2. **Domain name** — Is "GharCraft" the final brand name? This affects database naming, project naming, and deployment configuration.

3. **Target market geography** — India-only initially? This affects:
   - Currency (INR only vs. multi-currency)
   - Payment gateway priority (Razorpay first if India)
   - CDN POP locations
   - GST/tax calculation requirements

4. **Admin panel** — Will you build a custom admin frontend (recommended: React/Next.js), or would you prefer an admin template/framework? This impacts the admin API design.

5. **Email provider** — SendGrid, Mailgun, or Amazon SES? Or simple SMTP initially?

6. **Image processing at upload** — Should the API generate multiple image sizes on upload (requires ImageSharp), or should we rely on CDN-side image transformation (Cloudflare Polish / Imgproxy)?

---

*This document will be updated as decisions are made and implementation progresses.*
