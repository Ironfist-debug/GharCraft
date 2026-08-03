# GharCraft — Software Architecture Document (SAD)

**Version:** 2.0 — *Solo-Developer / Low-Cost / SaaS-Ready Revision*
**Date:** August 4, 2026
**Author:** Principal Software Architect
**Supersedes:** v1.0 (August 3, 2026) — archived as `implementation_plan_v1_archive.md`
**Status:** Approved for Implementation
**Project:** GharCraft Furniture Ecommerce Platform

---

## What Changed in Version 2.0

> [!IMPORTANT]
> v1.0 was an *enterprise-team* architecture. v2.0 is the same architecture **calibrated for one developer working with AI assistance, on a near-zero infrastructure budget, targeting a production launch in weeks — not quarters.**
> Nothing structurally sound was removed. Complexity that costs time or money without buying MVP value was deferred, not deleted.

| # | Area | v1.0 | v2.0 | Reason |
|---|------|------|------|--------|
| 1 | Runtime | .NET 9 / C# 13 / EF Core 9 | **ASP.NET Core 8 LTS / C# 12 / EF Core 8** | LTS branch, maximum library/host/AI-training-data compatibility — **but see the support-window advisory in §3.2** |
| 2 | Architecture | Clean Architecture + Modular Monolith | **Unchanged** | Correct decision; kept verbatim |
| 3 | Application flow | MediatR + CQRS + 5 pipeline behaviors | **Controller → Application Service → Repository → EF Core** | Removes ~40% of boilerplate for one developer; CQRS deferred to Phase 3 |
| 4 | Cache | Redis on Day 1 | **`IMemoryCache` on Day 1**, Redis at multi-instance | Saves ₹1,500/mo and one moving part; single instance doesn't need distributed cache |
| 5 | Projects | 5 projects + Shared + Generic Repo + Specs | **4 projects: Api / Application / Domain / Infrastructure** | Fewer indirections, faster navigation |
| 6 | Authorization | 4 roles + 25 granular permissions | **2 roles: `Admin`, `Customer`** | Permission matrix is a Phase 3 feature; roles are enum-simple now |
| 7 | Object storage | Azure Blob | **Cloudflare R2** | S3-compatible, **zero egress fees** — decisive for an image-heavy furniture catalog |
| 8 | Deployment | Azure App Service + Terraform + AKS path | **Cloudflare Pages + Railway/Render + Neon** | ~₹800/mo vs ~₹7,650/mo; Docker optional; K8s/Terraform moved to future scaling |
| 9 | Logging | Serilog → Seq/ELK + OTel + Prometheus + Grafana | **Serilog → Console + rolling file** | Seq/ELK/Grafana listed as future scaling |
| 10 | Search | PG FTS → OpenSearch | **Unchanged** (OpenSearch stays future-only) | PG FTS is genuinely sufficient to ~500K SKUs |
| 11 | Payments | Razorpay + Stripe | **Razorpay only**; Stripe → Future Enhancements | One gateway to certify, one webhook to harden |
| 12 | Notifications | Email + Push + SMS + Marketing | **Order confirmation + password reset only** | Everything else → Future Enhancements |
| 13 | Frontend | Next.js / MUI (implied) | **React + Vite + TypeScript + Tailwind + TanStack Query + React Router + RHF + Zod** | Fastest solo stack; Tailwind avoids design-system lock-in for white-labelling |
| 14 | SEO | One NFR row | **Dedicated first-class section (§14)** | Furniture is a search-discovery category; SEO is architecture, not decoration |
| 15 | Images | Half a page | **Dedicated section (§15)** — responsive sets, AVIF/WebP, lazy loading, versioning, R2+CDN | Images *are* the product for furniture |
| 16 | SaaS reuse | Not addressed | **New §20 — SaaS Readiness & Multi-Tenant Path** | Same codebase must resell to future clients |
| 17 | Roadmap | 5 phases / 18 weeks | **3 phases, AI-accelerated (§24–25)** | Realistic solo timeline with an AI pair |
| 18 | Cost | Azure estimate only | **New §17 — Cost Optimization Strategy** | Explicit ₹/month budget discipline |
| 19 | ER diagrams, schema, indexes, API design, security, conventions | — | **Preserved and extended** | These were the strongest parts of v1.0 |

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
13. [Frontend Architecture](#13-frontend-architecture)
14. [SEO Architecture](#14-seo-architecture)
15. [Image & Media Architecture](#15-image--media-architecture)
16. [Cloud & Deployment Architecture](#16-cloud--deployment-architecture)
17. [Cost Optimization Strategy](#17-cost-optimization-strategy)
18. [Logging, Monitoring & Observability](#18-logging-monitoring--observability)
19. [Security Plan](#19-security-plan)
20. [SaaS Readiness & Multi-Tenant Path](#20-saas-readiness--multi-tenant-path)
21. [Scalability Roadmap](#21-scalability-roadmap)
22. [DevOps & CI/CD](#22-devops--cicd)
23. [Service Boundaries](#23-service-boundaries)
24. [Development Roadmap — Phased Plan](#24-development-roadmap--phased-plan)
25. [Realistic AI-Assisted Delivery Plan](#25-realistic-ai-assisted-delivery-plan)
26. [Risks & Mitigation](#26-risks--mitigation)
27. [Appendix — Architecture Diagrams (Text)](#27-appendix--architecture-diagrams-text)
28. [Open Questions](#28-open-questions)

---

## 1. Executive Summary

GharCraft is a furniture ecommerce platform inspired by [Crush Outdoor](https://crushoutdoor.com/) for catalog experience and [Furlenco](https://www.furlenco.com/) for ecommerce workflows (cart, checkout, payments, orders, accounts). It is a single-branch, direct-to-consumer operation where an admin manages the entire product catalog, pricing, inventory, and content.

Version 2.0 adds a second, equally binding objective: **the codebase must be re-sellable.** Every architectural decision below is checked against two questions — *"can one developer ship and operate this?"* and *"can this become a white-label SaaS product without a rewrite?"*

### Key Design Principles

| Principle | Rationale |
|-----------|-----------|
| **Read-optimized** | 95%+ traffic is browse/search; order conversion is < 5% |
| **Modular monolith** | Single developer; must be deployable, debuggable, and maintainable by one person |
| **Clean Architecture** | Testable, framework-agnostic domain; easy to swap infrastructure |
| **Boring by default** | Prefer the framework's built-in solution over a library; prefer a library over a service; prefer a service over a cluster |
| **Cost-first** | Every Day-1 dependency must justify its monthly cost. Target MVP infra: **< ₹1,000/month** |
| **SEO & imagery as architecture** | For furniture, organic search and image quality *are* the conversion funnel — not frontend polish |
| **Security-first** | PCI-compliant payment flow (SAQ-A); OWASP Top 10 coverage |
| **Progressive complexity** | Start simple; every deferred component (Redis, CQRS, OpenSearch, multi-tenancy) has a pre-designed insertion point |
| **Configuration over code** | Branding, tax, categories, payment gateways and theme are data — the prerequisite for white-labelling |

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

**Optimize for:** CDN delivery, HTTP response caching, in-process caching (`IMemoryCache`), PostgreSQL full-text search, aggressive image optimization, precomputed aggregates (materialized views).

> [!NOTE]
> **What v1.0 optimized for that v2.0 does not (yet):** Redis, read replicas, distributed tracing. At a single instance serving < 10,000 users/day, `IMemoryCache` + CDN + materialized views produce equivalent user-facing latency at zero marginal cost. §21 defines exactly when each returns.

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

### 2.2 Feature Matrix — Phased

Features are now explicitly tagged by delivery phase, so the matrix doubles as a scope contract.

| Domain | Customer Features | Admin Features | Phase |
|--------|------------------|----------------|-------|
| **Catalog** | Browse, categories, collections, search, filters, product variants, gallery, related products | Product CRUD, category CRUD, collection CRUD, inventory, media library | **P1** |
| **Catalog+** | Recently viewed, compare | Bulk import (CSV) | P2 |
| **Shopping** | Cart, checkout | — | **P1** |
| **Shopping+** | Wishlist, coupons | Coupon management | P2 |
| **Orders** | Order placement, tracking, history | Order management, status updates | **P1** |
| **Payments** | Razorpay (UPI/cards/netbanking) | Payment reconciliation, refunds | **P1** |
| **Payments+** | Stripe, multi-gateway selector | Gateway configuration UI | P3 |
| **Accounts** | Registration, login, profile, addresses | Customer list & detail | **P1** |
| **Content** | CMS pages, banners | CMS editor, banner management, SEO fields | **P1** |
| **Content+** | Blogs | Blog editor | P2 |
| **Reviews** | Submit & read reviews | Review moderation | P2 |
| **Analytics** | — | Dashboard KPIs, sales reports | P2 |
| **Notifications** | Order confirmation email, password reset email | — | **P1** |
| **Notifications+** | Push, SMS, marketing campaigns | Campaign manager | P3 |

### 2.3 Non-Functional Requirements

| Requirement | Target | v2.0 Note |
|-------------|--------|-----------|
| Page load (homepage, LCP) | < 2.0s on 4G, < 1.2s cached | Tightened; image strategy is the primary lever |
| Core Web Vitals | LCP < 2.5s, INP < 200ms, CLS < 0.1 | **New** — directly affects Google ranking |
| API response (catalog) | < 200ms (cached), < 600ms (uncached) | Uncached target relaxed slightly for shared-tier hosting |
| API response (checkout) | < 1s | Unchanged |
| Uptime | 99.5% (MVP) → 99.9% (post-scale) | Realistic for single-instance PaaS |
| Concurrent users | 500 (MVP) → 100,000/day (scale) | Unchanged scaling ceiling |
| Image delivery | CDN, AVIF/WebP with JPEG fallback, responsive `srcset` | **Expanded** — see §15 |
| SEO | SSR/prerender, structured data, sitemap, canonicals | **Elevated to first-class** — see §14 |
| Security | OWASP Top 10, PCI DSS SAQ-A (tokenized payments) | Unchanged |
| Infrastructure cost | **< ₹1,000/month at MVP** | **New hard constraint** — see §17 |
| Time to production | **8–10 weeks solo, AI-assisted** | **New** — see §25 |

---

## 3. Technology Stack Evaluation & Recommendation

### 3.1 Backend Framework Comparison

| Criteria | ASP.NET Core 8 (LTS) | Java Spring Boot | Node.js (NestJS) |
|----------|----------------------|------------------|-------------------|
| **Performance** | ⭐⭐⭐⭐⭐ Exceptional. Kestrel is among the fastest web servers. Benchmark leader on TechEmpower. | ⭐⭐⭐⭐ Very good. JIT + GraalVM excellent, but higher baseline latency than .NET. | ⭐⭐⭐ Good for I/O-bound. Single-threaded event loop caps CPU-bound tasks. |
| **Scalability** | ⭐⭐⭐⭐⭐ Async/await first-class. Excellent horizontal + vertical scaling. | ⭐⭐⭐⭐⭐ Mature scaling with Spring Cloud, reactive stack. | ⭐⭐⭐⭐ Scales horizontally well. CPU-bound bottleneck requires worker threads. |
| **Maintainability** | ⭐⭐⭐⭐⭐ Strong typing (C# 12), compile-time safety, excellent refactoring tooling. | ⭐⭐⭐⭐ Strong typing (Java), but verbose. Annotation-heavy. | ⭐⭐⭐ TypeScript helps, but runtime type issues persist. |
| **Learning curve** | ⭐⭐⭐⭐ Moderate. C# is elegant, .NET CLI is productive. | ⭐⭐⭐ Steep. Annotation complexity, Maven/Gradle build system. | ⭐⭐⭐⭐ Low barrier, but NestJS patterns add learning. |
| **Development speed** | ⭐⭐⭐⭐⭐ Scaffolding, EF Core migrations, hot reload, minimal APIs. | ⭐⭐⭐ Slower. Boilerplate-heavy, longer compile times. | ⭐⭐⭐⭐ Fast prototyping, harder to maintain complex domains. |
| **Memory footprint (matters on ₹0 tiers)** | ⭐⭐⭐⭐ ~50–80MB base — fits Railway/Render free & hobby tiers comfortably | ⭐⭐⭐ ~150–300MB base. JVM overhead often exceeds free-tier RAM. | ⭐⭐⭐⭐⭐ ~30–50MB base. |
| **Low-cost PaaS deployment** | ⭐⭐⭐⭐⭐ Railway/Render/Fly.io all have first-class .NET buildpacks or Dockerfiles | ⭐⭐⭐ Works, but memory cost is real money | ⭐⭐⭐⭐⭐ Universally supported |
| **Ecosystem** | ⭐⭐⭐⭐ NuGet: 400K+ packages. EF Core, Identity, FluentValidation, Serilog. | ⭐⭐⭐⭐⭐ Largest ecosystem. | ⭐⭐⭐⭐⭐ npm largest registry, quality varies. |
| **Security** | ⭐⭐⭐⭐⭐ Built-in Identity, AntiForgery, CORS, data protection. Hardened by default. | ⭐⭐⭐⭐⭐ Spring Security is the gold standard, but complex. | ⭐⭐⭐ Passport.js requires careful manual configuration. |
| **AI-assisted development suitability** | ⭐⭐⭐⭐⭐ Strongly typed + compiler feedback loop catches AI-generated errors immediately; huge, stable training corpus for .NET 6/7/8 | ⭐⭐⭐⭐ Similar type-safety benefit, more verbose output to review | ⭐⭐⭐ Dynamic errors surface at runtime, not compile time |
| **Single developer suitability** | ⭐⭐⭐⭐⭐ One person can own the entire stack. | ⭐⭐⭐ Enterprise-grade but overhead is high for one person. | ⭐⭐⭐⭐ Fast for small features, harder at scale. |
| **Long-term maintenance** | ⭐⭐⭐⭐⭐ Predictable LTS cadence. **.NET 8 LTS ends Nov 2026; .NET 10 LTS runs to Nov 2028 — see the advisory in §3.2.** | ⭐⭐⭐⭐⭐ Battle-tested 20+ years. | ⭐⭐⭐ npm churn, frequent breaking changes. |

### 3.2 Verdict & Recommendation

> [!IMPORTANT]
> **Recommended Technology: ASP.NET Core 8 (LTS) with C# 12 and EF Core 8**

**Justification:**

1. **LTS over latest** — v1.0 specified .NET 9, a Standard Term Support release (18 months). For a solo developer who will *not* have time for forced runtime upgrades mid-launch, an **LTS branch** is the correct choice: every hosting provider supports it, every NuGet package is stable against it, and the largest body of public code and documentation targets it.

> [!WARNING]
> **Support-window advisory — read before starting.**
> .NET 8's LTS support window ends **10 November 2026 — approximately three months from this document's date.** After that it receives no security patches.
> This is *not* a reason to abandon .NET 8 for the build, but it changes the plan in one specific way:
> - **Build on .NET 8** as specified. It is battle-tested, universally hosted, and has the deepest AI/documentation corpus — all of which matter for an 8-week AI-assisted sprint.
> - **Schedule the move to .NET 10 LTS (supported to November 2028) into Phase 2, as a non-negotiable item.** Do not let it drift.
> - The migration is genuinely small: bump `<TargetFramework>` to `net10.0`, update the Microsoft/EF Core package versions, run the test suite, redeploy. Budget **one day**, and do it on a branch with the integration tests green before merging.
> - **If you have not yet written a line of code, targeting `net10.0` directly is the better choice** — the architecture in this document is unchanged either way (nothing here depends on a .NET 8-only API), C# 12 code compiles unmodified on .NET 10, and you skip the migration entirely. Choose .NET 8 only if a specific hosting or library constraint forces it.
> Everything else in this document — layering, patterns, project structure, deployment, cost model — is version-independent.

2. **Maximum AI-assist reliability** — .NET 8 has the deepest, most stable body of public code and documentation. AI-generated code for .NET 8 needs materially less correction than for a bleeding-edge release, which matters when AI is your pair programmer.

3. **Performance leadership** — Kestrel consistently tops TechEmpower benchmarks. For a read-heavy catalog, raw throughput matters. .NET 8 output caching delivers sub-millisecond cached responses.

4. **Single developer productivity** — C# 12 is the most productive strongly-typed language for one person:
   - EF Core 8 handles migrations, queries, and relationships with minimal boilerplate
   - Built-in ASP.NET Core Identity scaffolds authentication in minutes
   - Primary constructors and collection expressions (C# 12) cut service-class boilerplate significantly
   - `dotnet new`, `dotnet ef`, and hot reload accelerate iteration

5. **Clean Architecture fit** — .NET's DI container is built into the framework. Clean Architecture needs no third-party library at all in v2.0 — just interfaces and constructor injection.

6. **Cloud economics** — .NET 8 containers are ~80–110MB, start in under a second, and idle at ~60MB RAM. This is what makes a ₹400–800/month hosting bill achievable.

7. **Security hardening** — ASP.NET Core is secure by default: anti-forgery, CORS policies, data protection APIs, Identity, and built-in rate limiting cover ~90% of security concerns out of the box.

### 3.3 Final Technology Stack

| Layer | Technology | Version | Day 1? |
|-------|-----------|---------|--------|
| **Runtime** | .NET | **8 (LTS)** | ✅ |
| **Framework** | ASP.NET Core | **8** | ✅ |
| **Language** | C# | **12** | ✅ |
| **ORM** | Entity Framework Core | **8** (Npgsql provider 8.x) | ✅ |
| **Database** | PostgreSQL (Neon or Railway) | 16+ | ✅ |
| **Cache** | **`IMemoryCache`** + ASP.NET Core Output Caching | built-in | ✅ |
| **Cache (later)** | Redis / `IDistributedCache` | 7+ | ⏳ multi-instance |
| **Search** | PostgreSQL Full-Text Search (`tsvector`) | built-in | ✅ |
| **Search (later)** | OpenSearch | — | ⏳ Phase 3 |
| **Object Storage** | **Cloudflare R2** (S3-compatible, `AWSSDK.S3`) | — | ✅ |
| **CDN / DNS / WAF** | **Cloudflare** (free tier) | — | ✅ |
| **Image Processing** | ImageSharp (upload-time variants) + Cloudflare optimization | 3.x | ✅ |
| **Authentication** | ASP.NET Core Identity + JWT + refresh tokens | built-in | ✅ |
| **Validation** | FluentValidation | 11.x | ✅ |
| **Mapping** | Manual mapping / extension methods (no AutoMapper) | — | ✅ |
| **Application pattern** | **Application Services** (no MediatR) | — | ✅ |
| **CQRS** | MediatR | 12+ | ⏳ Phase 3, optional |
| **Logging** | **Serilog → Console + rolling file** | 8.x | ✅ |
| **Logging (later)** | Seq / ELK / OpenTelemetry | — | ⏳ Phase 3 |
| **Background Jobs** | `IHostedService` / `BackgroundService` | built-in | ✅ |
| **Background Jobs (later)** | Hangfire | — | ⏳ Phase 2+ |
| **Payments** | **Razorpay** (.NET SDK) | — | ✅ |
| **Payments (later)** | Stripe | — | ⏳ Phase 3 |
| **Email** | Resend / Brevo / SMTP via `MailKit` | — | ✅ |
| **API docs** | Swashbuckle (Swagger/OpenAPI) | 6.x | ✅ |
| **Frontend** | React 18 + Vite + TypeScript | — | ✅ |
| **Frontend styling** | Tailwind CSS | 3.x | ✅ |
| **Frontend data** | TanStack Query v5 | — | ✅ |
| **Frontend routing** | React Router v6 | — | ✅ |
| **Frontend forms** | React Hook Form + Zod | — | ✅ |
| **Frontend hosting** | Cloudflare Pages | — | ✅ |
| **Backend hosting** | Railway (or Render) | — | ✅ |
| **Containerization** | Docker (**optional**, for local parity & portability) | — | ⚪ optional |
| **CI/CD** | GitHub Actions | — | ✅ |
| **IaC** | Terraform | — | ⏳ future scaling only |
| **Orchestration** | Kubernetes | — | ⏳ future scaling only |

> [!NOTE]
> **Deliberately excluded from Day 1:** MediatR, AutoMapper, Redis, Hangfire, Terraform, Kubernetes, OpenSearch, Stripe, MUI, Prometheus/Grafana, Seq. Each has a defined re-entry point later in this document. This is not minimalism for its own sake — every removed dependency is a thing that can break at 2 a.m. while you are the only person on call.

---

## 4. Architecture Overview

### 4.1 High-Level Architecture (C4 Level 1 — System Context)

```
                    ┌──────────────┐
                    │   Customer   │
                    │   (Browser)  │
                    └──────┬───────┘
                           │ HTTPS
                    ┌──────▼────────────────────┐
                    │      Cloudflare           │
                    │  DNS • CDN • WAF • SSL    │
                    │  Image optimization       │
                    └───┬───────────────────┬───┘
                        │                   │
          ┌─────────────▼──────┐   ┌────────▼──────────┐
          │ Cloudflare Pages   │   │  Railway / Render │
          │ React SPA + SSR/   │   │  GharCraft.Api    │
          │ prerendered SEO    │──▶│ (ASP.NET Core 8)  │
          │ routes             │   │                   │
          └────────────────────┘   │ ┌───────────────┐ │
                                   │ │ Customer API  │ │
                                   │ │ Admin API     │ │
                                   │ │ Webhook API   │ │
                                   │ │ IMemoryCache  │ │
                                   │ │ BackgroundSvc │ │
                                   │ └───────────────┘ │
                                   └──┬─────────────┬──┘
                                      │             │
                        ┌─────────────▼──┐   ┌──────▼────────────┐
                        │  PostgreSQL    │   │  Cloudflare R2    │
                        │  (Neon /       │   │  (Images, media)  │
                        │   Railway)     │   │  S3-compatible    │
                        │  + FTS + MVs   │   │  Zero egress fees │
                        └────────────────┘   └───────┬───────────┘
                                                     │
                                             ┌───────▼────────┐
                                             │ Cloudflare CDN │
                                             │ (image edge)   │
                                             └────────────────┘

                        ┌────────────────┐   ┌───────────────────┐
                        │   Razorpay     │   │  Email provider   │
                        │   (Payments)   │   │  (Resend/Brevo)   │
                        └────────────────┘   └───────────────────┘
```

**What disappeared from the v1.0 diagram, and why it doesn't hurt:**

| Removed | Replaced by | Impact |
|---------|-------------|--------|
| Dedicated load balancer | Railway/Render built-in ingress + Cloudflare | None at 1 instance; LB is free when you add instances |
| Redis node | `IMemoryCache` in-process | Faster (no network hop) at 1 instance; see §12 |
| Read replica | Materialized views + caching | Read load at MVP scale is ~2% of a burstable Postgres |
| Hangfire server | `BackgroundService` hosted in the API | Same process, zero infra, adequate for MV refresh + cleanup |

### 4.2 Architecture Style: Modular Monolith

Modules are **folder-and-namespace boundaries inside single projects**, not separate assemblies. This preserves the conceptual separation of v1.0 while removing project-reference overhead.

```
┌──────────────────────────────────────────────────────────────┐
│                      GharCraft API                           │
│              (single deployable, ASP.NET Core 8)             │
│                                                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐    │
│  │ Catalog  │ │ Shopping │ │ Identity │ │   Content    │    │
│  │ Module   │ │ Module   │ │ Module   │ │   Module     │    │
│  │          │ │          │ │          │ │              │    │
│  │•Products │ │•Cart     │ │•Auth     │ │ •CMS Pages   │    │
│  │•Category │ │•Checkout │ │•Users    │ │ •Blogs (P2)  │    │
│  │•Search   │ │•Orders   │ │•2 Roles  │ │ •Banners     │    │
│  │•Reviews  │ │•Payments │ │•JWT +    │ │ •SEO fields  │    │
│  │  (P2)    │ │•Coupons  │ │ Refresh  │ │ •Media (R2)  │    │
│  │•Inventory│ │  (P2)    │ │          │ │              │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘    │
│                                                              │
│  ┌──────────────────┐  ┌────────────────────────────────┐    │
│  │  Notifications   │  │      Platform / Settings       │    │
│  │  Module          │  │      Module  (NEW in v2.0)     │    │
│  │                  │  │                                 │    │
│  │  •Order email    │  │  •Brand config (SaaS-ready)    │    │
│  │  •Reset email    │  │  •Theme tokens                 │    │
│  │  •Push  (P3)     │  │  •Tax rules                    │    │
│  │  •SMS   (P3)     │  │  •Payment gateway config       │    │
│  └──────────────────┘  │  •Feature flags                │    │
│                        └────────────────────────────────┘    │
│                                                              │
│  ┌──────────────────┐                                        │
│  │    Analytics     │  (P2 — dashboard, sales reports,       │
│  │    Module        │        audit log viewer)               │
│  └──────────────────┘                                        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │        Domain/Common  (no separate Shared project)   │    │
│  │  •BaseEntity  •AuditableEntity  •Result<T>           │    │
│  │  •PagedResult<T>  •Money  •Address  •Slug            │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

> [!NOTE]
> The modular monolith approach means each module has its own domain entities, application services, and infrastructure implementations — but they share a single database and deployment unit. This gives microservices-level separation of concerns without the operational complexity. When traffic demands it, any module can be extracted into a standalone service (§23.3).

> [!TIP]
> **v2.0 addition — the Platform/Settings module.** This is the single most important structural change for SaaS reuse. Anything that would differ between GharCraft and a future client (logo, palette, currency, tax %, enabled gateways, enabled features, homepage layout) lives here as **data**, never as a hardcoded constant. See §20.

---

## 5. Backend Architecture — Clean Architecture

### 5.1 Layer Dependency Flow

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │         Presentation Layer  (Api)       │    │
│  │  Controllers • Middleware • Filters     │    │
│  │  Request/Response DTOs • Swagger        │    │
│  └───────────────┬─────────────────────────┘    │
│                  │ depends on                    │
│  ┌───────────────▼─────────────────────────┐    │
│  │      Application Layer  (Application)   │    │
│  │  Application Services (use cases)       │    │
│  │  DTOs • FluentValidation validators     │    │
│  │  Service interfaces (ports)             │    │
│  └───────────────┬─────────────────────────┘    │
│                  │ depends on                    │
│  ┌───────────────▼─────────────────────────┐    │
│  │          Domain Layer   (Domain)        │    │
│  │  Entities • Value Objects • Enums       │    │
│  │  Repository interfaces • Result<T>      │    │
│  │  Domain rules & invariants              │    │
│  │       ** ZERO DEPENDENCIES **           │    │
│  └───────────────▲─────────────────────────┘    │
│                  │ implements                    │
│  ┌───────────────┴─────────────────────────┐    │
│  │    Infrastructure Layer (Infrastructure)│    │
│  │  EF Core DbContext • Repositories       │    │
│  │  R2 storage • Email • Razorpay • JWT    │    │
│  │  MemoryCacheService • BackgroundJobs    │    │
│  └─────────────────────────────────────────┘    │
│                                                 │
└─────────────────────────────────────────────────┘

Dependency Rule: Inner layers NEVER depend on outer layers.
Domain is the innermost layer with ZERO external dependencies.
Composition happens once, in Api/Program.cs.
```

### 5.2 The v2.0 Request Pipeline — No MediatR

> [!IMPORTANT]
> **v1.0 mandated MediatR + CQRS + 5 pipeline behaviors. v2.0 replaces this with a direct, explicit call chain.**

```
┌────────────┐    ┌──────────────────┐    ┌──────────────┐    ┌──────────┐
│ Controller │───▶│   Application    │───▶│  Repository  │───▶│ EF Core  │
│            │    │     Service      │    │  (interface  │    │    +     │
│ • Bind     │    │                  │    │   in Domain) │    │ Postgres │
│ • Validate │    │ • Orchestrate    │    │              │    │          │
│   (filter) │    │ • Authorize      │    │ • Query      │    └──────────┘
│ • Map to   │    │ • Business rules │    │ • Persist    │
│   HTTP     │    │ • Cache          │    │              │
│            │    │ • Transaction    │    └──────────────┘
│            │    │ • Return Result<T>│
└────────────┘    └──────────────────┘
        ▲                   │
        │                   ├──▶ ICacheService      (IMemoryCache)
        │                   ├──▶ IFileStorageService (Cloudflare R2)
        │                   ├──▶ IPaymentGateway     (Razorpay)
        │                   ├──▶ IEmailService       (Resend/SMTP)
        │                   └──▶ ISearchService      (PG FTS)
        │
   Global exception middleware → ProblemDetails (RFC 7807)
```

**Concretely:**

```csharp
// Api/Controllers/V1/ProductsController.cs
[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet("{slug}")]
    [OutputCache(PolicyName = "CatalogRead")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await products.GetBySlugAsync(slug, ct);
        return result.ToActionResult();          // Result<T> → 200 / 404 / 400
    }
}

// Application/Catalog/Services/ProductService.cs
public class ProductService(
    IProductRepository repo,
    ICacheService cache) : IProductService
{
    public async Task<Result<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.Product(slug),
            TimeSpan.FromMinutes(10),
            async () =>
            {
                var product = await repo.GetBySlugWithDetailsAsync(slug, ct);
                return product is null
                    ? Result<ProductDetailDto>.NotFound($"Product '{slug}' not found.")
                    : Result<ProductDetailDto>.Success(product.ToDetailDto());
            });
    }
}

// Infrastructure/Persistence/Repositories/ProductRepository.cs
public class ProductRepository(GharCraftDbContext db) : IProductRepository
{
    public Task<Product?> GetBySlugWithDetailsAsync(string slug, CancellationToken ct) =>
        db.Products
          .AsNoTracking()
          .Include(p => p.Images.OrderBy(i => i.SortOrder))
          .Include(p => p.Variants.Where(v => v.IsActive))
          .Include(p => p.Category)
          .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted, ct);
}
```

**Why this is the right call for GharCraft:**

| Concern | MediatR/CQRS answer (v1.0) | v2.0 answer |
|---------|---------------------------|-------------|
| Cross-cutting validation | `ValidationBehavior` pipeline | `[ApiController]` auto-400 + FluentValidation filter registered once |
| Cross-cutting logging | `LoggingBehavior` | Serilog request-logging middleware (one line in `Program.cs`) |
| Cross-cutting caching | `CachingBehavior` | Explicit `cache.GetOrCreateAsync` in the service — *visible where it happens* |
| Transactions | `TransactionBehavior` | Explicit `await using var tx = await db.BeginTransactionAsync()` in the 3 services that need it |
| Testability | Handler unit tests | Service unit tests — identical, one fewer indirection |
| Navigability | Ctrl-click lands on `IRequest`, not the handler | Ctrl-click lands on the implementation |
| Files per feature | 3 (Command, Handler, Validator) × 2 for query | 1 service method + 1 validator |

> [!NOTE]
> **When to introduce CQRS with MediatR (deferred, not rejected):**
> Add MediatR when **any two** become true — (a) more than one developer works on the codebase; (b) read and write models for the same aggregate genuinely diverge (e.g. a denormalized catalog projection); (c) you need uniform cross-cutting behavior across 50+ use cases; (d) you begin extracting a module into its own service.
> The migration is mechanical: an application service method becomes a handler, its parameters become a command/query record. Because controllers already depend on an *interface*, nothing above the Application layer changes. Budget ~2 days for the whole codebase.

### 5.3 Layer Responsibilities

#### Domain Layer
- **Entities**: `Product`, `ProductVariant`, `Category`, `Collection`, `Order`, `OrderItem`, `Cart`, `Payment`, `User`, `Coupon`, `CmsPage`, `Banner`, `MediaFile`, `SiteSetting`
- **Value Objects**: `Money`, `Address`, `Slug`, `SKU`, `EmailAddress`, `PhoneNumber`
- **Enums**: `OrderStatus`, `PaymentStatus`, `ProductStatus`, `UserRole` *(`Admin`, `Customer`)*, `CouponType`, `BannerPosition`
- **Repository interfaces**: `IProductRepository`, `ICategoryRepository`, `IOrderRepository`, `ICartRepository`, `IUserRepository`, `IContentRepository`, `ISettingsRepository`
- **Domain services** (only where logic spans entities): `PricingCalculator`, `CouponValidator`, `TaxCalculator`
- **Common**: `BaseEntity`, `AuditableEntity`, `ISoftDeletable`, `Result<T>`, `Error`
- **No dependencies** on any framework, ORM, or external library.

> **Explicitly not in v2.0 Domain:** generic repository, specification pattern, domain events. Each is justified below.

| Removed pattern | Why it was in v1.0 | Why it's out of v2.0 | When to add back |
|-----------------|--------------------|-----------------------|------------------|
| `IGenericRepository<T>` | Reduce repetition across ~12 repositories | EF Core's `DbSet<T>` *is* the generic repository. Wrapping it adds a layer that leaks `IQueryable` anyway. Concrete repositories with intention-revealing method names (`GetBySlugWithDetailsAsync`) are clearer and easier for AI to generate correctly. | Never, realistically — prefer concrete repositories |
| Specification pattern | Composable query predicates | For ~15 query shapes, hand-written repository methods are shorter, faster to read, and produce better SQL. Specifications shine at 100+ query permutations. | If admin gains a dynamic query builder |
| Domain events | Decouple side effects (email on order placed) | With two side effects total (order email, inventory deduction) an explicit call in `OrderService.PlaceOrderAsync` is clearer and debuggable in one stack frame. | Phase 3, alongside MediatR notifications — insertion point already exists in `OrderService` |

#### Application Layer
- **Application Services** — one per aggregate/use-case cluster, e.g. `ProductService`, `CategoryService`, `CartService`, `OrderService`, `PaymentService`, `AuthService`, `ContentService`, `MediaService`, `AdminReportService`, `SettingsService`
- **DTOs** — request and response models; domain entities are *never* returned from a controller
- **Validators** — FluentValidation class per request DTO
- **Port interfaces** — `ICacheService`, `ICurrentUser`, `IEmailService`, `IFileStorageService`, `ISearchService`, `ITokenService`, `IPaymentGateway`
- **Mapping** — hand-written `ToDto()` extension methods in `Application/<Module>/Mapping/`. No AutoMapper: mapping bugs become compile errors instead of runtime surprises, and AI generates explicit mappers reliably.

#### Infrastructure Layer
- **Persistence**: `GharCraftDbContext`, entity configurations (Fluent API, one file per entity), migrations, `AuditableEntityInterceptor`, `SoftDeleteInterceptor`, `DataSeeder`
- **Repositories**: concrete implementations of the Domain interfaces
- **Services**: `MemoryCacheService`, `R2FileStorageService`, `EmailService`, `TokenService`, `CurrentUser`, `PostgresSearchService`, `ImageProcessingService`
- **Payments**: `RazorpayGateway` + `RazorpayWebhookProcessor` behind `IPaymentGateway`
- **BackgroundJobs**: `MaterializedViewRefreshJob`, `AbandonedCartCleanupJob`, `OrphanedMediaCleanupJob` — all `BackgroundService`

#### Presentation Layer (Api)
- **Controllers**: thin; call one application service method and translate `Result<T>` to HTTP
- **Middleware**: exception handling → ProblemDetails, correlation ID, Serilog request logging, security headers
- **Filters**: FluentValidation filter, `[Authorize(Roles = "Admin")]`
- **API versioning**: URL-based (`/api/v1/...`)
- **Swagger/OpenAPI**: auto-generated docs with JWT auth support
- **SEO endpoints**: `sitemap.xml`, `robots.txt`, structured-data payloads (see §14)

### 5.4 Cross-Cutting Concerns

| Concern | v2.0 Implementation |
|---------|--------------------|
| **Dependency Injection** | .NET built-in container; one `AddApplication()` / `AddInfrastructure()` extension method per layer, called from `Program.cs` |
| **Validation** | FluentValidation registered as an MVC filter; failures return RFC 7807 with a per-field `errors` dictionary |
| **Logging** | Serilog: console (dev) + rolling file (prod), structured, correlation ID enriched, PII redacted — see §18 |
| **Exception Handling** | `IExceptionHandler` (built into .NET 8) maps domain errors → status codes; ProblemDetails RFC 7807 |
| **Caching** | `ICacheService` over `IMemoryCache`, plus ASP.NET Core **Output Caching** with tag-based eviction — see §12 |
| **Rate Limiting** | ASP.NET Core built-in rate limiter (`AddRateLimiter`); fixed window for public APIs, stricter sliding window for auth endpoints |
| **Transactions** | Explicit `IDbContextTransaction` in the three multi-write services (order placement, payment confirmation, refund). Everything else is a single `SaveChangesAsync` (already atomic) |
| **Audit Logging** | EF Core `SaveChangesInterceptor` auto-populates `CreatedAt/By`, `ModifiedAt/By`; admin mutations additionally write an `AuditLog` row |
| **Authorization** | `[Authorize]`, `[Authorize(Roles = "Admin")]`, plus resource ownership checks inside services (a customer may only read their own orders) |
| **Result handling** | `Result<T>` return type from every service method; controllers use a single `ToActionResult()` extension. No exceptions for control flow |

---

## 6. Project Structure

Four projects. Every folder below has a reason to exist.

```
GharCraft/
│
├── backend/
│   ├── src/
│   │   ├── GharCraft.Domain/                      # Layer 1 — no dependencies
│   │   │   ├── Common/
│   │   │   │   ├── BaseEntity.cs
│   │   │   │   ├── AuditableEntity.cs
│   │   │   │   ├── ISoftDeletable.cs
│   │   │   │   ├── Result.cs                      # Result<T>, Error, ErrorType
│   │   │   │   └── PagedResult.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── Money.cs
│   │   │   │   ├── Address.cs
│   │   │   │   ├── Slug.cs
│   │   │   │   └── SKU.cs
│   │   │   ├── Enums/
│   │   │   │   ├── OrderStatus.cs
│   │   │   │   ├── PaymentStatus.cs
│   │   │   │   ├── ProductStatus.cs
│   │   │   │   ├── UserRole.cs                    # Admin, Customer  (only two)
│   │   │   │   ├── CouponType.cs
│   │   │   │   └── BannerPosition.cs
│   │   │   ├── Entities/
│   │   │   │   ├── Catalog/
│   │   │   │   │   ├── Product.cs
│   │   │   │   │   ├── ProductVariant.cs
│   │   │   │   │   ├── ProductImage.cs
│   │   │   │   │   ├── Category.cs
│   │   │   │   │   ├── Collection.cs
│   │   │   │   │   ├── ProductReview.cs           # P2
│   │   │   │   │   └── InventoryRecord.cs
│   │   │   │   ├── Shopping/
│   │   │   │   │   ├── Cart.cs
│   │   │   │   │   ├── CartItem.cs
│   │   │   │   │   ├── Wishlist.cs                # P2
│   │   │   │   │   ├── WishlistItem.cs            # P2
│   │   │   │   │   ├── Order.cs
│   │   │   │   │   ├── OrderItem.cs
│   │   │   │   │   ├── Payment.cs
│   │   │   │   │   └── Coupon.cs                  # P2
│   │   │   │   ├── Identity/
│   │   │   │   │   ├── ApplicationUser.cs         # extends IdentityUser<Guid>
│   │   │   │   │   ├── RefreshToken.cs
│   │   │   │   │   └── UserAddress.cs
│   │   │   │   ├── Content/
│   │   │   │   │   ├── CmsPage.cs
│   │   │   │   │   ├── BlogPost.cs                # P2
│   │   │   │   │   ├── Banner.cs
│   │   │   │   │   └── MediaFile.cs
│   │   │   │   ├── Platform/                      # NEW in v2.0 — SaaS readiness
│   │   │   │   │   ├── SiteSetting.cs             # key/value + jsonb
│   │   │   │   │   ├── BrandProfile.cs            # name, logo, palette, fonts
│   │   │   │   │   ├── TaxRule.cs
│   │   │   │   │   └── PaymentGatewayConfig.cs
│   │   │   │   └── System/
│   │   │   │       ├── AuditLog.cs
│   │   │   │       └── ContactSubmission.cs
│   │   │   ├── Services/                          # pure domain logic
│   │   │   │   ├── PricingCalculator.cs
│   │   │   │   ├── CouponValidator.cs
│   │   │   │   └── TaxCalculator.cs
│   │   │   └── Interfaces/
│   │   │       ├── IProductRepository.cs
│   │   │       ├── ICategoryRepository.cs
│   │   │       ├── IOrderRepository.cs
│   │   │       ├── ICartRepository.cs
│   │   │       ├── IUserRepository.cs
│   │   │       ├── IContentRepository.cs
│   │   │       ├── ISettingsRepository.cs
│   │   │       └── IUnitOfWork.cs
│   │   │
│   │   ├── GharCraft.Application/                 # Layer 2 — depends on Domain
│   │   │   ├── Common/
│   │   │   │   ├── Interfaces/
│   │   │   │   │   ├── ICacheService.cs
│   │   │   │   │   ├── ICurrentUser.cs
│   │   │   │   │   ├── IEmailService.cs
│   │   │   │   │   ├── IFileStorageService.cs
│   │   │   │   │   ├── IImageProcessor.cs
│   │   │   │   │   ├── ISearchService.cs
│   │   │   │   │   ├── ITokenService.cs
│   │   │   │   │   └── IPaymentGateway.cs
│   │   │   │   ├── Constants/
│   │   │   │   │   ├── CacheKeys.cs
│   │   │   │   │   ├── CacheTags.cs
│   │   │   │   │   └── Roles.cs                   # "Admin", "Customer"
│   │   │   │   └── Extensions/
│   │   │   │       ├── QueryableExtensions.cs     # .Paginate(), .ApplySort()
│   │   │   │       └── StringExtensions.cs        # .ToSlug()
│   │   │   ├── Catalog/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── ProductService.cs
│   │   │   │   │   ├── CategoryService.cs
│   │   │   │   │   ├── CollectionService.cs
│   │   │   │   │   ├── SearchService.cs
│   │   │   │   │   └── InventoryService.cs
│   │   │   │   ├── Dtos/
│   │   │   │   │   ├── ProductListItemDto.cs
│   │   │   │   │   ├── ProductDetailDto.cs
│   │   │   │   │   ├── CreateProductRequest.cs
│   │   │   │   │   ├── UpdateProductRequest.cs
│   │   │   │   │   └── CategoryTreeDto.cs
│   │   │   │   ├── Validators/
│   │   │   │   │   └── CreateProductRequestValidator.cs
│   │   │   │   └── Mapping/
│   │   │   │       └── CatalogMappings.cs         # ToDto() extensions
│   │   │   ├── Shopping/
│   │   │   │   ├── Services/  { CartService, OrderService, PaymentService, CouponService }
│   │   │   │   ├── Dtos/  Validators/  Mapping/
│   │   │   ├── Identity/
│   │   │   │   ├── Services/  { AuthService, AccountService }
│   │   │   │   ├── Dtos/  Validators/  Mapping/
│   │   │   ├── Content/
│   │   │   │   ├── Services/  { ContentService, MediaService, SeoService }
│   │   │   │   ├── Dtos/  Validators/  Mapping/
│   │   │   ├── Platform/                          # NEW in v2.0
│   │   │   │   ├── Services/  { SettingsService, BrandService, FeatureFlagService }
│   │   │   │   └── Dtos/  { StorefrontConfigDto, BrandProfileDto }
│   │   │   ├── Notifications/
│   │   │   │   └── Services/  { NotificationService }   # order email, reset email
│   │   │   ├── Admin/
│   │   │   │   └── Services/  { DashboardService, ReportService }   # P2
│   │   │   └── DependencyInjection.cs             # AddApplication()
│   │   │
│   │   ├── GharCraft.Infrastructure/              # Layer 3 — implements Domain/Application ports
│   │   │   ├── Persistence/
│   │   │   │   ├── GharCraftDbContext.cs
│   │   │   │   ├── Configurations/                # one per entity (Fluent API)
│   │   │   │   ├── Repositories/                  # concrete only — no GenericRepository
│   │   │   │   │   ├── ProductRepository.cs
│   │   │   │   │   ├── CategoryRepository.cs
│   │   │   │   │   ├── OrderRepository.cs
│   │   │   │   │   ├── CartRepository.cs
│   │   │   │   │   ├── UserRepository.cs
│   │   │   │   │   ├── ContentRepository.cs
│   │   │   │   │   └── SettingsRepository.cs
│   │   │   │   ├── Interceptors/
│   │   │   │   │   ├── AuditableEntityInterceptor.cs
│   │   │   │   │   └── SoftDeleteInterceptor.cs
│   │   │   │   ├── Migrations/
│   │   │   │   └── Seed/
│   │   │   │       ├── DataSeeder.cs
│   │   │   │       └── SeedData/                  # categories, demo products, admin user
│   │   │   ├── Caching/
│   │   │   │   └── MemoryCacheService.cs          # swap point → RedisCacheService
│   │   │   ├── Storage/
│   │   │   │   ├── R2FileStorageService.cs        # Cloudflare R2 via AWSSDK.S3
│   │   │   │   └── ImageProcessingService.cs      # ImageSharp variant generation
│   │   │   ├── Identity/
│   │   │   │   ├── TokenService.cs                # JWT + refresh token rotation
│   │   │   │   └── CurrentUser.cs
│   │   │   ├── Email/
│   │   │   │   ├── EmailService.cs                # Resend / Brevo / SMTP
│   │   │   │   └── Templates/                     # order-confirmation.html, password-reset.html
│   │   │   ├── Payments/
│   │   │   │   ├── RazorpayGateway.cs             # implements IPaymentGateway
│   │   │   │   └── RazorpayWebhookProcessor.cs
│   │   │   ├── Search/
│   │   │   │   └── PostgresSearchService.cs       # swap point → OpenSearchService
│   │   │   ├── BackgroundJobs/
│   │   │   │   ├── MaterializedViewRefreshJob.cs
│   │   │   │   ├── AbandonedCartCleanupJob.cs
│   │   │   │   └── OrphanedMediaCleanupJob.cs
│   │   │   └── DependencyInjection.cs             # AddInfrastructure()
│   │   │
│   │   └── GharCraft.Api/                         # Layer 4 — composition root
│   │       ├── Controllers/
│   │       │   ├── V1/
│   │       │   │   ├── ProductsController.cs
│   │       │   │   ├── CategoriesController.cs
│   │       │   │   ├── CollectionsController.cs
│   │       │   │   ├── SearchController.cs
│   │       │   │   ├── CartController.cs
│   │       │   │   ├── OrdersController.cs
│   │       │   │   ├── PaymentsController.cs
│   │       │   │   ├── WebhooksController.cs
│   │       │   │   ├── AuthController.cs
│   │       │   │   ├── AccountController.cs
│   │       │   │   ├── ContentController.cs
│   │       │   │   ├── StorefrontController.cs    # NEW — brand/theme/config for SPA
│   │       │   │   ├── SeoController.cs           # NEW — sitemap.xml, robots.txt
│   │       │   │   └── ContactController.cs
│   │       │   └── Admin/
│   │       │       ├── AdminProductsController.cs
│   │       │       ├── AdminCategoriesController.cs
│   │       │       ├── AdminCollectionsController.cs
│   │       │       ├── AdminInventoryController.cs
│   │       │       ├── AdminOrdersController.cs
│   │       │       ├── AdminCustomersController.cs
│   │       │       ├── AdminContentController.cs
│   │       │       ├── AdminMediaController.cs
│   │       │       ├── AdminSettingsController.cs # NEW — brand, tax, gateways, flags
│   │       │       ├── AdminDashboardController.cs
│   │       │       └── AdminReportsController.cs
│   │       ├── Middleware/
│   │       │   ├── GlobalExceptionHandler.cs      # IExceptionHandler (.NET 8)
│   │       │   ├── CorrelationIdMiddleware.cs
│   │       │   └── SecurityHeadersMiddleware.cs
│   │       ├── Filters/
│   │       │   └── ValidationFilter.cs
│   │       ├── Extensions/
│   │       │   ├── ServiceCollectionExtensions.cs # auth, cors, swagger, ratelimit, outputcache
│   │       │   ├── ResultExtensions.cs            # Result<T> → IActionResult
│   │       │   └── WebApplicationExtensions.cs    # migrate + seed on startup
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       └── Dockerfile                         # optional
│   │
│   ├── tests/
│   │   ├── GharCraft.UnitTests/                   # Domain + Application services (mocked repos)
│   │   │   ├── Domain/       { PricingCalculatorTests, CouponValidatorTests, TaxCalculatorTests }
│   │   │   └── Application/  { ProductServiceTests, CartServiceTests, OrderServiceTests }
│   │   └── GharCraft.IntegrationTests/            # WebApplicationFactory + Testcontainers(Postgres)
│   │       ├── Catalog/  Shopping/  Identity/
│   │       └── TestFixtures/
│   │
│   ├── GharCraft.sln
│   ├── Directory.Build.props                      # nullable, warnings-as-errors, langversion 12
│   └── .editorconfig
│
├── frontend/                                       # React + Vite + TS (see §13)
│   ├── src/
│   │   ├── app/            # router, providers, layout shells
│   │   ├── features/       # catalog, cart, checkout, account, admin, content
│   │   ├── components/     # ui primitives, Image, SeoHead
│   │   ├── lib/            # apiClient, queryKeys, formatters, theme
│   │   ├── hooks/
│   │   └── types/          # generated from OpenAPI
│   ├── public/
│   ├── index.html
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── package.json
│
├── docker-compose.yml                              # OPTIONAL: postgres for local dev
│
├── docs/
│   ├── implementation_plan.md                      # this document
│   ├── implementation_plan_v1_archive.md
│   ├── api-conventions.md
│   ├── coding-standards.md
│   └── deployment.md
│
├── .github/
│   └── workflows/
│       ├── backend-ci.yml                          # build + test + vulnerability scan
│       ├── backend-deploy.yml                      # → Railway/Render on main
│       └── frontend-deploy.yml                     # → Cloudflare Pages on main
│
├── .gitignore
└── README.md
```

### 6.1 Structural Decisions

| Decision | Rationale |
|----------|-----------|
| **No `GharCraft.Shared` project** | Its contents (constants, extensions, helpers) belong either to `Domain/Common` (business meaning) or `Application/Common` (application meaning). A "Shared" project with no owner becomes a dumping ground. |
| **Modules are folders, not projects** | `Application/Catalog/`, `Application/Shopping/` etc. give the same boundary clarity as separate assemblies, with zero project-reference friction. The compiler doesn't enforce module isolation — a code review checklist and folder discipline do. |
| **Feature-first inside modules** | Services, DTOs, validators and mappings for one module sit together. Adding a feature touches one folder. |
| **Backend and frontend in one repo** | Single developer, single PR, atomic API+UI changes. Type generation from OpenAPI keeps them in sync. |
| **Tests split by speed, not by layer** | `UnitTests` runs in < 5s on every save; `IntegrationTests` runs in CI and before deploy. v1.0's four test projects fragmented an already small test surface. |
| **`Directory.Build.props`** | `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>12.0</LangVersion>` applied once to every project — the compiler becomes the first reviewer of AI-generated code. |

---

## 7. Database Architecture

### 7.1 Database Selection

> [!IMPORTANT]
> **Recommended: PostgreSQL 16+ — hosted on Neon (serverless) or Railway PostgreSQL**

| Criteria | PostgreSQL | SQL Server | MySQL |
|----------|-----------|------------|-------|
| **Cost** | ⭐⭐⭐⭐⭐ Free, open source; generous free tiers on Neon/Railway/Supabase | ⭐⭐ Expensive licensing (or Express with limits) | ⭐⭐⭐⭐⭐ Free, open source |
| **JSON support** | ⭐⭐⭐⭐⭐ `jsonb` is best-in-class; indexable, queryable | ⭐⭐⭐ JSON support exists but less mature | ⭐⭐⭐ Functional but limited indexing |
| **Full-text search** | ⭐⭐⭐⭐ `tsvector/tsquery` — excellent for MVP; eliminates Elasticsearch early on | ⭐⭐⭐ Full-text exists but less flexible | ⭐⭐⭐ Basic |
| **EF Core 8 support** | ⭐⭐⭐⭐⭐ Npgsql provider is mature and feature-rich | ⭐⭐⭐⭐⭐ First-class Microsoft support | ⭐⭐⭐⭐ Pomelo provider is good |
| **Low-cost cloud availability** | ⭐⭐⭐⭐⭐ **Neon free tier + scale-to-zero; Railway ~$5/mo** | ⭐⭐ Cheapest managed SQL Server is ~$15–30/mo | ⭐⭐⭐⭐ PlanetScale/Railway |
| **Advanced features** | ⭐⭐⭐⭐⭐ CTEs, window functions, materialized views, LISTEN/NOTIFY, partitioning | ⭐⭐⭐⭐ Good but proprietary | ⭐⭐⭐ Fewer |
| **Performance (read-heavy)** | ⭐⭐⭐⭐⭐ Excellent with proper indexing and connection pooling | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐⭐⭐ Good |
| **Furniture catalog fit** | ⭐⭐⭐⭐⭐ `jsonb` for specs, FTS for search, `text[]` for tags | ⭐⭐⭐⭐ Less flexible for dynamic attributes | ⭐⭐⭐ Less suitable |
| **SaaS/multi-tenant path** | ⭐⭐⭐⭐⭐ Row-Level Security + schema-per-tenant both first-class | ⭐⭐⭐⭐ RLS available | ⭐⭐ No RLS |

**Why PostgreSQL wins for GharCraft:**

1. **Free at MVP, cheap at scale** — Neon's free tier covers the entire MVP; Railway PostgreSQL is ~₹400/month for production-grade.
2. **`jsonb` for product attributes** — Furniture has highly variable attributes (dimensions, materials, colours, weight capacity, fabric options). `jsonb` avoids EAV anti-patterns while staying queryable and GIN-indexable.
3. **Built-in full-text search** — Removes the need for OpenSearch at MVP scale. `tsvector` handles product search for up to ~500K products with sub-100ms responses.
4. **Materialized views** — Pre-compute category counts, bestsellers, and rating aggregates for instant homepage loads without a cache server.
5. **Read replicas when needed** — Native streaming replication; a config change, not a redesign.
6. **Row-Level Security** — the cleanest future path to multi-tenancy (§20), available without changing databases.

### 7.2 Hosting Choice — Neon vs. Railway PostgreSQL

| | **Neon** | **Railway PostgreSQL** |
|---|---|---|
| Free tier | Yes — 0.5 GB storage, scale-to-zero | Trial credit, then usage-based |
| Cold start | ~500ms after idle (mitigate with a keep-warm ping) | None — always on |
| Branching | ✅ Database branching per git branch (excellent for testing migrations) | ❌ |
| Backups | PITR on paid tiers | Automated snapshots |
| Best for | **Development, staging, cost-sensitive launch** | **Production, if API is already on Railway (same-network latency)** |

> [!TIP]
> **Recommended combination:** Neon for development/staging (free, branchable) and Railway PostgreSQL for production co-located with the API — this eliminates cross-provider network latency on every query, which matters far more than the ₹400/month.

### 7.3 Supplementary Data Stores

| Store | Use Case | When to Introduce |
|-------|----------|-------------------|
| **`IMemoryCache` (in-process)** | Catalog DTOs, category tree, homepage payload, storefront config, rate-limit counters | **Day 1** — zero cost, zero latency, zero ops |
| **ASP.NET Core Output Cache** | Whole-response caching for public GET endpoints with tag invalidation | **Day 1** — built in |
| **PostgreSQL Full-Text Search** | Product, blog, CMS search | **Day 1** — built into PostgreSQL, no extra infra |
| **Cloudflare R2** | Product images, media library, CMS assets | **Day 1** — never store images in the database |
| **Cloudflare CDN** | Static assets, product images, cached API responses | **Day 1** — free tier is sufficient |
| **Redis** | Distributed cache, distributed rate limiting, shared session state | ⏳ **Only when ≥ 2 API instances run** — see §12.5 |
| **OpenSearch / Elasticsearch** | Faceted search, autocomplete, typo tolerance, synonyms | ⏳ **Phase 3** (> 50K products, or when PG FTS relevance is measurably insufficient) |

---

## 8. Database Schema Design

### 8.1 Entity Relationship Diagram (Text)

> Unchanged from v1.0 except: the `Permissions` table is removed (two fixed roles now), and a new **PLATFORM** block is added for SaaS readiness.

```
┌──────────────────────────────────────────────────────────────────┐
│                        IDENTITY                                  │
│                                                                  │
│  ┌────────────┐  1    M  ┌──────────────┐                       │
│  │   Users    │─────────▶│ UserAddresses│                       │
│  │ (AspNet    │          └──────────────┘                       │
│  │  Identity) │                                                  │
│  │            │  M    M  ┌──────────────┐                       │
│  │ •Id (GUID) │─────────▶│  Roles       │                       │
│  │ •Email     │          │              │                       │
│  │ •PasswordH │          │ ONLY TWO:    │                       │
│  │ •FirstName │          │  • Admin     │                       │
│  │ •LastName  │          │  • Customer  │                       │
│  │ •Phone     │          └──────────────┘                       │
│  │ •IsActive  │  1    M  ┌──────────────┐                       │
│  │ •AvatarUrl │─────────▶│RefreshTokens │  (hashed, rotating)   │
│  └────────────┘          └──────────────┘                       │
│                                                                  │
│   ✂ v1.0 Permissions table REMOVED — see §9.2                    │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         CATALOG                                  │
│                                                                  │
│  ┌────────────────┐  1  M  ┌─────────────────┐                  │
│  │   Categories   │───────▶│    Products     │                  │
│  │                │        │                 │                  │
│  │ •Id            │        │ •Id (GUID v7)   │                  │
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
│  │                │        │ •Specifications │                  │
│  │ •Id            │        │   (jsonb)       │                  │
│  │ •Name          │        │ •SeoMeta (jsonb)│                  │
│  │ •Slug          │        │ •AvgRating      │                  │
│  │ •Description   │        │ •ReviewCount    │                  │
│  │ •ImageUrl      │        │ •SearchVector   │                  │
│  │ •IsActive      │        │   (tsvector)    │                  │
│  │ •SortOrder     │        └───┬──┬──┬───────┘                  │
│  └────────────────┘            │  │  │                          │
│       1  M  ┌──────────────────┘  │  └─────────────────┐       │
│       ┌─────▼──────────┐  ┌──────▼───────┐  ┌──────────▼──┐   │
│       │ProductVariants │  │ProductImages │  │ProductReview│   │
│       │                │  │              │  │   (Phase 2) │   │
│       │ •Id            │  │ •Id          │  │             │   │
│       │ •ProductId(FK) │  │ •ProductId   │  │ •Id         │   │
│       │ •SKU (unique)  │  │ •StorageKey  │  │ •ProductId  │   │
│       │ •Name          │  │ •AltText     │  │ •UserId     │   │
│       │ •Price         │  │ •Width       │  │ •Rating 1-5 │   │
│       │ •SalePrice     │  │ •Height      │  │ •Title      │   │
│       │ •Attributes    │  │ •BlurHash    │  │ •Comment    │   │
│       │  (jsonb)       │  │ •Version     │  │ •IsApproved │   │
│       │ •StockQuantity │  │ •SortOrder   │  └─────────────┘   │
│       │ •IsActive      │  │ •IsPrimary   │                    │
│       └────────────────┘  └──────────────┘                    │
│                                                                │
│                           ┌────────────────┐                   │
│                           │InventoryRecord │                   │
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
│  │   Wishlists  │───────▶│WishlistItem│      (Phase 2)          │
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
│  │ •TaxAmount       │        │ •ImageUrl    │                   │
│  │ •ShippingAmount  │        │  (snapshot)  │                   │
│  │ •TotalAmount     │        │ •Quantity    │                   │
│  │ •CouponCode      │        │ •UnitPrice   │                   │
│  │ •ShippingAddress │        │ •TotalPrice  │                   │
│  │  (jsonb)         │        └──────────────┘                   │
│  │ •BillingAddress  │  1  M  ┌──────────────┐                   │
│  │  (jsonb)         │───────▶│  Payments    │                   │
│  │ •Notes           │        │              │                   │
│  │ •PlacedAt        │        │ •Id          │                   │
│  │ •CompletedAt     │        │ •OrderId(FK) │                   │
│  └──────────────────┘        │ •Gateway     │  ("Razorpay")     │
│                              │ •GatewayTxnId│                   │
│                              │ •Amount      │                   │
│                              │ •Currency    │                   │
│                              │ •Status      │                   │
│                              │ •IdempotencyK│                   │
│                              │ •RawResponse │                   │
│                              │  (jsonb)     │                   │
│                              │ •PaidAt      │                   │
│                              └──────────────┘                   │
│                                                                  │
│  ┌──────────────────┐                                            │
│  │  Coupons  (P2)   │                                            │
│  │ •Id              │                                            │
│  │ •Code (unique)   │                                            │
│  │ •Type (enum)     │  (Percentage / FixedAmount / FreeShip)     │
│  │ •Value           │                                            │
│  │ •MinOrderAmount  │                                            │
│  │ •MaxDiscount     │                                            │
│  │ •UsageLimit      │                                            │
│  │ •UsedCount       │                                            │
│  │ •ValidFrom/To    │                                            │
│  │ •IsActive        │                                            │
│  │ •ApplicableCats  │  (uuid[] — nullable)                       │
│  │ •ApplicableProds │  (uuid[] — nullable)                       │
│  └──────────────────┘                                            │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         CONTENT                                  │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  CmsPages    │  │  BlogPosts   │  │   Banners    │           │
│  │              │  │    (P2)      │  │              │           │
│  │ •Id          │  │ •Id          │  │ •Id          │           │
│  │ •Title       │  │ •Title       │  │ •Title       │           │
│  │ •Slug        │  │ •Slug        │  │ •ImageKey    │           │
│  │ •Content     │  │ •Content     │  │ •MobileKey   │           │
│  │  (HTML/MD)   │  │ •Excerpt     │  │ •LinkUrl     │           │
│  │ •IsPublished │  │ •CoverImage  │  │ •Position    │           │
│  │ •SeoMeta     │  │ •AuthorId    │  │  (enum)      │           │
│  │  (jsonb)     │  │ •Tags(text[])│  │ •SortOrder   │           │
│  │ •SortOrder   │  │ •IsPublished │  │ •IsActive    │           │
│  └──────────────┘  │ •PublishedAt │  │ •StartsAt    │           │
│                    │ •SeoMeta     │  │ •EndsAt      │           │
│  ┌──────────────┐  │  (jsonb)     │  └──────────────┘           │
│  │  MediaFiles  │  └──────────────┘                             │
│  │              │                                               │
│  │ •Id          │  ┌──────────────────────────────────────┐     │
│  │ •FileName    │  │  SeoMetadata (jsonb on parent rows)  │     │
│  │ •OriginalName│  │                                      │     │
│  │ •ContentType │  │  •MetaTitle        •OgTitle          │     │
│  │ •Size        │  │  •MetaDescription  •OgDescription    │     │
│  │ •StorageKey  │  │  •CanonicalUrl     •OgImage          │     │
│  │ •Variants    │  │  •NoIndex          •TwitterCard      │     │
│  │  (jsonb)     │  │  •StructuredData (jsonb, schema.org) │     │
│  │ •AltText     │  └──────────────────────────────────────┘     │
│  │ •Width/Height│                                               │
│  │ •BlurHash    │                                               │
│  │ •Folder      │                                               │
│  │ •UploadedBy  │                                               │
│  └──────────────┘                                               │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                   PLATFORM   (NEW in v2.0 — SaaS readiness)      │
│                                                                  │
│  ┌──────────────────┐  ┌───────────────────┐  ┌──────────────┐  │
│  │  SiteSettings    │  │  BrandProfile     │  │  TaxRules    │  │
│  │                  │  │                   │  │              │  │
│  │ •Id              │  │ •Id               │  │ •Id          │  │
│  │ •Key (unique)    │  │ •SiteName         │  │ •Name        │  │
│  │ •Value (jsonb)   │  │ •Tagline          │  │ •Rate (%)    │  │
│  │ •Group           │  │ •LogoKey          │  │ •AppliesTo   │  │
│  │ •IsPublic        │  │ •FaviconKey       │  │  (Category / │  │
│  │  (exposed to SPA)│  │ •PrimaryColor     │  │   Global)    │  │
│  │ •TenantId ⟵ null │  │ •SecondaryColor   │  │ •CategoryId  │  │
│  │  (future-proof)  │  │ •AccentColor      │  │ •IsInclusive │  │
│  └──────────────────┘  │ •FontHeading      │  │ •IsActive    │  │
│                        │ •FontBody         │  │ •TenantId ⟵  │  │
│  ┌──────────────────┐  │ •SocialLinks(jsb) │  └──────────────┘  │
│  │PaymentGatewayCfg │  │ •ContactInfo(jsb) │                    │
│  │                  │  │ •Currency         │  ┌──────────────┐  │
│  │ •Id              │  │ •CurrencySymbol   │  │ FeatureFlags │  │
│  │ •Provider        │  │ •Locale           │  │              │  │
│  │  ("Razorpay")    │  │ •TenantId ⟵ null  │  │ •Key         │  │
│  │ •IsEnabled       │  └───────────────────┘  │ •IsEnabled   │  │
│  │ •IsDefault       │                         │ •TenantId ⟵  │  │
│  │ •ConfigJson      │   (secrets remain in    └──────────────┘  │
│  │  (non-secret)    │    env vars, never DB)                    │
│  │ •TenantId ⟵ null │                                           │
│  └──────────────────┘                                           │
│                                                                  │
│  ⟵ TenantId columns are defined now, always NULL in v2.0.        │
│    They cost nothing today and make multi-tenancy a migration    │
│    rather than a rewrite. See §20.                               │
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
│  │ •OldValues(json)│  │ •Type (enum)      │  │ •Subject      │  │
│  │ •NewValues(json)│  │ •IsRead           │  │ •Message      │  │
│  │ •UserId         │  │ •ReadAt           │  │ •IsRead       │  │
│  │ •Timestamp      │  │ •CreatedAt        │  │ •RepliedAt    │  │
│  │ •IpAddress      │  └───────────────────┘  │ •CreatedAt    │  │
│  └─────────────────┘                         └───────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **GUIDs (v7) as primary keys** | Safe for distributed systems, no sequential ID guessing, merge-friendly. Use time-ordered UUID v7 so B-tree indexes stay sequential — critical for insert performance. |
| **`jsonb` for product attributes/specs** | Furniture attributes vary wildly (dimensions, materials, weight, fabric options). `jsonb` avoids the EAV anti-pattern while remaining indexable via GIN. |
| **`tsvector` search column** | Auto-populated by trigger on name + short description + tags + description. Full-text search with no external service. |
| **Denormalized order snapshots** | `OrderItem` stores `ProductName`, `SKU`, `UnitPrice`, **and `ImageUrl`** at order time. Product edits never corrupt order history or invoices. |
| **Address as `jsonb` on Order** | Shipping/billing addresses snapshotted at order time; users may edit saved addresses without affecting past orders. |
| **Soft deletes** | Products, categories, users, content use `IsDeleted` + `DeletedAt` with a global EF query filter. Orders, payments and audit logs are never deleted. |
| **`SortOrder` columns** | Admin-controlled display ordering for categories, banners, images, collections. Integer-based for simple drag-and-drop reordering. |
| **Separate `ProductVariant`** | A sofa may come in different fabrics, sizes, or colours. Each variant carries its own SKU, price, and stock. `Product` is the parent aggregate. |
| **`ProductImage.StorageKey` not `Url`** *(new in v2.0)* | Storing the R2 object key rather than a full URL means the CDN domain, image-transform provider, or storage provider can change without a data migration. URLs are composed at read time from `StorageKey` + configured CDN base. |
| **`ProductImage.Version` + `BlurHash`** *(new in v2.0)* | `Version` enables immutable, infinitely-cacheable image URLs on re-upload. `BlurHash` gives a zero-layout-shift placeholder — directly improves CLS. See §15. |
| **`Payment.IdempotencyKey`** *(new in v2.0)* | Guarantees a retried checkout or a duplicated webhook cannot create a second charge. |
| **Nullable `TenantId` columns on Platform tables** *(new in v2.0)* | Zero cost today; converts future multi-tenancy from a rewrite into a backfill + RLS policy. See §20. |
| **No `Permissions` table** *(changed in v2.0)* | Two roles, enforced by `[Authorize(Roles = "Admin")]` and ownership checks. Reintroduce as its own table in Phase 3 if staff roles are needed. |

### 8.3 Indexing Strategy

```sql
-- Performance-critical indexes for a read-heavy workload

-- Product catalog (most queried table)
CREATE INDEX idx_products_category_status ON products (category_id, status) WHERE is_deleted = false;
CREATE INDEX idx_products_slug            ON products (slug) WHERE is_deleted = false;
CREATE INDEX idx_products_featured        ON products (is_featured, sort_order) WHERE status = 'Active' AND is_deleted = false;
CREATE INDEX idx_products_search          ON products USING GIN (search_vector);
CREATE INDEX idx_products_attributes      ON products USING GIN (attributes);
CREATE INDEX idx_products_tags            ON products USING GIN (tags);
CREATE INDEX idx_products_price           ON products (sale_price NULLS LAST, base_price);
CREATE INDEX idx_products_created         ON products (created_at DESC) WHERE is_deleted = false;  -- "New arrivals"

-- Categories
CREATE INDEX idx_categories_slug   ON categories (slug) WHERE is_deleted = false;
CREATE INDEX idx_categories_parent ON categories (parent_id, sort_order);

-- Product variants
CREATE INDEX idx_variants_product ON product_variants (product_id) WHERE is_active = true;
CREATE UNIQUE INDEX idx_variants_sku ON product_variants (sku);

-- Product images  (ordering + primary lookup are on every catalog query)
CREATE INDEX idx_images_product ON product_images (product_id, sort_order);
CREATE INDEX idx_images_primary ON product_images (product_id) WHERE is_primary = true;

-- Orders (write-light, read for history)
CREATE INDEX idx_orders_user   ON orders (user_id, placed_at DESC);
CREATE INDEX idx_orders_status ON orders (status) WHERE status NOT IN ('Completed', 'Cancelled');
CREATE UNIQUE INDEX idx_orders_number ON orders (order_number);

-- Payments (webhook lookups must be fast and unique)
CREATE UNIQUE INDEX idx_payments_gateway_txn ON payments (gateway, gateway_txn_id);
CREATE UNIQUE INDEX idx_payments_idempotency ON payments (idempotency_key) WHERE idempotency_key IS NOT NULL;

-- Cart (session-based, frequently accessed)
CREATE INDEX idx_carts_user    ON carts (user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_carts_session ON carts (session_id) WHERE user_id IS NULL;
CREATE INDEX idx_carts_expiry  ON carts (expires_at);   -- cleanup job

-- Content & SEO
CREATE INDEX idx_cms_slug   ON cms_pages (slug) WHERE is_published = true;
CREATE INDEX idx_blog_slug  ON blog_posts (slug) WHERE is_published = true;
CREATE INDEX idx_blog_pub   ON blog_posts (published_at DESC) WHERE is_published = true;

-- Platform settings (read on every storefront bootstrap)
CREATE UNIQUE INDEX idx_settings_key ON site_settings (key);

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

-- Typo tolerance without OpenSearch: trigram similarity for "did you mean"
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_products_name_trgm ON products USING GIN (name gin_trgm_ops);
```

> [!TIP]
> **`pg_trgm` is the reason OpenSearch stays deferred.** Combining `tsvector` ranking with trigram similarity gives fuzzy matching and "did you mean" suggestions — the two features people usually buy Elasticsearch for — at zero infrastructure cost.

### 8.4 Materialized Views (Pre-computed Aggregates)

Materialized views do the work Redis would otherwise do for aggregate reads, inside the database you are already paying for.

```sql
-- Homepage: category product counts
CREATE MATERIALIZED VIEW mv_category_product_counts AS
SELECT
    c.id, c.name, c.slug, c.image_url,
    COUNT(p.id) AS product_count
FROM categories c
LEFT JOIN products p ON p.category_id = c.id
    AND p.status = 'Active' AND p.is_deleted = false
WHERE c.is_deleted = false AND c.is_active = true
GROUP BY c.id;

CREATE UNIQUE INDEX ON mv_category_product_counts (id);   -- required for CONCURRENTLY

-- Bestsellers (top products by completed-order count)
CREATE MATERIALIZED VIEW mv_bestsellers AS
SELECT
    p.id, p.name, p.slug, p.base_price, p.sale_price,
    (SELECT storage_key FROM product_images pi
      WHERE pi.product_id = p.id AND pi.is_primary = true LIMIT 1) AS image_key,
    COUNT(DISTINCT oi.order_id) AS order_count
FROM products p
JOIN product_variants pv ON pv.product_id = p.id
JOIN order_items oi      ON oi.variant_id = pv.id
JOIN orders o            ON o.id = oi.order_id AND o.status IN ('Completed', 'Delivered')
WHERE p.is_deleted = false AND p.status = 'Active'
GROUP BY p.id
ORDER BY order_count DESC
LIMIT 50;

CREATE UNIQUE INDEX ON mv_bestsellers (id);

-- Refreshed every 15 minutes by MaterializedViewRefreshJob (BackgroundService)
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_category_product_counts;
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_bestsellers;
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
│  { email, password }           { email, password }          │
│         │                              │                    │
│         ▼                              ▼                    │
│  ┌──────────────┐             ┌──────────────┐              │
│  │ Validate     │             │ Validate     │              │
│  │ Credentials  │             │ Credentials  │              │
│  │ (Identity)   │             │ + Role=Admin │              │
│  └──────┬───────┘             └──────┬───────┘              │
│         │                            │                      │
│         ▼                            ▼                      │
│  ┌──────────────────────────────────────┐                   │
│  │       Generate JWT Token Pair        │                   │
│  │                                      │                   │
│  │  Access Token  (15 min expiry)       │                   │
│  │  ┌────────────────────────────┐      │                   │
│  │  │ Header: { alg, typ }       │      │                   │
│  │  │ Payload:                   │      │                   │
│  │  │   sub:   userId (GUID)     │      │                   │
│  │  │   email: user@email.com    │      │                   │
│  │  │   role:  "Customer"|"Admin"│  ◀── ONE role claim.     │
│  │  │   iat, exp, jti            │      No permissions[]    │
│  │  │ Signature: HMAC-SHA256     │      array (v1.0 had it) │
│  │  └────────────────────────────┘      │                   │
│  │                                      │                   │
│  │  Refresh Token (7 days expiry)       │                   │
│  │  ┌────────────────────────────┐      │                   │
│  │  │ Stored in DB (SHA-256)     │      │                   │
│  │  │ One-time use               │      │                   │
│  │  │ Rotated on each refresh    │      │                   │
│  │  │ Revoked-descendant detect  │      │                   │
│  │  └────────────────────────────┘      │                   │
│  └──────────────────────────────────────┘                   │
│                                                             │
│  Token Refresh: POST /api/v1/auth/refresh                   │
│  { refreshToken } → new access + refresh pair               │
│                                                             │
│  Password Reset: POST /api/v1/auth/forgot-password          │
│  { email } → time-limited token emailed (1 of only 2 emails)│
│                                                             │
│  Email Verify: POST /api/v1/auth/verify-email               │
│  { token } → marks email verified   (optional at MVP)       │
│                                                             │
│  Google Login (Phase 3):                                     │
│  GET /api/v1/auth/google → OAuth2 redirect flow             │
└─────────────────────────────────────────────────────────────┘
```

### 9.2 Authorization Model — Simplified

> [!IMPORTANT]
> **v1.0 defined 4 roles and ~25 granular permissions. v2.0 defines two roles.**

```
Roles (exhaustive)
──────────────────

  ┌── Admin      → full access to every /api/v1/admin/* endpoint
  └── Customer   → shop, cart, order, review, manage own account

Enforcement — two mechanisms only:

  1. Attribute-based (coarse):
       [Authorize]                        → any authenticated user
       [Authorize(Roles = Roles.Admin)]   → admin-only controllers

  2. Ownership check (fine, inside the application service):
       if (order.UserId != currentUser.Id && !currentUser.IsAdmin)
           return Result<OrderDto>.Forbidden();

That is the entire authorization model.
```

**Why this is sufficient — and safe:**

- GharCraft is a single-branch business. There is one admin: the owner. A permission matrix models an organisation that does not exist yet.
- The v1.0 permission list (`catalog:products:create`, …) required a `Permissions` table, a `RolePermissions` join table, a custom `[RequirePermission]` attribute, a claims-enrichment step, an admin UI to manage it, and a seeder — roughly a week of work protecting a single user account.
- Security is **not** weakened: every admin route still requires authentication *and* the Admin role, and every customer-scoped resource still enforces ownership.

> [!NOTE]
> **Path to granular permissions (Phase 3, or first SaaS client with staff):**
> 1. Add `Permission` and `RolePermission` tables (schema already sketched in v1.0 — reuse it).
> 2. Add roles beyond the two enum values; `UserRole` becomes a table rather than an enum.
> 3. Add a `[RequirePermission("catalog:products:create")]` authorization handler.
> 4. Enrich JWT with a `permissions` claim array.
> Because controllers are already decorated with `[Authorize(Roles = ...)]`, this is an additive change — no service or repository code moves. Estimated 3–4 days.

### 9.3 Security Token Lifecycle

| Token | Storage | Expiry | Rotation |
|-------|---------|--------|----------|
| Access (JWT) | Client memory / React context — **never `localStorage`** | 15 minutes | On refresh |
| Refresh | HTTP-only, Secure, `SameSite=Strict` cookie + DB (SHA-256 hashed) | 7 days | Every use (rotation); reuse of a rotated token revokes the whole chain |
| Email verification | DB | 24 hours | Single use |
| Password reset | DB | 1 hour | Single use |
| Razorpay webhook secret | Environment variable (Railway/Render secrets) | — | Manual rotation |
| JWT signing key | Environment variable | — | Manual rotation; supports dual-key validation during rollover |

> [!TIP]
> **Secrets management at MVP:** platform environment variables (Railway/Render/Cloudflare Pages) are encrypted at rest and injected at runtime. This replaces v1.0's Azure Key Vault at zero cost. Migrate to a managed vault when you have more than one environment-owner.

---

## 10. Payment Architecture

### 10.1 Payment Gateway Abstraction

Razorpay is the only Day-1 implementation — but the abstraction that made Stripe pluggable in v1.0 is retained, because it is also what makes **per-tenant gateway configuration** possible later (§20).

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
│  │ IPaymentGateway          │  ← Application port            │
│  │                          │                                │
│  │ + CreateOrderAsync(      │                                │
│  │     amount, currency,    │                                │
│  │     idempotencyKey,      │                                │
│  │     metadata)            │                                │
│  │ + VerifySignature(       │                                │
│  │     payload, signature)  │                                │
│  │ + GetPaymentStatusAsync( │                                │
│  │     gatewayTxnId)        │                                │
│  │ + InitiateRefundAsync(   │                                │
│  │     paymentId, amount)   │                                │
│  └──────────────────────────┘                                │
│         ▲                                                     │
│         │ implements                                          │
│  ┌──────┴──────────────────────────────────────────────┐     │
│  │                                                      │     │
│  │  ┌──────────────────┐    ┌──────────────────────┐   │     │
│  │  │ RazorpayGateway  │    │ StripeGateway        │   │     │
│  │  │  ✅ DAY 1        │    │  ⏳ PHASE 3          │   │     │
│  │  │                  │    │  (Future Enhancement)│   │     │
│  │  │ • Razorpay .NET  │    │                      │   │     │
│  │  │   SDK            │    │ • Multi-currency     │   │     │
│  │  │ • INR            │    │ • International      │   │     │
│  │  │ • UPI / cards /  │    │   cards              │   │     │
│  │  │   netbanking /   │    │                      │   │     │
│  │  │   wallets / EMI  │    │                      │   │     │
│  │  └──────────────────┘    └──────────────────────┘   │     │
│  │                                                      │     │
│  │  ┌──────────────────┐                               │     │
│  │  │PaymentGateway    │  ← Resolver (Phase 3)          │     │
│  │  │Resolver          │                               │     │
│  │  │                  │  Day 1: returns Razorpay.      │     │
│  │  │ Resolve(currency)│  Phase 3: reads                │     │
│  │  │   → IGateway     │  PaymentGatewayConfig table    │     │
│  │  └──────────────────┘  (per-tenant, per-currency)    │     │
│  └──────────────────────────────────────────────────────┘     │
│                                                               │
│  Webhook Processing:                                          │
│  ──────────────────                                          │
│  POST /api/v1/webhooks/razorpay  → RazorpayWebhookProcessor  │
│                                                               │
│  Verifies HMAC signature, is idempotent by                    │
│  (gateway, gateway_txn_id), updates Payment + Order,          │
│  deducts inventory, sends confirmation email.                 │
└───────────────────────────────────────────────────────────────┘
```

**Why Razorpay-only for MVP:**

| Reason | Detail |
|--------|--------|
| Market fit | India-first business; Razorpay covers UPI, cards, netbanking, wallets, EMI — Stripe's India card support does not cover UPI, which is the majority payment method for Indian furniture buyers |
| Certification effort | Every gateway needs its own test-mode QA, webhook hardening, refund flow, and reconciliation. One gateway = one surface to get exactly right |
| Cost | Zero setup fee, ~2% per transaction — no monthly minimum |
| Deferral is cheap | `IPaymentGateway` already exists; adding `StripeGateway` later is one class plus one webhook endpoint, roughly 3 days |

### 10.2 Payment Flow

```
Customer                  API                  Razorpay            Webhook
   │                       │                        │                  │
   │ 1. Place Order        │                        │                  │
   │──────────────────────▶│                        │                  │
   │                       │ 2. Create Order (DB)   │                  │
   │                       │    status = Pending    │                  │
   │                       │    + IdempotencyKey    │                  │
   │                       │ 3. Create gateway order│                  │
   │                       │───────────────────────▶│                  │
   │                       │                        │                  │
   │  5. razorpayOrderId   │ 4. gateway order id    │                  │
   │◀──────────────────────│◀───────────────────────│                  │
   │                       │                        │                  │
   │ 6. Razorpay Checkout  │                        │                  │
   │    (client-side SDK,  │                        │                  │
   │     card data never   │                        │                  │
   │     touches our API)  │                        │                  │
   │──────────────────────────────────────────────▶│                  │
   │                       │                        │                  │
   │                       │                        │ 7. webhook       │
   │                       │                        │  payment.captured│
   │                       │◀──────────────────────────────────────────│
   │                       │ 8. Verify HMAC signature                  │
   │                       │ 9. Idempotency check (txn id already seen?)│
   │                       │10. Re-verify amount against Order total    │
   │                       │11. BEGIN TRANSACTION                       │
   │                       │      Payment  → Completed                  │
   │                       │      Order    → Confirmed                  │
   │                       │      Inventory→ deducted                   │
   │                       │      Cart     → cleared                    │
   │                       │    COMMIT                                  │
   │                       │12. Send order confirmation email           │
   │ 13. Poll/redirect →   │    (async, failure does not fail the order)│
   │     order confirmed   │                                            │
   │◀──────────────────────│                                            │
```

> [!IMPORTANT]
> **The webhook is the source of truth, not the client callback.** A customer closing the browser mid-payment must still receive their order. The client-side verify endpoint is a UX accelerator only; order state transitions happen on the webhook path, and both paths converge through the same idempotent handler.

### 10.3 Payment Security

- **Never handle raw card data** — Razorpay Checkout performs client-side tokenization. Our servers never see a PAN.
- **Webhook signature verification** — every webhook verified with HMAC-SHA256 against the Razorpay webhook secret; unverified payloads are logged and dropped with 400.
- **Idempotency** — unique index on `(gateway, gateway_txn_id)` plus `idempotency_key`; a replayed webhook is a no-op.
- **Server-side amount verification** — the backend recomputes subtotal + discount + tax + shipping from the cart and never trusts a client-supplied amount.
- **PCI DSS SAQ-A** — hosted checkout means the simplest compliance level applies.
- **Refunds** — Admin role required, amount-capped to the original payment, fully audit-logged.
- **Webhook endpoint** — excluded from global rate limiting, but IP-allowlisted to Razorpay's published ranges where the host supports it.

---

## 11. API Organization

### 11.1 API Routes

```
Customer APIs (Public + Authenticated)
──────────────────────────────────────

Storefront bootstrap (Public — cached, NEW in v2.0)
  GET    /api/v1/storefront/config           # brand, theme tokens, currency, enabled features

Catalog (Public — heavily cached)
  GET    /api/v1/products                    # List (paginated, filtered, sorted)
  GET    /api/v1/products/{slug}             # Product detail by slug
  GET    /api/v1/products/{id}/related       # Related products
  GET    /api/v1/products/{id}/reviews       # Reviews                        [P2]
  GET    /api/v1/categories                  # Category tree
  GET    /api/v1/categories/{slug}/products  # Products by category
  GET    /api/v1/collections                 # All collections
  GET    /api/v1/collections/{slug}          # Collection detail with products
  GET    /api/v1/search?q=sofa&category=...  # Full-text search with filters
  GET    /api/v1/search/suggest?q=sof        # Autocomplete (pg_trgm)
  GET    /api/v1/homepage                    # Pre-aggregated homepage payload

SEO (Public — NEW in v2.0)
  GET    /sitemap.xml                        # Index sitemap
  GET    /sitemap-products.xml               # Paged product sitemap
  GET    /sitemap-categories.xml
  GET    /sitemap-content.xml
  GET    /robots.txt

Authentication (Public)
  POST   /api/v1/auth/register               # Customer registration
  POST   /api/v1/auth/login                  # Customer login → JWT pair
  POST   /api/v1/auth/admin/login            # Admin login → JWT pair
  POST   /api/v1/auth/refresh                # Rotate tokens
  POST   /api/v1/auth/logout                 # Revoke refresh token
  POST   /api/v1/auth/forgot-password        # Request reset (email #2 of 2)
  POST   /api/v1/auth/reset-password         # Reset with token
  POST   /api/v1/auth/verify-email           # Verify email address

Account (Authenticated — Customer)
  GET    /api/v1/account/profile
  PUT    /api/v1/account/profile
  PUT    /api/v1/account/change-password
  GET    /api/v1/account/addresses
  POST   /api/v1/account/addresses
  PUT    /api/v1/account/addresses/{id}
  DELETE /api/v1/account/addresses/{id}

Shopping (Authenticated or Session-based)
  GET    /api/v1/cart                        # Get current cart
  POST   /api/v1/cart/items                  # Add item
  PUT    /api/v1/cart/items/{id}             # Update quantity
  DELETE /api/v1/cart/items/{id}             # Remove item
  POST   /api/v1/cart/merge                  # Merge guest cart on login
  POST   /api/v1/cart/coupon                 # Apply coupon               [P2]
  DELETE /api/v1/cart/coupon                 # Remove coupon              [P2]

  GET    /api/v1/wishlist                                                 [P2]
  POST   /api/v1/wishlist/{productId}                                     [P2]
  DELETE /api/v1/wishlist/{productId}                                     [P2]

Orders (Authenticated — Customer)
  POST   /api/v1/orders                      # Place order (from cart)
  GET    /api/v1/orders                      # Order history
  GET    /api/v1/orders/{id}                 # Order detail (ownership enforced)
  GET    /api/v1/orders/{id}/track           # Status timeline

Payments (Authenticated)
  POST   /api/v1/payments/initiate           # Create Razorpay order for an order
  POST   /api/v1/payments/verify             # Client-side callback verification

Webhooks (Public — signature verified, excluded from rate limiting)
  POST   /api/v1/webhooks/razorpay

Reviews (Authenticated — Customer)                                        [P2]
  POST   /api/v1/products/{id}/reviews

Content (Public — cached)
  GET    /api/v1/pages/{slug}                # CMS page
  GET    /api/v1/banners?position={position} # Active banners by position
  GET    /api/v1/blog                        # Blog listing               [P2]
  GET    /api/v1/blog/{slug}                 # Blog post detail           [P2]

Contact (Public — rate limited)
  POST   /api/v1/contact

───────────────────────────────────────────────────────────────

Admin APIs  (Authenticated — [Authorize(Roles = "Admin")])
──────────────────────────────────────────────────────────────

Dashboard
  GET    /api/v1/admin/dashboard              # KPIs, recent orders, low stock   [P2]

Products
  GET    /api/v1/admin/products               # List all (incl. draft/archived)
  GET    /api/v1/admin/products/{id}
  POST   /api/v1/admin/products
  PUT    /api/v1/admin/products/{id}
  DELETE /api/v1/admin/products/{id}          # Soft delete
  POST   /api/v1/admin/products/{id}/images   # Upload → R2 + variant generation
  PUT    /api/v1/admin/products/{id}/images/reorder
  DELETE /api/v1/admin/products/{id}/images/{imgId}

Variants
  POST   /api/v1/admin/products/{id}/variants
  PUT    /api/v1/admin/products/{id}/variants/{vid}
  DELETE /api/v1/admin/products/{id}/variants/{vid}

Categories
  GET    /api/v1/admin/categories             # Full tree
  POST   /api/v1/admin/categories
  PUT    /api/v1/admin/categories/{id}
  DELETE /api/v1/admin/categories/{id}
  PUT    /api/v1/admin/categories/reorder

Collections
  CRUD   /api/v1/admin/collections
  POST   /api/v1/admin/collections/{id}/products

Inventory
  GET    /api/v1/admin/inventory
  PUT    /api/v1/admin/inventory/{variantId}
  GET    /api/v1/admin/inventory/low-stock

Orders
  GET    /api/v1/admin/orders
  GET    /api/v1/admin/orders/{id}
  PUT    /api/v1/admin/orders/{id}/status
  POST   /api/v1/admin/orders/{id}/refund

Customers
  GET    /api/v1/admin/customers
  GET    /api/v1/admin/customers/{id}

Content
  CRUD   /api/v1/admin/cms-pages
  CRUD   /api/v1/admin/banners
  CRUD   /api/v1/admin/blog-posts                                          [P2]
  PUT    /api/v1/admin/seo/{entityType}/{id}  # SEO metadata for any entity

Media
  GET    /api/v1/admin/media                  # Media library (paged, filtered)
  POST   /api/v1/admin/media/upload           # Multipart → R2 + variants
  DELETE /api/v1/admin/media/{id}

Settings  (NEW in v2.0 — the SaaS control surface)
  GET    /api/v1/admin/settings/brand         # Brand profile
  PUT    /api/v1/admin/settings/brand         # Logo, colours, fonts, socials
  GET    /api/v1/admin/settings/tax
  PUT    /api/v1/admin/settings/tax           # Tax rules
  GET    /api/v1/admin/settings/payments
  PUT    /api/v1/admin/settings/payments      # Enabled gateways (non-secret config)
  GET    /api/v1/admin/settings/features
  PUT    /api/v1/admin/settings/features      # Feature flags

Coupons                                                                    [P2]
  CRUD   /api/v1/admin/coupons

Reports                                                                    [P2]
  GET    /api/v1/admin/reports/sales
  GET    /api/v1/admin/reports/products
  GET    /api/v1/admin/reports/customers

Audit
  GET    /api/v1/admin/audit-logs

✂ Removed from v1.0: /api/v1/admin/roles/*  (two fixed roles — see §9.2)
✂ Removed from v1.0: /api/v1/webhooks/stripe  (Phase 3)
```

### 11.2 API Conventions

| Convention | Standard |
|-----------|----------|
| **Versioning** | URL prefix: `/api/v1/` |
| **Success response** | `{ "success": true, "data": {...}, "meta": { "page", "pageSize", "totalCount", "totalPages" } }` |
| **Error response** | RFC 7807 ProblemDetails: `{ "type", "title", "status", "detail", "traceId", "errors": { "field": ["msg"] } }` |
| **Pagination** | `?page=1&pageSize=20&sortBy=price&sortOrder=asc` — `pageSize` capped at 100 |
| **Filtering** | `?category=sofas&minPrice=10000&maxPrice=50000&material=wood&inStock=true` |
| **Status codes** | 200, 201, 204, 400, 401, 403, 404, 409, 422, 429, 500 |
| **CORS** | Explicit allowlist: the Cloudflare Pages production domain, preview domains, `localhost:5173` in Development |
| **Content-Type** | `application/json`; `multipart/form-data` for uploads |
| **Rate limits** | Public 100/min per IP · Authenticated 300/min per user · Auth endpoints 10/min per IP · Contact form 5/hour per IP · Webhooks exempt |
| **Caching headers** | Public catalog GETs: `Cache-Control: public, max-age=60, stale-while-revalidate=300`. Authenticated: `no-store` |
| **Correlation** | `X-Correlation-Id` accepted or generated; echoed in every response and log line |
| **Idempotency** | `Idempotency-Key` header honoured on `POST /orders` and `POST /payments/initiate` |
| **Naming** | Resources plural & kebab-case in URLs (`/cms-pages`), camelCase in JSON, PascalCase in C# |
| **Result mapping** | `Result.NotFound → 404`, `Forbidden → 403`, `Validation → 422`, `Conflict → 409`, `Failure → 500` — one shared `ToActionResult()` |

---

## 12. Caching Strategy

### 12.1 Multi-Layer Caching Architecture — No Redis

```
┌──────────────────────────────────────────────────────────────┐
│                    Caching Layers (v2.0)                      │
│                                                              │
│  Layer 1: Cloudflare CDN Edge                                │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Static assets (JS/CSS/fonts): 1 year, immutable      │  │
│  │ • Product images: 1 year, immutable (versioned keys)   │  │
│  │ • Public API GETs: 60s + stale-while-revalidate=300    │  │
│  │ • Prerendered HTML: 300s + SWR                         │  │
│  │ • Cost: ₹0 (free tier)                                 │  │
│  └────────────────────────────────────────────────────────┘  │
│                              ↓ miss                          │
│  Layer 2: ASP.NET Core Output Cache (in-process)             │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Whole HTTP responses for public GET endpoints        │  │
│  │ • Vary by: query string, Accept-Encoding               │  │
│  │ • Tag-based eviction: "products", "categories",        │  │
│  │   "product:{slug}", "content", "storefront"            │  │
│  │ • Built into .NET 8 — zero dependencies                │  │
│  └────────────────────────────────────────────────────────┘  │
│                              ↓ miss                          │
│  Layer 3: Application Cache — IMemoryCache                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • DTO-level caching inside application services        │  │
│  │ • ICacheService facade (swap point for Redis)          │  │
│  │ • Size-limited (e.g. 200 MB) with LRU eviction         │  │
│  │ • Key pattern: "gharcraft:{module}:{entity}:{id|slug}" │  │
│  │ • Nanosecond access — no network hop, no serialization │  │
│  └────────────────────────────────────────────────────────┘  │
│                              ↓ miss                          │
│  Layer 4: Database-Level                                     │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • PostgreSQL shared_buffers                            │  │
│  │ • Materialized views for aggregates (§8.4)             │  │
│  │ • EF Core AsNoTracking + compiled queries on hot paths │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

> [!IMPORTANT]
> **Why `IMemoryCache` beats Redis on Day 1.**
> With one API instance, Redis adds a network round-trip (~1ms), serialization/deserialization cost, an additional failure mode, an additional secret to manage, and ~₹1,500/month — in exchange for a benefit (shared state across instances) that does not exist until there is a second instance. `IMemoryCache` is faster, free, and has zero operational surface.
> **The cost of being wrong is one class.** All caching goes through `ICacheService`; switching to Redis means writing `RedisCacheService : ICacheService` and changing one DI registration.

### 12.2 Cache TTL by Entity

| Cache Target | TTL | Layer | Invalidation |
|-------------|-----|-------|--------------|
| **Storefront config** (brand, theme, flags) | 60 min | Memory + Output | Evict on any settings update |
| **Homepage payload** | 5 min | Memory + Output + CDN | Tag `homepage`; evicted on banner/featured-product change |
| **Category tree** | 30 min | Memory + Output | Tag `categories` |
| **Category product listing** | 5 min | Memory + Output + CDN | Tag `products` |
| **Product detail** | 10 min | Memory + Output + CDN | Tag `product:{slug}` + `products` |
| **Search results** | 2 min | Memory | Time-based only (too many key permutations) |
| **Search suggestions** | 15 min | Memory | Tag `products` |
| **Product reviews** | 5 min | Memory | Tag `product:{slug}` on approval |
| **CMS page** | 30 min | Memory + Output + CDN | Tag `content` |
| **Blog listing / post** | 15 min | Memory + Output + CDN | Tag `content` |
| **Banners by position** | 10 min | Memory + Output | Tag `content` |
| **Sitemaps** | 60 min | Output + CDN | Tag `products` + `content` |
| **User cart** | **Never cached** | — | Always read-through to DB |
| **User profile / orders** | **Never cached** | — | `Cache-Control: no-store` |
| **Admin endpoints** | **Never cached** | — | Always fresh |

### 12.3 Cache Key Schema

```
gharcraft:storefront:config                      → brand + theme + flags
gharcraft:homepage:data                          → aggregated homepage JSON
gharcraft:categories:tree                        → full category tree
gharcraft:categories:{slug}:products:p{page}:{filterHash}
gharcraft:products:{slug}                        → product detail DTO
gharcraft:products:{id}:related                  → related products
gharcraft:products:{id}:reviews:p{page}
gharcraft:collections:{slug}
gharcraft:search:{queryHash}
gharcraft:search:suggest:{prefix}
gharcraft:cms:{slug}
gharcraft:blog:list:p{page}
gharcraft:blog:{slug}
gharcraft:banners:{position}
gharcraft:sitemap:{segment}
```

Keys are produced only by `CacheKeys` static factory methods — never by inline string interpolation. This makes prefix-based invalidation reliable and makes a future tenant prefix (`gharcraft:t{tenantId}:…`) a one-file change.

### 12.4 Cache Invalidation Pattern

```csharp
// Application/Catalog/Services/ProductService.cs
public async Task<Result> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
{
    var product = await repo.GetByIdAsync(id, ct);
    if (product is null) return Result.NotFound("Product not found.");

    var previousSlug = product.Slug;
    product.Apply(request);                       // domain-enforced invariants
    await unitOfWork.SaveChangesAsync(ct);

    // 1. Precise DTO cache eviction
    await cache.RemoveAsync(CacheKeys.Product(previousSlug));
    await cache.RemoveAsync(CacheKeys.Product(product.Slug));
    await cache.RemoveByPrefixAsync(CacheKeys.CategoryProductsPrefix(product.CategoryId));
    await cache.RemoveByPrefixAsync(CacheKeys.SearchPrefix);
    if (product.IsFeatured) await cache.RemoveAsync(CacheKeys.HomepageData);

    // 2. Output cache eviction by tag
    await outputCache.EvictByTagAsync(CacheTags.Products, ct);
    await outputCache.EvictByTagAsync(CacheTags.Product(product.Slug), ct);

    // 3. CDN purge — only for slug/visibility changes that affect indexed URLs
    if (previousSlug != product.Slug || product.Status != ProductStatus.Active)
        await cdnPurge.PurgeAsync([$"/products/{previousSlug}", $"/products/{product.Slug}"], ct);

    return Result.Success();
}
```

### 12.5 When to Introduce Redis

Introduce Redis when **any one** of these becomes true:

| Trigger | Why Redis becomes necessary |
|---------|-----------------------------|
| **A second API instance is deployed** | In-process caches diverge; two instances can serve inconsistent product data after an edit |
| **Rate limiting must be global** | Per-instance counters allow N× the intended request rate |
| **Cache warm-up cost after deploy becomes visible** | An in-process cache is empty on every restart; Redis survives deploys |
| **Server-side sessions or distributed locks are needed** | e.g. inventory reservation across instances |
| **Memory pressure on the API container** | Offloading the cache lets the API run on a smaller instance |

**Migration effort:** add `StackExchange.Redis`, implement `RedisCacheService : ICacheService`, register `AddStackExchangeRedisOutputCache`, set one connection-string env var. **Estimated half a day**, because nothing outside `Infrastructure/Caching/` refers to the cache implementation.

---

## 13. Frontend Architecture

### 13.1 Stack

| Concern | Choice | Why |
|---------|--------|-----|
| **UI library** | **React 18** | Largest ecosystem, best AI code-generation accuracy, hireable |
| **Build tool** | **Vite 5** | Sub-second HMR; production build in seconds; native TS; no webpack config to maintain |
| **Language** | **TypeScript 5 (strict)** | Compile-time contract with the API; catches AI-generated mistakes before runtime |
| **Styling** | **Tailwind CSS 3** | Utility-first — no component-library lock-in, and **theme tokens are CSS variables**, which is precisely what white-labelling needs |
| **Server state** | **TanStack Query v5** | Caching, background refetch, pagination, optimistic cart updates — replaces ~all of Redux for an ecommerce app |
| **Routing** | **React Router v6** | Data routers, nested layouts, code-splitting per route |
| **Forms** | **React Hook Form** | Uncontrolled inputs = fewer re-renders on long checkout/admin forms |
| **Schema/validation** | **Zod** | One schema validates the form *and* types the API response; mirrors FluentValidation rules on the client |
| **Client state** | React Context + `useReducer` (or Zustand if it grows) | Only cart-drawer/UI state remains local; server state belongs to TanStack Query |
| **Icons** | lucide-react | Tree-shakeable, consistent |
| **Charts (admin, P2)** | Recharts | Simple, React-native API |

> [!NOTE]
> **Material UI is deliberately excluded.** MUI ships a large runtime, imposes an opinionated visual language, and makes per-client rebranding a theme-override exercise fighting the framework. Tailwind + a small set of hand-rolled primitives (`Button`, `Input`, `Modal`, `Drawer`, `Card`) driven by CSS custom properties gives full design freedom, a smaller bundle, and the cleanest white-label story. Add `shadcn/ui` (which is Tailwind + Radix, copied into your repo — not a dependency) if you want accessible primitives without lock-in.

### 13.2 Application Structure

```
frontend/src/
├── app/
│   ├── router.tsx                  # route tree, lazy boundaries
│   ├── providers.tsx               # QueryClient, Theme, Auth, Cart
│   └── layouts/
│       ├── StorefrontLayout.tsx    # header, nav, footer, cart drawer
│       └── AdminLayout.tsx         # sidebar, topbar, guards
├── features/
│   ├── catalog/    { pages, components, api, hooks }   # listing, PDP, search
│   ├── cart/       { CartDrawer, useCart, api }
│   ├── checkout/   { CheckoutPage, AddressStep, PaymentStep, Razorpay }
│   ├── account/    { Profile, Addresses, Orders, OrderDetail }
│   ├── content/    { CmsPage, BlogList, BlogPost }
│   └── admin/      { products, categories, orders, media, settings, dashboard }
├── components/
│   ├── ui/         # Button, Input, Select, Modal, Drawer, Badge, Skeleton
│   ├── Image.tsx   # responsive srcset + lazy + blurhash (see §15)
│   ├── SeoHead.tsx # meta, OG, Twitter, canonical, JSON-LD (see §14)
│   └── Money.tsx   # currency formatting from storefront config
├── lib/
│   ├── apiClient.ts     # fetch wrapper: auth, refresh-on-401, error normalization
│   ├── queryKeys.ts     # centralized TanStack Query key factory
│   ├── theme.ts         # applies brand CSS variables from /storefront/config
│   └── seo.ts           # schema.org builders
├── hooks/           { useAuth, useStorefrontConfig, useDebounce, useMediaQuery }
└── types/
    └── api.ts           # GENERATED from backend OpenAPI — never hand-edited
```

### 13.3 Frontend ↔ Backend Contract

```
backend Swagger JSON  ──▶  openapi-typescript  ──▶  src/types/api.ts
                                                        │
                                        typed apiClient + TanStack Query hooks
```

A `npm run gen:api` script regenerates types from the running backend's `/swagger/v1/swagger.json`. Renaming a backend DTO field breaks the frontend build immediately rather than silently at runtime — this is the single highest-value safeguard when generating code with AI on both sides of the wire.

### 13.4 Rendering Strategy

| Route class | Rendering | Reason |
|-------------|-----------|--------|
| Home, category, product detail, CMS, blog | **Prerendered / SSR** | Must be crawlable and fast — see §14.2 |
| Search results | CSR with SSR shell | Query-dependent; `noindex` on filtered permutations |
| Cart, checkout, account, admin | **CSR only** | Authenticated, never indexed, no SEO value |

### 13.5 Performance Budget

| Metric | Budget |
|--------|--------|
| Initial JS (gzipped, storefront route) | < 150 KB |
| Route-level code splitting | Every top-level route lazy-loaded |
| Admin bundle | Fully separate chunk — never shipped to shoppers |
| LCP image | Preloaded, `fetchpriority="high"`, never lazy-loaded |
| Third-party scripts | Analytics loaded `defer`; no chat widget at MVP |
| Fonts | Self-hosted `woff2`, `font-display: swap`, max 2 families / 4 weights |

### 13.6 Theming for White-Label (SaaS prerequisite)

```ts
// lib/theme.ts — brand comes from the API, not from source code
export function applyBrand(cfg: StorefrontConfig) {
  const r = document.documentElement.style;
  r.setProperty('--color-primary',   cfg.primaryColor);
  r.setProperty('--color-secondary', cfg.secondaryColor);
  r.setProperty('--color-accent',    cfg.accentColor);
  r.setProperty('--font-heading',    cfg.fontHeading);
  r.setProperty('--font-body',       cfg.fontBody);
}
```

```js
// tailwind.config.ts — utilities resolve to the CSS variables above
colors: {
  primary:   'rgb(var(--color-primary) / <alpha-value>)',
  secondary: 'rgb(var(--color-secondary) / <alpha-value>)',
  accent:    'rgb(var(--color-accent) / <alpha-value>)',
}
```

The consequence: **a new client's storefront is a database row, not a fork.** Logo, palette, typography, currency, and enabled features all arrive from `GET /api/v1/storefront/config`.

---

## 14. SEO Architecture

> [!IMPORTANT]
> For a furniture retailer, organic search is the primary customer acquisition channel — shoppers search *"teak dining table 6 seater"*, not *"GharCraft"*. Paid acquisition for high-consideration, high-ticket furniture is expensive; organic ranking compounds. **SEO is therefore treated as an architectural requirement in v2.0, not a marketing afterthought.**

### 14.1 SEO Responsibility Map

```
┌──────────────────────────────────────────────────────────────────┐
│                       SEO ARCHITECTURE                            │
│                                                                   │
│  BACKEND (source of truth)          FRONTEND (rendering)          │
│  ─────────────────────────          ────────────────────          │
│                                                                   │
│  SeoMetadata (jsonb) on             <SeoHead /> component         │
│    • Product                          • <title>, <meta>           │
│    • Category                         • canonical <link>          │
│    • Collection                       • OpenGraph tags            │
│    • CmsPage                          • Twitter Card tags         │
│    • BlogPost                         • JSON-LD <script>          │
│      ├ MetaTitle                      • hreflang (future i18n)    │
│      ├ MetaDescription                                            │
│      ├ CanonicalUrl                 Prerender / SSR layer         │
│      ├ NoIndex                        • Crawlers receive fully    │
│      ├ OgTitle / OgDescription          rendered HTML             │
│      ├ OgImage (R2 key)               • Not an empty React root   │
│      └ StructuredData (jsonb)                                     │
│                                                                   │
│  SeoController                      Route → URL discipline        │
│    • /sitemap.xml (index)             /products/{slug}            │
│    • /sitemap-products.xml            /categories/{slug}          │
│    • /sitemap-categories.xml          /collections/{slug}         │
│    • /sitemap-content.xml             /blog/{slug}                │
│    • /robots.txt                      /pages/{slug}               │
│      (env-aware: staging = noindex)   — lowercase, hyphenated,    │
│                                          stable, never IDs        │
│                                                                   │
│  Admin SEO editor                   Performance = ranking         │
│    PUT /admin/seo/{type}/{id}         • Core Web Vitals (§13.5)   │
│    Live SERP + OG preview             • Image strategy (§15)      │
└──────────────────────────────────────────────────────────────────┘
```

### 14.2 Rendering for Crawlers — SSR or Prerendering

Google renders JavaScript, but does so on a delayed second-pass queue; Bing, and most social/messaging link unfurlers, largely do not. A pure client-rendered SPA is a measurable ranking handicap for a catalog.

| Option | How | Cost | Verdict |
|--------|-----|------|---------|
| **A. Prerender at build** (`vite-plugin-ssr` / `react-snap`) | Crawl known routes at build time, emit static HTML | ₹0 | ⚠️ Good for static pages; a rebuild per product edit does not scale past a few hundred SKUs |
| **B. Cloudflare Pages Functions SSR** ⭐ | Edge function renders HTML on request, calls the API, caches at edge for 5 min | ₹0 on free tier (100K req/day) | ✅ **Recommended** — real SSR, no server to operate, no Node host to pay for |
| **C. Prerender-only-for-bots middleware** | Detect crawler UA → serve cached rendered HTML | ₹0–low | ✅ Acceptable fallback; risk of cloaking perception if content diverges |
| **D. Migrate to Next.js** | Full framework SSR/ISR | Needs a Node host (~₹400+/mo) and a rewrite | ⏳ Only if SSR needs outgrow option B |

> **Decision:** Ship **Option B — SSR via Cloudflare Pages Functions** for the five indexable route families (home, category, product, collection, content/blog). Authenticated routes stay pure CSR. If SSR proves fiddly during the sprint, fall back to **Option A** for home/category/CMS and **Option C** for products — then revisit. Under no circumstances should product pages ship as an empty `<div id="root">`.

### 14.3 Structured Data (schema.org)

Structured data is what produces rich results — price, stars, and availability directly in Google — which materially lifts click-through for furniture queries.

| Page | Schema types | Key fields |
|------|-------------|------------|
| **Product detail** | `Product` + `Offer` + `AggregateRating` (P2) + `BreadcrumbList` | `name`, `image[]`, `description`, `sku`, `brand`, `offers.price`, `priceCurrency: INR`, `availability`, `itemCondition`, `shippingDetails`, `hasMerchantReturnPolicy` |
| **Category / collection** | `CollectionPage` + `ItemList` + `BreadcrumbList` | ordered `itemListElement` of product URLs |
| **Homepage** | `Organization` + `WebSite` (+ `SearchAction` for sitelinks searchbox) | `name`, `logo`, `url`, `sameAs[]` (socials), `contactPoint` |
| **Blog post** | `BlogPosting` + `BreadcrumbList` | `headline`, `image`, `datePublished`, `dateModified`, `author` |
| **CMS / FAQ / contact** | `WebPage`, `FAQPage`, `LocalBusiness` (address, hours) | as applicable |
| **All pages** | `BreadcrumbList` | full ancestor trail |

```jsonc
// Emitted server-side inside <head> on every product page
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "Aria Teak Dining Table — 6 Seater",
  "image": [
    "https://cdn.gharcraft.com/products/aria-table/v3/1600.webp",
    "https://cdn.gharcraft.com/products/aria-table/v3/1200.webp"
  ],
  "description": "Solid teak 6-seater dining table with a hand-rubbed matte finish.",
  "sku": "GC-DIN-ARIA-6S",
  "brand": { "@type": "Brand", "name": "GharCraft" },
  "offers": {
    "@type": "Offer",
    "url": "https://gharcraft.com/products/aria-teak-dining-table-6-seater",
    "priceCurrency": "INR",
    "price": "58999.00",
    "availability": "https://schema.org/InStock",
    "itemCondition": "https://schema.org/NewCondition",
    "priceValidUntil": "2026-12-31"
  }
}
```

`StructuredData` is generated by `SeoService` from live entity data (never hand-authored), so price and stock in search results can never drift from the database.

### 14.4 Sitemaps

```
/sitemap.xml                 → sitemap index
  ├── /sitemap-static.xml     → home, about, contact, policy pages
  ├── /sitemap-categories.xml → all active categories + collections
  ├── /sitemap-products.xml   → active products, 5,000 URLs per page,
  │                             auto-paginated (?page=2) beyond that
  └── /sitemap-content.xml    → CMS pages + published blog posts
```

- Generated on demand by `SeoController`, output-cached for 60 minutes, tag-invalidated when products or content change.
- `<lastmod>` reflects the entity's `ModifiedAt` — this is what makes Google recrawl a repriced product quickly.
- `<changefreq>`/`<priority>` included but treated as advisory.
- Product image URLs are included via the `image:` sitemap extension — **important for furniture**, since Google Images is a real discovery surface for the category.
- Sitemap index URL is submitted to Google Search Console and Bing Webmaster Tools at launch.

### 14.5 robots.txt

```
# Production
User-agent: *
Allow: /
Disallow: /admin
Disallow: /account
Disallow: /cart
Disallow: /checkout
Disallow: /api/
Disallow: /*?sort=
Disallow: /*?page=
Allow: /*?page=1$

Sitemap: https://gharcraft.com/sitemap.xml

# Staging / preview environments emit instead:
User-agent: *
Disallow: /
```

Environment-aware generation prevents the classic disaster of a staging domain outranking production or triggering duplicate-content penalties.

### 14.6 Canonical URLs & Duplicate Content

Faceted catalog navigation is the number-one source of duplicate content in ecommerce. Rules:

| Situation | Directive |
|-----------|-----------|
| Product reachable via multiple categories | Canonical → `/products/{slug}` (single home) |
| Paginated listings | Each page self-canonical; `rel=prev/next` semantics preserved via internal links |
| Filtered/sorted listings (`?material=teak&sort=price`) | `<meta name="robots" content="noindex,follow">` + canonical → unfiltered category URL |
| Slug changed | 301 redirect from old slug (old slugs retained in a `product_slug_history` table) |
| Trailing slash / case | Single canonical form enforced by a Cloudflare redirect rule |
| `www` vs apex | One canonical host, 301 the other |
| HTTP | Always 301 → HTTPS (HSTS) |
| Out-of-stock product | Stays indexed with `availability: OutOfStock`; discontinued products 301 to their category |

### 14.7 Meta Tags, OpenGraph & Twitter Cards

```html
<title>Aria Teak Dining Table — 6 Seater | GharCraft</title>
<meta name="description" content="Solid teak 6-seater dining table with a hand-rubbed matte finish. Free delivery across India. ₹58,999.">
<link rel="canonical" href="https://gharcraft.com/products/aria-teak-dining-table-6-seater">
<meta name="robots" content="index,follow,max-image-preview:large">

<!-- OpenGraph — WhatsApp/Facebook/LinkedIn shares; furniture is shared a LOT -->
<meta property="og:type"        content="product">
<meta property="og:site_name"   content="GharCraft">
<meta property="og:title"       content="Aria Teak Dining Table — 6 Seater">
<meta property="og:description" content="Solid teak 6-seater dining table, hand-rubbed matte finish.">
<meta property="og:image"       content="https://cdn.gharcraft.com/products/aria-table/v3/og-1200x630.jpg">
<meta property="og:image:width" content="1200">
<meta property="og:image:height" content="630">
<meta property="og:url"         content="https://gharcraft.com/products/aria-teak-dining-table-6-seater">
<meta property="product:price:amount"   content="58999.00">
<meta property="product:price:currency" content="INR">

<!-- Twitter/X -->
<meta name="twitter:card"        content="summary_large_image">
<meta name="twitter:title"       content="Aria Teak Dining Table — 6 Seater">
<meta name="twitter:description" content="Solid teak 6-seater dining table, hand-rubbed matte finish.">
<meta name="twitter:image"       content="https://cdn.gharcraft.com/products/aria-table/v3/og-1200x630.jpg">
```

Defaults are composed by `SeoService` (`{ProductName} | {BrandName}`, description from short description truncated at 155 chars, OG image from the primary product image rendered at 1200×630). Admins can override any field per entity — but a page is never shipped without metadata.

### 14.8 Additional On-Page SEO Requirements

| Requirement | Implementation |
|-------------|----------------|
| **Semantic HTML** | Exactly one `<h1>` per page, ordered heading hierarchy, `<nav>`/`<main>`/`<article>`/`<footer>` landmarks |
| **Image alt text** | `AltText` is a **required field** on media upload; product images default to `"{ProductName} — {view}"` |
| **Internal linking** | Breadcrumbs on every catalog page; related products; category cross-links; blog → product links |
| **URL structure** | `/categories/dining/products/aria-teak-dining-table` — human-readable, keyword-bearing, never `?id=427` |
| **Core Web Vitals** | LCP < 2.5s (image strategy §15), CLS < 0.1 (explicit dimensions + BlurHash), INP < 200ms (code-split routes) |
| **Mobile-first** | Responsive from 320px; Google indexes the mobile rendering |
| **404 handling** | Real 404 status (not a 200 SPA shell) with suggested categories |
| **Analytics** | Google Search Console + a lightweight analytics tool (Cloudflare Web Analytics — free, cookieless, no consent banner needed) |
| **Page speed** | Cloudflare CDN, Brotli, HTTP/3, preconnect to CDN origin |

---

## 15. Image & Media Architecture

> [!IMPORTANT]
> **For furniture, images are the product.** A shopper cannot sit on a sofa through a screen — image quality *is* the product experience, and image weight *is* the bounce rate. A furniture PDP typically carries 6–12 high-resolution photographs; a category page carries 20–40 thumbnails. Unoptimized, a single product page can exceed 15 MB and lose most mobile visitors before it renders. This section is therefore a first-class architectural concern, not a delivery detail.

### 15.1 End-to-End Image Pipeline

```
┌──────────────┐   ┌────────────────────┐   ┌──────────────┐   ┌───────────────┐
│ Admin Upload │──▶│  API: validate +   │──▶│ Cloudflare   │──▶│  Cloudflare   │
│              │   │  process           │   │ R2 Bucket    │   │  CDN Edge     │
│ • 4000×3000  │   │                    │   │              │   │               │
│   JPEG ~8MB  │   │ • MIME + magic-byte│   │ products/    │   │ • Cache 1 yr  │
│              │   │   sniffing         │   │  {slug}/     │   │   immutable   │
│              │   │ • Max 15MB         │   │   v{n}/      │   │ • Brotli      │
│              │   │ • Strip EXIF/GPS   │   │    320.avif  │   │ • HTTP/3      │
│              │   │ • Resize (ImageSharp)   320.webp  │   │ • Auto AVIF/  │
│              │   │ • Encode AVIF+WebP │   │    320.jpg   │   │   WebP negot. │
│              │   │   +JPEG fallback   │   │    640.*     │   │ • ZERO EGRESS │
│              │   │ • Compute BlurHash │   │    960.*     │   │   FEES        │
│              │   │ • Record w/h       │   │   1280.*     │   │               │
│              │   │ • Version = n+1    │   │   1600.*     │   └───────────────┘
│              │   │                    │   │  og-1200x630 │
│              │   │ DB: MediaFile /    │   │  original.jpg│
│              │   │ ProductImage row   │   │              │
└──────────────┘   └────────────────────┘   └──────────────┘

Async: OrphanedMediaCleanupJob deletes R2 objects with no DB reference (weekly).
```

### 15.2 Responsive Variant Set

| Variant | Width | Formats | Used for |
|---------|-------|---------|----------|
| `thumb` | 160px | AVIF, WebP, JPEG | Cart line items, admin tables, search suggestions |
| `sm` | 320px | AVIF, WebP, JPEG | Mobile product cards, 2-up grids |
| `md` | 640px | AVIF, WebP, JPEG | Tablet cards, mobile PDP hero |
| `lg` | 960px | AVIF, WebP, JPEG | Desktop cards, tablet PDP |
| `xl` | 1280px | AVIF, WebP, JPEG | Desktop PDP hero |
| `xxl` | 1600px | AVIF, WebP | Zoom / lightbox view |
| `og` | 1200×630 | JPEG | OpenGraph / social sharing (fixed aspect, JPEG for compatibility) |
| `original` | as uploaded | JPEG | Admin re-download / future reprocessing — **never served to customers** |

Quality settings: AVIF q50, WebP q80, JPEG q82 — the point on the curve where furniture textures (wood grain, fabric weave) remain convincing while file size collapses. Typical result: a 4000px 8 MB upload becomes a 1280px AVIF of ~90 KB — roughly **98% smaller**.

### 15.3 Delivery — Markup Contract

```html
<picture>
  <source
    type="image/avif"
    srcset="https://cdn.gharcraft.com/products/aria-table/v3/320.avif  320w,
            https://cdn.gharcraft.com/products/aria-table/v3/640.avif  640w,
            https://cdn.gharcraft.com/products/aria-table/v3/960.avif  960w,
            https://cdn.gharcraft.com/products/aria-table/v3/1280.avif 1280w"
    sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw">
  <source type="image/webp" srcset="... .webp ..." sizes="...">
  <img
    src="https://cdn.gharcraft.com/products/aria-table/v3/960.jpg"
    alt="Aria teak dining table with six chairs in a sunlit dining room"
    width="960" height="640"          <!-- prevents CLS -->
    loading="lazy"                     <!-- except the LCP image -->
    decoding="async"
    style="background-image:url(data:image/png;base64,<blurhash>);background-size:cover">
</picture>
```

All of the above is encapsulated in a single `<Image>` React component so no developer — human or AI — can accidentally ship a raw `<img src>`.

### 15.4 Loading Strategy

| Image | Strategy |
|-------|----------|
| PDP hero / first gallery image | **Eager**, `fetchpriority="high"`, `<link rel="preload">` in the SSR head — this is the LCP element |
| Homepage banner (above fold) | Eager + preload |
| Remaining gallery images | `loading="lazy"`, prefetched on gallery hover/focus |
| Category grid, first row | Eager |
| Category grid, below fold | `loading="lazy"` with a 200px `rootMargin` |
| Cart thumbnails | `loading="lazy"` |
| Blur placeholder | BlurHash string (≈30 bytes, stored on the row) rendered as the background until decode |

### 15.5 Caching & Versioning

```
https://cdn.gharcraft.com/products/{slug}/v{version}/{width}.{ext}
                                            ▲
                                            └── increments on every re-upload
```

- `Cache-Control: public, max-age=31536000, immutable` — safe **because** the version segment changes when the asset changes.
- No CDN purge is ever needed for images; a new version is simply a new URL.
- The previous version stays available until the cleanup job reaps it, so in-flight pages and social-share caches never 404.
- URLs are composed at read time from `StorageKey` + `Version` + the configured CDN base — the CDN host or storage provider can change without touching a single database row.

### 15.6 Why Cloudflare R2 Specifically

| Property | R2 | S3 / Azure Blob |
|----------|-----|-----------------|
| **Egress fees** | **₹0 — unlimited** | ~$0.09/GB (S3), ~$0.087/GB (Azure) |
| Storage | ~$0.015/GB/month | ~$0.023/GB/month |
| API compatibility | S3 API — works with `AWSSDK.S3` | — |
| CDN integration | Native, same provider, zero-cost origin fetch | Extra CDN product + egress to CDN |
| Vendor lock-in | Low — S3 API means a provider swap is a config change | — |

**Worked example.** 50,000 monthly visitors × 25 images/session × 120 KB average = **~150 GB/month of image egress**.

| Provider | Monthly egress cost |
|----------|--------------------|
| AWS S3 (no CDN) | ~$13.50 (~₹1,130) |
| Azure Blob (no CDN) | ~$13.00 (~₹1,090) |
| **Cloudflare R2 + CDN** | **₹0** |

At 500,000 visitors that gap becomes ~₹11,000/month versus ₹0. For an image-heavy furniture catalog, **egress pricing is the single largest scaling cost lever**, and it is why R2 replaces Azure Blob throughout v2.0.

### 15.7 Additional Image Concerns

| Concern | Handling |
|---------|----------|
| **Upload security** | Magic-byte validation (never trust `Content-Type`), max 15 MB, image dimensions capped, re-encoded through ImageSharp (which neutralises polyglot/malicious payloads), served from a **separate CDN hostname** so a stored-XSS payload cannot execute in the app's origin |
| **EXIF** | Stripped on ingest — removes GPS coordinates of the warehouse/studio and reduces file size |
| **Colour accuracy** | Preserve/convert to sRGB — furniture returns are frequently driven by colour mismatch |
| **Zoom** | `xxl` (1600px) variant loaded on demand in the lightbox only |
| **Alt text** | Required at upload; blocks save if empty (SEO + accessibility) |
| **Processing cost** | ImageSharp runs in-process on upload (admin action, low frequency) — no separate worker needed at MVP |
| **Failure isolation** | Variant generation runs after the DB row is created; a failed variant marks the image `ProcessingFailed` and can be retried from the admin UI |
| **Future — Cloudflare Images** | If on-the-fly transformation is ever preferable to build-time variants, Cloudflare Images ($5/month for 100K images) drops in behind `IImageProcessor` with no application changes |
| **Video (Phase 3)** | 360° spins and room videos → Cloudflare Stream; the same versioned-URL discipline applies |

---

## 16. Cloud & Deployment Architecture

### 16.1 Production Deployment Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│              PRODUCTION ARCHITECTURE (v2.0 — low cost)               │
│                                                                      │
│  ┌─────────────┐                                                    │
│  │   Internet   │                                                    │
│  └──────┬───────┘                                                    │
│         │                                                            │
│  ┌──────▼────────────────────────────────────────┐                  │
│  │            CLOUDFLARE (free tier)             │                  │
│  │  • DNS (authoritative)     • Free SSL/TLS     │                  │
│  │  • Global CDN (300+ PoPs)  • DDoS protection  │                  │
│  │  • WAF managed rules       • Bot Fight Mode   │                  │
│  │  • Brotli + HTTP/3         • Redirect rules   │                  │
│  │  • Web Analytics (free)    • Page Rules       │                  │
│  └───┬─────────────────────────────────┬─────────┘                  │
│      │                                 │                            │
│  ┌───▼──────────────────────┐   ┌──────▼─────────────────────────┐  │
│  │   CLOUDFLARE PAGES       │   │   RAILWAY  (or Render)         │  │
│  │   (frontend, free)       │   │   (backend)                    │  │
│  │                          │   │                                │  │
│  │  • React + Vite build    │──▶│  GharCraft.Api                 │  │
│  │  • Unlimited bandwidth   │   │  ASP.NET Core 8                │  │
│  │  • Auto preview deploys  │   │  512MB–1GB RAM, ~0.5 vCPU      │  │
│  │    per PR                │   │                                │  │
│  │  • Pages Functions (SSR) │   │  • Auto-deploy from GitHub     │  │
│  │  • Custom domain + SSL   │   │  • Health checks (/health)     │  │
│  │  • Rollback in 1 click   │   │  • Zero-downtime restarts      │  │
│  └──────────────────────────┘   │  • Secrets as env vars         │  │
│                                 │  • IMemoryCache in-process     │  │
│                                 │  • BackgroundService jobs      │  │
│                                 └───┬───────────────────┬────────┘  │
│                                     │                   │           │
│                     ┌───────────────▼──────┐   ┌────────▼────────┐  │
│                     │  POSTGRESQL          │   │ CLOUDFLARE R2   │  │
│                     │  Railway PG (prod)   │   │                 │  │
│                     │  Neon (dev/staging)  │   │ products/       │  │
│                     │                      │   │ media/          │  │
│                     │  • Same-region as API│   │ cms/            │  │
│                     │  • Automated backups │   │ banners/        │  │
│                     │  • FTS + pg_trgm     │   │                 │  │
│                     │  • Materialized views│   │ ZERO egress     │  │
│                     └──────────────────────┘   └────────┬────────┘  │
│                                                          │          │
│                                                 ┌────────▼───────┐  │
│                                                 │ Cloudflare CDN │  │
│                                                 │ (image edge)   │  │
│                                                 └────────────────┘  │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │  EXTERNAL SERVICES                                        │        │
│  │  • Razorpay (payments)   • Resend / Brevo (email)         │        │
│  │  • Google Search Console • Cloudflare Web Analytics       │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │  OBSERVABILITY (MVP)                                      │        │
│  │  • Serilog → console (platform log stream) + rolling file │        │
│  │  • /health, /health/ready endpoints                       │        │
│  │  • UptimeRobot (free) → alerts to email/WhatsApp          │        │
│  │  • Cloudflare Analytics for traffic + Core Web Vitals     │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │  SECRETS & CONFIG                                         │        │
│  │  Platform environment variables (encrypted at rest):      │        │
│  │  • ConnectionStrings__Default   • Jwt__SigningKey         │        │
│  │  • Razorpay__KeyId / KeySecret / WebhookSecret            │        │
│  │  • R2__AccessKey / SecretKey / Bucket / PublicBaseUrl     │        │
│  │  • Email__ApiKey                                          │        │
│  │  ✂ No Azure Key Vault at MVP — see §9.3                   │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │  BACKUP & DR                                              │        │
│  │  • PostgreSQL: platform automated daily backups           │        │
│  │  • Weekly `pg_dump` to R2 via GitHub Actions cron         │        │
│  │    (provider-independent — protects against account loss) │        │
│  │  • R2: versioned objects; immutable versioned image keys  │        │
│  │  • Migrations in git = reproducible schema                │        │
│  │  • RTO ≈ 1 hour, RPO ≈ 24 hours at MVP                    │        │
│  │  ✂ Terraform/IaC → future scaling (§21)                   │        │
│  └──────────────────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────────────────┘
```

### 16.2 Deployment Component Choices

| Component | Choice | Alternative | Why |
|-----------|--------|-------------|-----|
| **Frontend hosting** | **Cloudflare Pages** | Vercel, Netlify | Free tier has **unlimited bandwidth** (Vercel/Netlify meter it — dangerous for an image-and-traffic-heavy store); preview deploy per PR; Pages Functions give SSR at the edge |
| **Backend hosting** | **Railway** | Render | Railway: usage-based (~$5/mo credit covers a small API), fastest GitHub→prod path, co-located Postgres. Render: predictable $7/mo, free tier sleeps (unacceptable for a storefront) |
| **Database** | **Railway PostgreSQL** (prod) | **Neon** (dev/staging) | Co-location with the API removes cross-provider latency on every query; Neon's free tier + branching is ideal for development and migration testing |
| **Object storage** | **Cloudflare R2** | S3, Azure Blob | Zero egress fees (§15.6) |
| **CDN / DNS / WAF** | **Cloudflare** | — | One free product covers DNS, CDN, SSL, WAF, DDoS, analytics, redirects |
| **Email** | **Resend** or **Brevo** | SendGrid, SES | Resend: 3,000 emails/month free, excellent DX. Brevo: 300/day free. Only two transactional emails at MVP |
| **CI/CD** | **GitHub Actions** | — | Free for public repos; 2,000 min/month free for private |
| **Container runtime** | **Docker (optional)** | Native buildpack | Railway and Render both build .NET without a Dockerfile. Keep a Dockerfile for local parity and provider portability — but do not make it a prerequisite for shipping |
| **Uptime monitoring** | **UptimeRobot** | Better Uptime | Free 5-minute checks; also keeps a scale-to-zero database warm |

> [!NOTE]
> **Kubernetes and Terraform are explicitly out of scope for the MVP.** A single container with a single database does not need an orchestrator, and a four-resource infrastructure that changes twice a year does not need declarative IaC — both would cost more developer-days than they save. Both appear in §21 as scaling steps, gated on concrete triggers.

### 16.3 Environments

| Environment | Frontend | Backend | Database | Cost |
|-------------|----------|---------|----------|------|
| **Local** | Vite dev server `:5173` | `dotnet watch` `:5000` | Docker Postgres (or Neon branch) | ₹0 |
| **Preview (per PR)** | Cloudflare Pages preview URL | Railway PR environment (optional) | Neon branch | ₹0 |
| **Staging** | `staging.gharcraft.com` (`noindex`) | Railway staging service | Neon free tier | ₹0 |
| **Production** | `gharcraft.com` | Railway production service | Railway PostgreSQL | see §17 |

### 16.4 Deployment Flow

```
git push origin main
        │
        ├──▶ GitHub Actions: backend-ci.yml
        │      dotnet restore → build (warnings as errors)
        │      → unit tests → integration tests (Testcontainers)
        │      → dotnet list package --vulnerable
        │              │ pass
        │              ▼
        │      backend-deploy.yml → Railway
        │        • Build image / buildpack
        │        • Run `dotnet ef database update` as a release step
        │        • Health check /health/ready
        │        • Swap traffic (zero downtime); auto-rollback on failure
        │
        └──▶ GitHub Actions: frontend-deploy.yml
               npm ci → tsc --noEmit → vitest → vite build
               → deploy to Cloudflare Pages
               → (rollback = redeploy previous build, 1 click)
```

### 16.5 Docker (Optional)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY backend/GharCraft.sln backend/Directory.Build.props ./
COPY backend/src/GharCraft.Domain/*.csproj          src/GharCraft.Domain/
COPY backend/src/GharCraft.Application/*.csproj     src/GharCraft.Application/
COPY backend/src/GharCraft.Infrastructure/*.csproj  src/GharCraft.Infrastructure/
COPY backend/src/GharCraft.Api/*.csproj             src/GharCraft.Api/
RUN dotnet restore
COPY backend/ .
RUN dotnet publish src/GharCraft.Api/GharCraft.Api.csproj \
      -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0                 # workstation GC — lower memory on small instances
HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "GharCraft.Api.dll"]
```

```yaml
# docker-compose.yml — OPTIONAL, local development only
services:
  postgres:
    image: postgres:16-alpine
    ports: ["5432:5432"]
    environment:
      POSTGRES_DB: gharcraft
      POSTGRES_USER: gharcraft
      POSTGRES_PASSWORD: dev_password
    volumes: ["postgres_data:/var/lib/postgresql/data"]

  # api:  uncomment only if you prefer containerized local dev.
  #       `dotnet watch run` is faster for day-to-day work.

volumes:
  postgres_data:
```

---

## 17. Cost Optimization Strategy

> **Goal: a production-grade storefront running for less than the price of a monthly streaming subscription — while remaining able to absorb a traffic spike without re-architecture.**

### 17.1 Estimated MVP Monthly Cost

| Service | Tier | USD/month | ₹/month |
|---------|------|-----------|---------|
| **Cloudflare** (DNS, CDN, SSL, WAF, Analytics) | Free | $0 | ₹0 |
| **Cloudflare Pages** (frontend, unlimited bandwidth) | Free | $0 | ₹0 |
| **Railway** (API: 512MB–1GB RAM, ~0.5 vCPU) | Hobby / usage | $5 | ~₹420 |
| **Railway PostgreSQL** (1GB storage, shared CPU) | Usage | $5 | ~₹420 |
| **Cloudflare R2** (20GB storage, unlimited egress) | Pay-as-you-go | $0.30 | ~₹25 |
| **Resend / Brevo** (transactional email) | Free tier | $0 | ₹0 |
| **GitHub Actions** (private repo, ~500 min) | Free tier | $0 | ₹0 |
| **UptimeRobot** (monitoring) | Free | $0 | ₹0 |
| **Domain** (`.com`, amortised) | ~₹1,000/yr | ~$1 | ~₹85 |
| **Total (MVP)** | | **~$11.30** | **≈ ₹950/month** |

**Excluded (Day 1) and what each would have added:** Redis ~₹1,500 · Azure App Service B2 ~₹3,500 · Azure Blob + egress ~₹1,100 · Key Vault ~₹50 · Seq/ELK ~₹1,500 · OpenSearch ~₹3,000 · load balancer ~₹1,600.

> **v1.0 estimate: ₹7,650/month. v2.0 estimate: ₹950/month — an ~88% reduction, for identical user-facing functionality at MVP scale.**

### 17.2 The Twelve Cost Levers

| # | Lever | Saving | Trade-off |
|---|-------|--------|-----------|
| 1 | **R2 instead of S3/Azure Blob** | ₹1,000–11,000/mo at scale | None — S3-compatible API |
| 2 | **Cloudflare Pages instead of Vercel/Netlify** | Avoids metered bandwidth overages | Pages Functions SSR is slightly less ergonomic than Next.js |
| 3 | **`IMemoryCache` instead of Redis** | ~₹1,500/mo | Cache is per-instance and empty after restart |
| 4 | **PostgreSQL FTS instead of OpenSearch** | ~₹3,000/mo | No faceted search/synonyms until Phase 3 |
| 5 | **Materialized views instead of a cache tier** | ~₹1,000/mo | 15-minute staleness on aggregates |
| 6 | **PaaS instead of IaaS + Kubernetes** | ~₹3,000/mo + days of setup | Less control over the runtime |
| 7 | **`BackgroundService` instead of Hangfire server** | ~₹800/mo | No job dashboard, no distributed scheduling |
| 8 | **Env-var secrets instead of Key Vault** | ~₹50/mo + integration time | Manual rotation |
| 9 | **Console/file logs instead of Seq/ELK** | ~₹1,500/mo | Grep instead of a query UI |
| 10 | **Aggressive image optimization** (AVIF/WebP, responsive sets) | 90%+ bandwidth reduction | Upload-time CPU (negligible) |
| 11 | **Long-lived immutable CDN caching** | Origin request volume drops ~95% | Requires versioned URL discipline (§15.5) |
| 12 | **Single modular monolith instead of services** | ~₹5,000+/mo and enormous complexity | Scale the whole unit rather than parts |

### 17.3 Cost at Scale

| Stage | Traffic | Architecture change | Est. ₹/month |
|-------|---------|--------------------|--------------|
| **MVP** | < 1,000 users/day | As specified | **~₹950** |
| **Growth** | 1,000–10,000/day | Railway 2GB RAM; PG 2GB; still no Redis | ~₹2,500 |
| **Scale** | 10,000–50,000/day | 2 API instances + **Redis (₹1,500)** + PG 4GB + read replica | ~₹8,000 |
| **High scale** | 50,000–100,000/day | 3–4 instances, PG General Purpose, OpenSearch, dedicated worker | ~₹25,000–35,000 |

Even at 100,000 users/day, v2.0 lands at roughly what **v1.0 proposed spending on day one**.

### 17.4 Cost Discipline Rules

1. **Every new paid dependency must displace an equivalent cost or unblock revenue.** Write the justification in the PR description.
2. **Egress before storage.** Storage is cheap everywhere; egress is where cloud bills detonate. Optimize for it first.
3. **Cache at the outermost layer that can answer correctly** — edge > output cache > memory > database.
4. **Prefer free tiers with hard limits over usage-based billing without caps** at MVP; a misconfigured loop should fail, not invoice.
5. **Set spend alerts on Railway and Cloudflare from day one.** ₹2,000/month is the alarm threshold.
6. **Re-evaluate quarterly.** Providers change pricing; R2, Neon, and Railway all currently compete hard on free tiers.
7. **Never pay for idle.** Scale-to-zero on non-production environments; the staging database should cost ₹0.

---

## 18. Logging, Monitoring & Observability

### 18.1 Logging Stack — Simplified

> [!IMPORTANT]
> v1.0 specified Serilog **plus** Seq/ELK, OpenTelemetry, Prometheus and Grafana. v2.0 keeps **Serilog** and two sinks.

```
┌─────────────────────────────────────────────────────────────┐
│                    Logging Architecture                      │
│                                                             │
│   Application (structured logging via ILogger<T>)           │
│                        │                                    │
│                    Serilog                                  │
│         ┌──────────────┴──────────────┐                     │
│         ▼                             ▼                     │
│  ┌─────────────┐              ┌──────────────────┐          │
│  │  Console    │              │  Rolling File    │          │
│  │  Sink       │              │  Sink            │          │
│  │             │              │                  │          │
│  │ Dev: human- │              │ logs/log-.txt    │          │
│  │ readable    │              │ Daily rollover   │          │
│  │             │              │ 7-day retention  │          │
│  │ Prod: JSON  │              │ 50 MB size cap   │          │
│  │ → captured  │              │ Shared-access    │          │
│  │   by Railway│              │ for tailing      │          │
│  │   log stream│              │                  │          │
│  └─────────────┘              └──────────────────┘          │
│                                                             │
│  Enrichers: CorrelationId · UserId · RequestPath ·          │
│             MachineName · Environment · SourceContext       │
│  Destructuring policy: redacts Password, Token, CardNumber, │
│             Email(partial), Phone(partial), Address         │
└─────────────────────────────────────────────────────────────┘
```

### 18.2 Log Levels & Policy

| Level | Used for | Example |
|-------|----------|---------|
| `Fatal` | Process cannot continue | Database unreachable at startup |
| `Error` | An operation failed and a user is affected | Payment webhook signature verification failed |
| `Warning` | Recovered/suspicious | Slow query > 1s; login attempt on a locked account; cache miss storm |
| `Information` | Business-significant events | Order placed, payment captured, product published, admin login |
| `Debug` | Development only | Cache hit/miss, SQL parameters |
| `Verbose` | Never in production | — |

Production minimum level is `Information`, with `Microsoft.AspNetCore` at `Warning` and `Microsoft.EntityFrameworkCore.Database.Command` at `Warning` — this keeps log volume (and therefore log storage cost) near zero while preserving every business event.

### 18.3 What Must Always Be Logged

- Every authentication event: login success/failure, lockout, password change, password reset, token refresh reuse detection
- Every admin mutation: entity, ID, before/after (also written to `AuditLog`)
- Every payment state transition, with `orderId`, `gatewayTxnId`, amount, and correlation ID
- Every unhandled exception, with correlation ID and stack trace
- Every request slower than 1,000ms
- Every failed outbound integration (Razorpay, R2, email) with the retry outcome

**Never logged:** passwords, tokens, card data, full email/phone (partial-masked only), full addresses.

### 18.4 Monitoring at MVP

| Signal | Tool | Cost |
|--------|------|------|
| Uptime + response time | UptimeRobot (5-min checks on `/health`) | ₹0 |
| Traffic, Core Web Vitals, geography | Cloudflare Web Analytics (cookieless) | ₹0 |
| Error visibility | Railway/Render log stream + `Error`-level grep | ₹0 |
| Health endpoints | `/health` (liveness), `/health/ready` (DB + R2 reachable) | ₹0 |
| Deploy failures | GitHub Actions notifications | ₹0 |
| Business KPIs | Admin dashboard (orders, revenue, low stock) — Phase 2 | ₹0 |

### 18.5 Observability Scaling Path

| Trigger | Add | Est. cost |
|---------|-----|-----------|
| Grepping logs becomes the bottleneck during an incident | **Seq** (free single-user licence, self-hosted) or **Better Stack** free tier | ₹0–800/mo |
| Errors are discovered by customers before by you | **Sentry** (free tier: 5K events/mo) — recommended as the *first* upgrade | ₹0 |
| Multiple instances make request tracing hard | **OpenTelemetry** → any OTLP backend | varies |
| Capacity planning / SLO tracking needed | **Prometheus + Grafana Cloud** free tier | ₹0–1,500/mo |
| Full log retention/compliance requirement | **ELK** or a managed log platform | ₹1,500+/mo |

> [!TIP]
> If you add exactly one observability tool beyond the MVP stack, make it **Sentry**. Aggregated, deduplicated, stack-traced frontend + backend errors with release tagging is worth more to a solo developer than an entire metrics stack.

---

## 19. Security Plan

### 19.1 OWASP Top 10 Coverage

| # | Vulnerability | Mitigation (v2.0) |
|---|--------------|-------------------|
| A01 | **Broken Access Control** | Two-role model enforced by `[Authorize(Roles = "Admin")]` on every admin controller **plus** explicit ownership checks in application services (a customer can only read their own orders/cart/addresses). Deny-by-default: controllers are `[Authorize]` unless explicitly `[AllowAnonymous]`. |
| A02 | **Cryptographic Failures** | HTTPS everywhere with HSTS. Passwords hashed by ASP.NET Identity (PBKDF2, 100K+ iterations). JWT HMAC-SHA256 with a 256-bit key from env vars. Refresh tokens stored SHA-256 hashed. TLS to the database. |
| A03 | **Injection** | EF Core parameterizes by default. Full-text search uses `EF.Functions.ToTsQuery` with parameterized input — never string-concatenated SQL. FluentValidation on every request DTO. Output encoding is automatic in React (and `dangerouslySetInnerHTML` is sanitized for CMS HTML). |
| A04 | **Insecure Design** | Clean Architecture keeps invariants in the domain. Server-side recalculation of every price, discount, tax and total. Idempotency on payments. Stock verified at order time, not at cart time. |
| A05 | **Security Misconfiguration** | No default error pages; `ProblemDetails` without stack traces in production. CORS allowlist. Security headers middleware. Swagger disabled in production. Admin endpoints separated by route prefix. |
| A06 | **Vulnerable Components** | Dependabot on NuGet + npm. `dotnet list package --vulnerable` and `npm audit` gate the CI pipeline. Dependency count deliberately minimized (§3.3). |
| A07 | **Authentication Failures** | Refresh token rotation with reuse detection (reuse revokes the chain). Lockout after 5 failures for 15 minutes. Rate limit 10/min on auth endpoints. Password policy enforced. Generic error messages — never "user not found". |
| A08 | **Software & Data Integrity** | Razorpay webhook HMAC verification. Amount re-verification server-side. Immutable, versioned image URLs. CI builds from source only; no unpinned scripts. |
| A09 | **Logging & Monitoring Failures** | Serilog structured logs with correlation IDs; `AuditLog` for every admin mutation; all auth and payment events logged; PII redacted; uptime alerting. |
| A10 | **SSRF** | No user-supplied URLs are fetched server-side. Image ingest accepts uploads only, never remote URLs. Webhook callers are signature-verified. |

### 19.2 Additional Security Measures

```
Authentication Security
──────────────────────
• Passwords: ASP.NET Identity default hasher (PBKDF2-HMAC-SHA256, 100k iters)
• Password policy: min 8 chars, 1 uppercase, 1 digit, 1 non-alphanumeric
• JWT: HMAC-SHA256, 256-bit secret, 15-minute access token
• Refresh tokens: one-time use, SHA-256 hashed at rest, rotation with
  reuse-detection (a replayed token revokes the entire token family)
• Account lockout: 5 failed attempts → 15-minute lockout
• Admin login: separate endpoint, stricter rate limit, always audit-logged
• 2FA for admin: Phase 3 (TOTP via ASP.NET Identity — already supported)

Transport Security
──────────────────
• HTTPS everywhere; HSTS max-age=31536000; includeSubDomains; preload
• TLS 1.2+ only (Cloudflare enforced)
• Free, auto-renewing certificates via Cloudflare (frontend) and the
  platform-managed certificate (backend) — no manual renewal to forget

API Security
───────────
• Rate limiting (ASP.NET Core built-in limiter):
    - Public APIs           : 100 req/min per IP
    - Authenticated APIs    : 300 req/min per user
    - Auth endpoints        :  10 req/min per IP
    - Contact form          :   5 req/hour per IP
    - Webhooks              : exempt, signature-verified instead
• Cloudflare WAF + Bot Fight Mode in front of all of the above
• Request size: 10MB default, 15MB on media upload endpoints only
• Validation on every endpoint; unknown JSON fields rejected
• CORS: explicit origin allowlist (no wildcard, no `AllowAnyOrigin`)
• Swagger UI: Development environment only

Data Security
─────────────
• Payment data never stored — tokenized at Razorpay (PCI SAQ-A)
• PII minimized; partial masking in all logs
• Soft deletes preserve the audit trail
• Database connections over TLS; credentials only in env vars
• Weekly encrypted `pg_dump` to R2 (separate credentials from the app)
• EXIF/GPS stripped from every uploaded image

Frontend Security
─────────────────
• Access token in memory only — never localStorage (XSS exfiltration risk)
• Refresh token in HttpOnly + Secure + SameSite=Strict cookie
• CMS-authored HTML sanitized (DOMPurify) before render
• Images served from a separate CDN hostname (isolates stored-XSS payloads)
• Dependencies pinned; `npm audit` in CI

Security Headers (middleware)
─────────────────────────────
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self';
  img-src 'self' https://cdn.gharcraft.com data:;
  script-src 'self' https://checkout.razorpay.com;
  frame-src https://api.razorpay.com;
  connect-src 'self' https://api.gharcraft.com;
  style-src 'self' 'unsafe-inline';
  font-src 'self';
  object-src 'none'; base-uri 'self'; form-action 'self'
```

### 19.3 Pre-Launch Security Checklist

```
[ ] All secrets in environment variables; none in git (verify with `git log -S`)
[ ] `.env`, `appsettings.Production.json` in .gitignore
[ ] Default/seeded admin password changed; seeder disabled in production
[ ] Swagger disabled in production
[ ] CORS allowlist contains only production + preview origins
[ ] Rate limiting verified on auth endpoints (load-tested)
[ ] Razorpay webhook signature verification tested with an invalid signature
[ ] Payment amount tampering tested (client sends ₹1 for a ₹50,000 order → rejected)
[ ] IDOR tested: customer A cannot fetch customer B's order by ID
[ ] Admin endpoints tested with a Customer token → 403
[ ] SQL injection attempted through search and filter parameters
[ ] XSS attempted through product name, review text, and CMS content
[ ] File upload tested with a renamed .exe and an SVG containing script
[ ] Security headers verified (securityheaders.com — target grade A)
[ ] HTTPS redirect + HSTS confirmed
[ ] `dotnet list package --vulnerable` and `npm audit` clean
[ ] Database backup restore rehearsed at least once
[ ] Account lockout verified
[ ] Error responses contain no stack traces or connection strings
```

---

## 20. SaaS Readiness & Multi-Tenant Path

> [!IMPORTANT]
> **Goal: GharCraft ships as a single-tenant store, but the codebase must be resellable to the next furniture retailer — and eventually operable as a multi-tenant SaaS — without a rewrite.**
> **Non-goal: implementing multi-tenancy now.** Multi-tenancy adds tenant resolution, data isolation testing, per-tenant migrations, per-tenant caching, per-tenant billing, and an onboarding flow. That is a product in its own right and would delay the MVP by months. v2.0 instead pays a **near-zero premium today** to keep that door open.

### 20.1 The Reusability Ladder

```
┌─────────────────────────────────────────────────────────────────┐
│ LEVEL 3 — Multi-tenant SaaS                        ⏳ Phase 3+  │
│  One deployment, many tenants, tenant resolved by host/subdomain│
│  Row-Level Security · per-tenant config, theme, domain, billing │
├─────────────────────────────────────────────────────────────────┤
│ LEVEL 2 — White-label single-tenant  ⭐ ACHIEVABLE AT LAUNCH+1  │
│  One codebase, N deployments. New client = new DB + new env     │
│  vars + settings rows. Zero code changes. Deploy in a day.      │
├─────────────────────────────────────────────────────────────────┤
│ LEVEL 1 — Configurable single-tenant       ✅ v2.0 MVP TARGET   │
│  Branding, theme, tax, categories, gateways, features are DATA  │
├─────────────────────────────────────────────────────────────────┤
│ LEVEL 0 — Hardcoded single store               ❌ what to avoid │
│  "GharCraft", #8B4513, 18% GST, INR baked into source           │
└─────────────────────────────────────────────────────────────────┘
```

**v2.0 must ship at Level 1 and be one sprint away from Level 2.**

### 20.2 What Becomes Configuration (Day 1)

| Concern | Configuration surface | Consumed by |
|---------|----------------------|-------------|
| **Brand identity** | `BrandProfile`: site name, tagline, logo, favicon, contact info, social links | `GET /storefront/config` → SPA header/footer/meta |
| **Theme** | `BrandProfile`: primary/secondary/accent colours, heading & body fonts, radius scale | Injected as CSS custom properties (§13.6) |
| **Currency & locale** | `BrandProfile`: currency code, symbol, locale, number format | `<Money>` component + backend formatting |
| **Categories** | Fully data-driven, arbitrary depth, admin-managed | Catalog module — **already correct in v1.0** |
| **Tax rules** | `TaxRule` rows: rate, global or per-category, inclusive/exclusive | `TaxCalculator` domain service — **no hardcoded 18%** |
| **Payment gateways** | `PaymentGatewayConfig`: provider, enabled, default, non-secret config (secrets stay in env vars) | `PaymentGatewayResolver` |
| **Feature flags** | `FeatureFlag`: `reviews`, `wishlist`, `blog`, `coupons`, `guestCheckout`, … | Backend guards + SPA conditional rendering |
| **CMS content** | `CmsPage`, `Banner`, `BlogPost` — all admin-editable | Content module — **already correct in v1.0** |
| **Homepage layout** | `SiteSetting` key `homepage.sections` (jsonb ordered section list) | Homepage renderer |
| **Email templates** | Templates with `{{BrandName}}`, `{{LogoUrl}}`, `{{PrimaryColor}}` placeholders | `EmailService` |
| **SEO defaults** | `SiteSetting`: title template, default OG image, robots policy | `SeoService` |

> [!TIP]
> **The practical test for Level 1:** grep the codebase for `"GharCraft"`, `"₹"`, `"INR"`, `18`, and any hex colour. Every hit outside `SeedData/`, tests, and `appsettings` is a bug against SaaS readiness. Enforce it as a CI check.

### 20.3 Multi-Tenant Readiness — Paid Now, Used Later

| Preparation | Cost today | Payoff later |
|-------------|-----------|--------------|
| **Nullable `TenantId` on Platform tables** (and on `Product`, `Order`, `CmsPage` where it will be needed) | One column, always NULL, no index | Converts multi-tenancy from a schema rewrite to a backfill |
| **`ICurrentTenant` abstraction returning a fixed default** | ~30 lines | Swap the implementation for host/subdomain resolution; call sites never change |
| **Tenant-prefix-ready cache keys** (`CacheKeys` factory) | Zero | `gharcraft:t{tenantId}:…` becomes a one-file change |
| **Tenant-prefixed storage keys** (`{tenant}/products/{slug}/v{n}/…`) | Zero | R2 object isolation without a bucket migration |
| **Settings read through `ISettingsRepository`, never `IConfiguration`** | Zero | Per-tenant settings become a `WHERE TenantId = @id` |
| **No cross-module direct DB joins outside module boundaries** | Discipline only | Enables per-tenant extraction/sharding |
| **Migrations in source control, idempotent, forward-only** | Zero | Per-tenant migration runs become scriptable |

### 20.4 Migration Path to Level 2 (White-Label) — ~1 Sprint

1. Externalize remaining brand strings into `BrandProfile` (should be near-complete already).
2. Add a provisioning script: create database → run migrations → seed roles + admin → seed default settings → set env vars.
3. Parameterize the CI/CD workflow by client (environment + secrets per client).
4. Write a `docs/onboarding-a-client.md` runbook.
5. Verify: stand up a second store from scratch with **zero code changes**. That test passing *is* Level 2.

### 20.5 Migration Path to Level 3 (Multi-Tenant SaaS) — Phase 3+

1. **Tenant resolution** — middleware maps `Host`/subdomain → tenant; populate `ICurrentTenant`.
2. **Data isolation** — PostgreSQL **Row-Level Security** policies on every tenant-scoped table, plus an EF Core global query filter on `TenantId` as defence in depth.
3. **Backfill** — set `TenantId` on all existing rows, then make the column `NOT NULL`.
4. **Cache & storage isolation** — enable the tenant prefix in `CacheKeys` and storage keys.
5. **Per-tenant secrets** — payment/email credentials move from env vars to an encrypted per-tenant store.
6. **Custom domains** — Cloudflare for SaaS (SSL for custom hostnames).
7. **Tenant admin / provisioning UI + subscription billing** — a new module.
8. **Isolation testing** — an automated test suite proving tenant A can never read tenant B's data. Non-negotiable before the first paying tenant.

> **Isolation model choice.** Row-Level Security with a shared schema is recommended over schema-per-tenant or database-per-tenant: one migration run, one connection pool, lowest cost per tenant. Database-per-tenant remains available for a large enterprise client demanding physical isolation — the `ICurrentTenant` abstraction supports connection-string switching too.

### 20.6 Reusable Assets Beyond Code

| Asset | Reuse value |
|-------|-------------|
| This architecture document | Template for every future ecommerce build |
| Database schema + migrations | Category-agnostic — works for décor, lighting, apparel |
| Admin panel | The single most time-consuming component; fully reusable |
| Razorpay integration + webhook hardening | Reusable verbatim |
| Image pipeline (R2 + variants + BlurHash) | Reusable verbatim; valuable for any visual catalog |
| SEO module (sitemaps, JSON-LD, meta) | Reusable verbatim |
| Auth module (Identity + JWT + rotation) | Reusable verbatim |
| CI/CD workflows | Parameterized, reusable |
| React component library | Reusable via CSS-variable theming |

---

## 21. Scalability Roadmap

### 21.1 Scaling from 500 to 100,000 Users/Day

```
Stage 1: MVP  (0 – 1,000 users/day)                         ✅ LAUNCH STATE
════════════════════════════════════════════════════════════

Architecture:
  • 1 API instance (Railway, 512MB–1GB)
  • 1 PostgreSQL instance
  • IMemoryCache + Output Cache
  • Cloudflare CDN + R2 (free/near-free)
  • BackgroundService jobs in-process

Bottleneck: none. Cold start after deploy is the only latency artefact.
Cost: ~₹950/month

────────────────────────────────────────────────────────────

Stage 2: Growth  (1,000 – 10,000 users/day)
════════════════════════════════════════════

Changes (in priority order):
  ✅ Vertical scale API to 2GB RAM (one slider)
  ✅ Vertical scale PostgreSQL; enable connection pooling (PgBouncer/Neon pooler)
  ✅ Extend output cache coverage to every public GET
  ✅ Add missing indexes surfaced by pg_stat_statements
  ✅ Add materialized views for any remaining aggregate query
  ✅ Add Sentry for error tracking
  ⚠️ STILL NO REDIS — a single larger instance is cheaper and simpler

Why no redesign: read traffic is absorbed by CDN + output cache; PostgreSQL
at this volume is running at a few percent utilization.

Cost: ~₹2,500/month

────────────────────────────────────────────────────────────

Stage 3: Scale  (10,000 – 50,000 users/day)
═══════════════════════════════════════════

Changes:
  ✅ Horizontal scale to 2–3 API instances behind the platform load balancer
  ✅ ➕ INTRODUCE REDIS  ← the trigger from §12.5 has fired
       • ICacheService → RedisCacheService  (half a day)
       • Distributed output cache
       • Global rate limiting
  ✅ PostgreSQL read replica; route catalog reads to it
  ✅ Move BackgroundService jobs to a dedicated worker service (or Hangfire)
  ✅ Consider CQRS/MediatR if the team grows past one developer
  ✅ Add OpenTelemetry tracing across instances

Why no redesign: the API is stateless (JWT, no server sessions), so
horizontal scaling is a configuration change. Every swap point
(ICacheService, ISearchService, IPaymentGateway, IFileStorageService)
was designed for exactly this moment.

Cost: ~₹8,000/month

────────────────────────────────────────────────────────────

Stage 4: High Scale  (50,000 – 100,000+ users/day)
══════════════════════════════════════════════════

Changes:
  ✅ 3–6 instances with auto-scaling rules
  ✅ ➕ OPENSEARCH for faceted search, autocomplete, synonyms
       • ISearchService → OpenSearchService  (~1 week incl. indexing pipeline)
  ✅ PostgreSQL General Purpose + 2 read replicas; partition orders by year
  ✅ Redis cluster / managed Redis with persistence
  ✅ ➕ TERRAFORM — infrastructure now changes often enough to justify IaC
  ✅ ➕ KUBERNETES (or Container Apps) — only if instance count and
       deployment complexity genuinely exceed PaaS capabilities
  ✅ Consider extracting the Catalog module as a standalone read service
  ✅ Event-driven order processing (queue-backed)
  ✅ Multi-region CDN tuning; consider a second region

Why no redesign: Clean Architecture confines infrastructure swaps to the
Infrastructure layer; modular monolith boundaries make extraction mechanical.

Cost: ~₹25,000–35,000/month
```

### 21.2 What Changes at Each Stage

| Stage | Database | Cache | Compute | Search | Workers | Logging | IaC |
|-------|----------|-------|---------|--------|---------|---------|-----|
| **MVP** | 1× PG | IMemoryCache | 1 instance | PG FTS + trgm | In-process | Console + file | None |
| **Growth** | 1× PG (larger) + pooling | IMemoryCache | 1 instance (larger) | PG FTS | In-process | + Sentry | None |
| **Scale** | PG + read replica | **Redis** | 2–3 instances | PG FTS | Dedicated worker | + OTel | Optional |
| **High** | PG GP + 2 replicas + partitioning | Redis cluster | 3–6 (auto-scale) | **OpenSearch** | Worker pool | Full stack | **Terraform** |

### 21.3 Deferred-Component Trigger Table

Single reference for *when* each deferred decision is revisited.

| Component | Introduce when | Effort | Cost |
|-----------|---------------|--------|------|
| **Redis** | ≥ 2 API instances, OR global rate limiting needed | 0.5 day | ~₹1,500/mo |
| **CQRS / MediatR** | 2nd developer joins, OR read/write models diverge | ~2 days | ₹0 |
| **Domain events** | ≥ 4 side effects on a single aggregate action | 1 day | ₹0 |
| **Hangfire** | Jobs need retries, scheduling UI, or a separate host | 1 day | ~₹800/mo |
| **OpenSearch** | > 50K products, OR facets/synonyms/typo tolerance beyond `pg_trgm` | ~1 week | ~₹3,000/mo |
| **Stripe** | International customers, OR non-INR currency | ~3 days | ₹0 fixed |
| **Push / SMS** | Order-status engagement becomes a measured priority | ~3 days | usage-based |
| **Granular permissions** | Staff accounts beyond the owner | 3–4 days | ₹0 |
| **Multi-tenancy** | 2nd paying client wants a shared deployment | ~3 weeks | ₹0 fixed |
| **Terraform** | Infrastructure changes more than monthly | ~3 days | ₹0 |
| **Kubernetes** | PaaS limits are genuinely hit (rare below 100K/day) | ~1 week | ₹5,000+/mo |
| **Read replica** | Read latency degrades under analytics/report load | 0.5 day | ~₹1,500/mo |

---

## 22. DevOps & CI/CD

### 22.1 Local Development Environment

```bash
# One-time
git clone <repo> && cd GharCraft
docker compose up -d postgres          # OR point at a free Neon branch
cd backend && dotnet restore
cd ../frontend && npm ci

# Daily
cd backend/src/GharCraft.Api && dotnet watch run      # :5000, hot reload
cd frontend && npm run dev                            # :5173, Vite HMR

# Database
dotnet ef migrations add <Name> -p ../GharCraft.Infrastructure -s .
dotnet ef database update -p ../GharCraft.Infrastructure -s .

# Contract sync
npm run gen:api        # regenerate TS types from /swagger/v1/swagger.json

# Tests
dotnet test ../../tests/GharCraft.UnitTests           # < 5s
dotnet test ../../tests/GharCraft.IntegrationTests    # Testcontainers Postgres
```

> Docker is used **only** for the local database. Running the API under `dotnet watch` rather than in a container preserves sub-second hot reload — a meaningful multiplier over an 8-week build.

### 22.2 CI/CD Pipeline (GitHub Actions)

```
┌───────────────────────────────────────────────────────────────┐
│                     CI/CD PIPELINE                            │
│                                                               │
│  ── Pull Request ────────────────────────────────────────     │
│  ┌─────────┐  ┌───────────┐  ┌──────────┐  ┌─────────────┐   │
│  │ PR      │─▶│ Build     │─▶│ Unit     │─▶│ Integration │   │
│  │ opened  │  │ (warnings │  │ Tests    │  │ Tests       │   │
│  │         │  │  as       │  │ (< 5s)   │  │ (Testcontai-│   │
│  │         │  │  errors)  │  │          │  │  ners + PG) │   │
│  └─────────┘  └───────────┘  └──────────┘  └──────┬──────┘   │
│                                                    │          │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────▼──────┐   │
│  │ tsc --noEmit │  │ npm audit +  │  │ Cloudflare Pages  │   │
│  │ + vitest     │  │ dotnet list  │  │ PREVIEW DEPLOY    │   │
│  │              │  │ --vulnerable │  │ (unique URL/PR)   │   │
│  └──────────────┘  └──────────────┘  └───────────────────┘   │
│                                                               │
│  ── Merge to main ───────────────────────────────────────     │
│  ┌───────────┐  ┌──────────────┐  ┌───────────┐  ┌────────┐  │
│  │ All PR    │─▶│ Deploy API   │─▶│ Run EF    │─▶│ Health │  │
│  │ checks    │  │ → Railway    │  │ migrations│  │ check  │  │
│  │ re-run    │  │              │  │ (release  │  │ /ready │  │
│  │           │  │              │  │  step)    │  │        │  │
│  └───────────┘  └──────────────┘  └───────────┘  └───┬────┘  │
│                                                       │       │
│  ┌───────────────────┐   ┌──────────────────┐   ┌────▼────┐  │
│  │ Deploy frontend   │   │ Smoke test:      │   │ Traffic │  │
│  │ → Cloudflare Pages│──▶│ home, PDP,       │──▶│ swapped │  │
│  │                   │   │ add-to-cart,     │   │ (auto-  │  │
│  │                   │   │ login            │   │ rollback│  │
│  └───────────────────┘   └──────────────────┘   │ on fail)│  │
│                                                  └─────────┘  │
│                                                               │
│  ── Scheduled ───────────────────────────────────────────     │
│  • Weekly: pg_dump → encrypted upload to R2                   │
│  • Weekly: Dependabot PRs (NuGet + npm)                       │
│  • Daily : sitemap freshness check                            │
└───────────────────────────────────────────────────────────────┘
```

### 22.3 Migration Safety Rules

Because migrations run automatically on deploy, they must never be able to take the site down:

1. **Forward-only.** Never edit a migration that has run in production; write a new one.
2. **Additive first.** Add a nullable column → backfill → make it non-nullable in a *later* deploy. Never rename in a single step; add-copy-drop across three deploys.
3. **Backfills go in code or a job, not in a migration.** A long-running migration blocks the deploy and can lock a table.
4. **Test every migration against a Neon branch cloned from production** before merging.
5. **Index creation uses `CONCURRENTLY`** on large tables (requires a raw SQL migration).
6. **Take a manual backup before any destructive migration.** No exceptions.

### 22.4 Coding Standards

| Rule | Enforcement |
|------|-------------|
| Nullable reference types enabled | `Directory.Build.props` |
| Warnings as errors | `Directory.Build.props` |
| C# 12 language version | `Directory.Build.props` |
| `.editorconfig` formatting | `dotnet format --verify-no-changes` in CI |
| Async suffix on async methods; `CancellationToken` on every I/O method | Code review + analyzer |
| No `async void` (except event handlers) | Analyzer |
| `AsNoTracking()` on every read-only query | Code review checklist |
| No `.Result` / `.Wait()` | Analyzer |
| Domain entities never returned from controllers | Code review checklist |
| Services return `Result<T>`; exceptions are exceptional | Code review checklist |
| One public type per file; file name matches type | `.editorconfig` |
| TypeScript `strict: true`; no `any` | `tsc --noEmit` in CI |
| ESLint + Prettier | CI |
| Conventional Commits (`feat:`, `fix:`, `chore:`) | Habit; enables changelog generation |
| Branch per feature; PR into `main`; squash merge | GitHub branch protection |

### 22.5 Testing Strategy

| Layer | Type | Tool | Coverage target |
|-------|------|------|-----------------|
| **Domain** | Unit — pricing, coupons, tax, order state transitions, stock rules | xUnit + FluentAssertions | **90%** — this is where money bugs live |
| **Application services** | Unit with mocked repositories | xUnit + NSubstitute | **70%** |
| **API** | Integration against a real PostgreSQL | `WebApplicationFactory` + Testcontainers | Every endpoint's happy path + auth failure |
| **Payments** | Integration against Razorpay test mode, incl. replayed and tampered webhooks | xUnit | 100% of payment paths |
| **Frontend** | Component tests for cart, checkout, forms | Vitest + Testing Library | Critical paths |
| **E2E** | Browse → add to cart → checkout → pay → order confirmed | Playwright (Phase 2) | One golden path |
| **Load** | Catalog endpoints at 100 concurrent users | k6 (pre-launch) | Verify p95 < 600ms |

> [!TIP]
> **With AI writing much of the implementation, tests are the primary quality gate.** Write the test for a business rule *before* asking AI to implement it: the test encodes intent unambiguously, and a passing test proves the generated code satisfies it. This inverts the usual risk of AI-assisted development.

---

## 23. Service Boundaries

### 23.1 Module Dependency Map

```
┌─────────────────────────────────────────────────────────────┐
│                    Module Dependencies                       │
│                                                             │
│              ┌────────────────────┐                         │
│              │ Domain/Common      │ ← used by ALL           │
│              │ (no separate       │                         │
│              │  Shared project)   │                         │
│              └────────┬───────────┘                         │
│                       │                                      │
│              ┌────────▼───────────┐                         │
│              │ Platform / Settings│ ← used by ALL           │
│              │ (brand, tax, flags,│   (NEW in v2.0)         │
│              │  gateway config)   │                         │
│              └────────┬───────────┘                         │
│         ┌─────────────┼────────────────┐                    │
│         │             │                │                    │
│    ┌────▼─────┐  ┌────▼──────┐  ┌─────▼──────┐            │
│    │ Identity │  │  Catalog  │  │  Content   │            │
│    │ Module   │  │  Module   │  │  Module    │            │
│    │          │  │           │  │            │            │
│    │ No deps  │  │ Depends:  │  │ Depends:   │            │
│    │ on other │  │ Identity  │  │ Identity   │            │
│    │ modules  │  │ (reviews) │  │ (authorId) │            │
│    └────┬─────┘  └────┬──────┘  └────────────┘            │
│         │             │                                     │
│         │        ┌────▼──────┐                              │
│         └───────▶│ Shopping  │                              │
│                  │ Module    │                              │
│                  │ Depends:  │                              │
│                  │ Identity  │                              │
│                  │ Catalog   │                              │
│                  │ Platform  │ ← tax rules, gateway config  │
│                  └────┬──────┘                              │
│                       │                                     │
│                  ┌────▼──────┐                              │
│                  │ Analytics │  (P2 — read-only across all) │
│                  └───────────┘                              │
│                                                             │
│  ┌──────────────┐                                           │
│  │Notifications │  Called explicitly by Shopping (order      │
│  │Module        │  email) and Identity (reset email).        │
│  │              │  No event bus at MVP — two call sites.     │
│  └──────────────┘                                           │
└─────────────────────────────────────────────────────────────┘
```

### 23.2 Communication Between Modules

| Type | Mechanism (v2.0) | Example |
|------|-----------------|---------|
| **Synchronous, same process** | Direct interface call via DI | `CartService` calls `IProductRepository` for the current price |
| **Side effects** | **Explicit call from the orchestrating service** | `OrderService.PlaceOrderAsync` calls `INotificationService.SendOrderConfirmationAsync` after commit |
| **Scheduled work** | `BackgroundService` | Materialized view refresh, abandoned-cart cleanup |
| **Future — decoupled side effects** | MediatR notifications (domain events) | `OrderPlacedEvent` → N handlers, when N ≥ 4 |
| **Future — if extracted** | Message queue | Order service publishes → Inventory service subscribes |

> [!NOTE]
> **v1.0 used domain events for the order-email flow; v2.0 calls the notification service directly.** With exactly two side effects, an explicit call is easier to read, easier to test, and appears in a single stack trace during debugging. The insertion point for events is a single method — `OrderService.PlaceOrderAsync` — so the change stays local when it becomes worthwhile.

### 23.3 Module Boundary Rules

These are the rules that keep future extraction mechanical:

1. A module's services may only be called through their **interfaces**.
2. A module **never** queries another module's tables directly — it goes through the owning module's repository or service.
3. Shared concepts live in `Domain/Common`, not duplicated per module.
4. Cross-module transactions are permitted at MVP (one database) but must be confined to the **orchestrating service** — currently only `OrderService`.
5. Every module exposes a single `DependencyInjection` registration surface.

### 23.4 Future Extraction Path

If a module must become a standalone service:

1. **Extract contracts** (interfaces, DTOs, events) into a shared package.
2. **Replace direct calls** with HTTP or a message queue behind the *same* interface.
3. **Give the service its own database** (data ownership), migrating its tables.
4. **Deploy independently.**

The most likely first extraction is **Catalog as a read-only service** (95% of traffic, cleanest boundary, no writes on the hot path). Clean Architecture plus modular boundaries make this mechanical rather than architectural.

---

## 24. Development Roadmap — Phased Plan

> v1.0 defined five phases across 18 weeks with features distributed by module. v2.0 defines **three phases distributed by business value**: Phase 1 is everything required to legally and practically take money from a customer. Nothing else.

### 24.1 Phase 1 — Core Ecommerce (launchable)

**Definition of done: a real customer can find a product on Google, buy it, pay for it, and receive a confirmation email — and the owner can run the business from the admin panel.**

```
FOUNDATION
[ ] Solution scaffold: Domain / Application / Infrastructure / Api
[ ] Directory.Build.props (nullable, warnings-as-errors, C# 12)
[ ] EF Core 8 + Npgsql; DbContext; audit + soft-delete interceptors
[ ] Result<T> + ProblemDetails exception handler
[ ] Serilog (console + rolling file), correlation ID middleware
[ ] Health checks, Swagger, CORS, rate limiting, security headers
[ ] IMemoryCache + ICacheService + Output Caching setup
[ ] GitHub Actions CI (build + test + vulnerability scan)
[ ] Vite + React + TS + Tailwind scaffold; apiClient; TanStack Query
[ ] Deploy the skeleton to Railway + Cloudflare Pages on day one  ← critical

AUTHENTICATION
[ ] ASP.NET Identity with Guid keys; two roles seeded
[ ] Register / login / logout
[ ] JWT access + refresh token rotation with reuse detection
[ ] Password reset (email #1)
[ ] Admin login + [Authorize(Roles="Admin")] on admin controllers
[ ] Frontend: auth context, protected routes, refresh-on-401 interceptor

CATALOG — PRODUCTS & CATEGORIES
[ ] Category entity + hierarchical CRUD (admin) + public tree API
[ ] Product + ProductVariant + ProductImage entities
[ ] Product CRUD (admin) with attributes (jsonb) and SEO fields
[ ] Product listing API: pagination, sorting, price/category/attribute filters
[ ] Product detail API by slug
[ ] PostgreSQL full-text search + pg_trgm suggestions
[ ] Materialized views + refresh BackgroundService
[ ] Seed data: real categories + 20–30 demo products

IMAGES  (§15 — do not defer any of this)
[ ] R2 bucket + IFileStorageService (AWSSDK.S3)
[ ] Upload endpoint: validation, EXIF strip, ImageSharp variants, BlurHash
[ ] Versioned immutable storage keys; CDN URL composition
[ ] <Image> React component: srcset, sizes, AVIF/WebP/JPEG, lazy, blur
[ ] Media library (admin): browse, upload, delete, alt text required

CART
[ ] Cart + CartItem; guest cart via session cookie
[ ] Add / update / remove; server-side price recalculation
[ ] Guest → user cart merge on login
[ ] Frontend cart drawer with optimistic updates

CHECKOUT & ORDERS
[ ] Address book (add/edit/delete/default)
[ ] Order + OrderItem with full snapshotting
[ ] Sequential, human-readable order numbers
[ ] Server-side totals: subtotal + tax (TaxRule) + shipping
[ ] Stock validation at order time; deduction on payment success
[ ] Order history + order detail + status timeline (customer)
[ ] Order management + status updates (admin)

PAYMENTS
[ ] Razorpay order creation with idempotency key
[ ] Razorpay Checkout integration (frontend)
[ ] Webhook endpoint: HMAC verification, idempotency, amount re-verification
[ ] Payment → Order → Inventory → Cart transaction
[ ] Refund initiation (admin)
[ ] Test-mode QA: success, failure, timeout, replayed webhook, tampered amount

NOTIFICATIONS  (exactly two emails)
[ ] Email service (Resend/Brevo) + branded HTML templates
[ ] Order confirmation email
[ ] Password reset email

CONTENT & SEO  (§14 — first-class, not deferred)
[ ] CMS pages (about, contact, shipping, returns, privacy, terms)
[ ] Banner management (admin) + public API
[ ] SeoMetadata on products, categories, collections, pages
[ ] <SeoHead> component: title, meta, canonical, OG, Twitter
[ ] JSON-LD: Product, Offer, BreadcrumbList, Organization, WebSite
[ ] sitemap.xml (index + products + categories + content)
[ ] robots.txt (environment-aware)
[ ] SSR/prerender for indexable routes
[ ] Google Search Console verification + sitemap submission

PLATFORM / SAAS READINESS  (§20 — cheap now, expensive later)
[ ] SiteSetting + BrandProfile + TaxRule + FeatureFlag entities
[ ] GET /storefront/config; theme applied via CSS variables
[ ] Admin settings screens: brand, tax, features
[ ] CI check: no hardcoded brand strings, currency, or hex colours

ADMIN PANEL
[ ] Admin shell: layout, nav, auth guard
[ ] Products (list/create/edit/images/variants), Categories, Collections
[ ] Orders, Customers, Inventory, Media, Content, Settings

LAUNCH
[ ] Domain + Cloudflare DNS/SSL
[ ] Production secrets configured
[ ] §19.3 security checklist executed end to end
[ ] Backup + restore rehearsed
[ ] Load test (k6): catalog p95 < 600ms at 100 concurrent
[ ] Lighthouse ≥ 90 performance / 100 SEO on home + PDP
[ ] UptimeRobot monitoring live
[ ] Real ₹1 transaction in Razorpay live mode, then refunded
```

### 24.2 Phase 2 — Engagement & Insight

*Starts only after Phase 1 is live and has processed real orders.*

```
[ ] Wishlist (add/remove/list; merge on login)
[ ] Coupons: entity, admin CRUD, cart application, usage limits,
    category/product scoping, stacking rules
[ ] Product reviews: submit, moderate, aggregate rating,
    AggregateRating JSON-LD on the PDP
[ ] Blog: admin editor, listing, detail, tags, BlogPosting JSON-LD,
    blog → product internal linking
[ ] Analytics dashboard: revenue, orders, AOV, conversion, top products,
    low stock, recent orders
[ ] Sales reports with date range + CSV export
[ ] Customer analytics: repeat rate, lifetime value
[ ] Audit log viewer
[ ] Recently viewed + product comparison
[ ] Bulk product import (CSV)
[ ] Abandoned cart report (email recovery is Phase 3)
[ ] Playwright E2E on the golden purchase path
[ ] Sentry error tracking
```

### 24.3 Phase 3 — Scale & Platform

*Each item is gated on the trigger in §21.3. None is scheduled by date.*

```
INFRASTRUCTURE
[ ] Redis                     ← trigger: 2nd API instance
[ ] PostgreSQL read replica   ← trigger: read latency under reporting load
[ ] Dedicated worker / Hangfire ← trigger: job retries & scheduling UI needed
[ ] OpenTelemetry             ← trigger: multi-instance tracing needed
[ ] Terraform                 ← trigger: infra changes more than monthly
[ ] Kubernetes / Container Apps ← trigger: PaaS limits genuinely reached

CAPABILITY
[ ] OpenSearch                ← trigger: >50K SKUs or facet/synonym needs
[ ] CQRS with MediatR         ← trigger: 2nd developer, or divergent models
[ ] Domain events             ← trigger: ≥4 side effects per action
[ ] Granular roles/permissions ← trigger: staff accounts beyond the owner

COMMERCE
[ ] Stripe (multi-currency, international cards)
[ ] Multiple gateway selector driven by PaymentGatewayConfig
[ ] EMI / BNPL options
[ ] Shipping provider integration + live tracking
[ ] GST invoice PDF generation

ENGAGEMENT
[ ] Push notifications (web push)
[ ] SMS notifications (order status, OTP for high-value orders)
[ ] Marketing email campaigns + abandoned-cart recovery
[ ] Google OAuth login
[ ] AI chatbot / assisted search
[ ] Recommendation engine ("customers also bought")

PLATFORM
[ ] Multi-tenancy (§20.5): tenant resolution, RLS, backfill,
    per-tenant secrets, custom domains, provisioning UI, billing
[ ] White-label onboarding runbook + provisioning script
[ ] i18n / multi-language
[ ] Multi-currency
[ ] Mobile app BFF endpoints
[ ] 360° product spins / room videos (Cloudflare Stream)
[ ] AR "view in your room"
```

---

## 25. Realistic AI-Assisted Delivery Plan

> This section is the architect's independent assessment — not a restatement of the requirements. It answers: *given one developer, an AI pair, and a mandate to finish fast without sacrificing quality, what actually happens week by week, and where does this go wrong?*

### 25.1 The Honest Estimate

| Scenario | Duration to a live, order-taking store |
|----------|---------------------------------------|
| Solo, no AI, part-time | 6–9 months |
| Solo, no AI, full-time | 4–5 months |
| **Solo + AI, part-time (≈20 h/week)** | **14–18 weeks** |
| **Solo + AI, full-time (≈40 h/week)** | **8–10 weeks** ⭐ *plan target* |
| Solo + AI, full-time, cutting Phase 1 scope | 6–7 weeks (see §25.5) |

AI reliably compresses **typing**, not **thinking**. It is 3–5× faster on CRUD controllers, EF configurations, DTOs, mappers, validators, React forms, admin tables, and test scaffolding — perhaps 70% of the line count. It provides close to **zero** speed-up on payment-flow correctness, cache-invalidation reasoning, production debugging, third-party quirks, deployment configuration, and taste-driven UI. Those are roughly 30% of the lines and 60% of the calendar time.

**Plan the calendar around the 30%.**

### 25.2 Week-by-Week (full-time, 8–10 weeks)

| Week | Focus | Ships at end of week |
|------|-------|----------------------|
| **1** | Foundation + deployment pipeline + auth | Skeleton API **live on Railway**, SPA **live on Pages**, register/login working in production |
| **2** | Catalog domain: categories, products, variants, listing/detail APIs, search | Admin can create a product; storefront lists and shows it |
| **3** | Images end-to-end (R2, variants, `<Image>`, media library) + storefront catalog UI | Product pages look real; Lighthouse performance ≥ 85 |
| **4** | Cart + address book + checkout UI | Full pre-payment flow works |
| **5** | **Razorpay + webhooks + orders** ← *the highest-risk week; do not compress it* | Test-mode payment produces a confirmed order and an email |
| **6** | Admin panel completion (orders, inventory, customers, content, settings) | Owner can run the business unaided |
| **7** | SEO: SSR/prerender, JSON-LD, sitemaps, canonicals, meta + performance pass | Lighthouse SEO 100; rich results validate |
| **8** | Hardening: security checklist, load test, backups, live-mode payment test, content population | **LAUNCH** |
| **9–10** | Buffer — it will be used | Bug fixes, real-customer feedback, polish |

> [!IMPORTANT]
> **Deploy in week 1, not week 8.** The most common way solo projects miss their date is discovering deployment, CORS, migrations-on-startup, secret management, and HTTPS problems at the end. Ship an empty API to production on day two and keep it green thereafter.

### 25.3 How to Work With the AI Pair (concretely)

| Practice | Why it matters |
|----------|----------------|
| **Give it this document as context, per session** | Architectural consistency is the first thing lost across a long AI-assisted build |
| **Work one vertical slice at a time** — entity → config → repo → service → controller → validator → tests → UI | Slices are reviewable; layer-at-a-time generates hundreds of unreviewed lines |
| **Write the test first for money-touching logic** (pricing, tax, coupons, stock, refunds) | The test encodes intent; AI code that passes it is verifiable rather than plausible |
| **Never merge code you haven't read** | The failure mode of AI development is silent, plausible wrongness — not obvious breakage |
| **Warnings-as-errors + `strict` TS + nullable refs** | The compiler reviews the first draft so you can review the second |
| **Ask for the boring version first** | Unprompted, AI reaches for abstractions (generic repos, mediators, factories). This document is your counter-spec |
| **Regenerate API types after every DTO change** | Keeps frontend and backend honest at compile time |
| **Keep a `decisions.md`** | One line per non-obvious choice; prevents re-litigating and keeps AI sessions aligned |
| **Hand-audit these four areas regardless of tests** | Payment webhook handling · authorization/ownership checks · cache invalidation · SQL generated on hot paths (`AsNoTracking`, N+1) |

### 25.4 Where This Project Will Actually Slip

Ranked by likelihood × cost:

| Risk | Why | Mitigation |
|------|-----|------------|
| **1. Payment edge cases** | Webhook ordering, duplicate delivery, partial failures, refunds, reconciliation — no amount of AI removes the need to think this through | Allocate a full week. Test replayed webhooks, tampered amounts, and browser-closed-mid-payment explicitly |
| **2. Admin panel scope creep** | Admin UI is ~40% of total UI work and expands invisibly ("just add a filter…") | Freeze the admin feature list at the start of week 6. Plain tables and forms — no dashboards in Phase 1 |
| **3. SSR/prerender complexity** | Pages Functions SSR is powerful but fiddly; SEO is non-negotiable so it cannot simply be dropped | Timebox to 3 days. Fall back to build-time prerender + bot-serving (§14.2) rather than slipping the launch |
| **4. Content & photography** | 30 products × 8 professional photos, descriptions, specs, and copy is *business* work that blocks launch and is nobody's assigned task | Start photography in week 1, in parallel. Build the CSV importer if the catalog exceeds ~50 SKUs |
| **5. Design perfectionism** | Furniture sites are judged visually; it is easy to lose two weeks on the homepage | Pick one reference site and match its *layout* early. Ship, then iterate with real traffic |
| **6. Silent AI-introduced defects** | Plausible code that mishandles an edge case and passes review-by-skimming | Tests on all money logic; hand-audit the four areas in §25.3 |
| **7. Free-tier surprises** | Cold starts, sleeping services, rate caps | Use paid Railway (~₹420) rather than a sleeping free tier for the storefront; UptimeRobot keeps things warm |

### 25.5 If You Must Launch in 6 Weeks

Cut in this order — each line is safe to defer, and none of them is architectural:

1. Collections (categories alone are sufficient)
2. Product variants (single-SKU products; add variants post-launch)
3. Guest checkout (require an account — simpler cart and order model)
4. Advanced filters (category + price only)
5. Blog (Phase 2 already)
6. Admin dashboard KPIs (Phase 2 already)
7. Search autocomplete (plain search only)

**Never cut, at any deadline:** payment correctness · authorization/ownership checks · image optimization · SEO fundamentals · backups · the security checklist. Each of these is orders of magnitude more expensive to retrofit than to build, and the first three are visible to customers within hours of launch.

### 25.6 Definition of "Solid Quality" for This Project

Quality here is not test coverage percentage or architectural purity. It is these seven properties:

1. **No customer is ever charged incorrectly** — server-side totals, idempotent payments, verified webhooks, tested refunds.
2. **No customer can ever see another customer's data** — ownership checks on every customer-scoped resource, tested.
3. **The site is fast on a mid-range Android phone on 4G** — LCP < 2.5s on the product page.
4. **Google can crawl, render, and richly display every product** — SSR/prerender, JSON-LD, sitemaps, canonicals.
5. **The owner can run the entire business without calling you** — complete, unglamorous admin panel.
6. **A failure is recoverable** — backups exist, have been restored once, and migrations are forward-only.
7. **The next client can be onboarded without touching code** — Level 1 configurability achieved (§20.2).

If all seven hold, the architecture is doing its job — regardless of whether MediatR, Redis, or Kubernetes are anywhere in the repository.

---

## 26. Risks & Mitigation

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| 1 | **Single developer bottleneck** | High | High | Clean Architecture enables focused work on one module at a time. AI pair for volume work. Deploy from week 1. Comprehensive tests on money logic reduce regression fear. Strict phase gating. |
| 2 | **Scope creep** | High | High | Phase 1 is defined as "can take money" and nothing more. Admin feature list frozen at week 6. §25.5 pre-defines the cut list so scope decisions are made calmly, in advance. |
| 3 | **Payment integration complexity** | Medium | Critical | Hosted Razorpay Checkout (no card data). Webhook HMAC verification, idempotency keys, server-side amount re-verification. A full dedicated week. Live-mode ₹1 test before launch. |
| 4 | **AI-generated code containing subtle defects** | **High** | **High** | *(New in v2.0)* Warnings-as-errors + strict TS + nullable refs. Tests written before implementation for money logic. Mandatory human read of every merge. Hand-audit of payments, authorization, cache invalidation, and hot-path SQL. |
| 5 | **Poor SEO performance post-launch** | Medium | High | *(New in v2.0)* SEO treated as architecture (§14): SSR/prerender, JSON-LD, sitemaps, canonicals from day one — not retrofitted. Search Console monitored weekly after launch. |
| 6 | **Image weight destroying mobile conversion** | Medium | High | *(New in v2.0)* Mandatory `<Image>` component; AVIF/WebP responsive variants; lazy loading; BlurHash; immutable CDN caching. Lighthouse gate before launch. |
| 7 | **In-memory cache inconsistency after scaling** | Low | Medium | *(New in v2.0)* Single instance at MVP by design. §12.5 defines the exact trigger for Redis; migration is half a day behind `ICacheService`. |
| 8 | **Free/hobby tier limits or provider pricing changes** | Medium | Medium | *(New in v2.0)* Every component is portable: R2 is S3-compatible, PostgreSQL is standard, the API is a plain container. Weekly `pg_dump` to R2 is provider-independent. Spend alerts at ₹2,000/month. |
| 9 | **Database performance at scale** | Low | High | Indexing designed up front (§8.3). Materialized views for aggregates. `pg_stat_statements` reviewed monthly. Read replica available as a config change. |
| 10 | **Security breach** | Low | Critical | OWASP Top 10 coverage (§19.1). Pre-launch checklist (§19.3). Dependabot + CI vulnerability scanning. Payment tokenization. No secrets in git. |
| 11 | **Content/photography not ready at launch** | **High** | High | *(New in v2.0)* This is the most commonly missed blocker. Photography and copy start in week 1, in parallel with development. CSV importer if > 50 SKUs. |
| 12 | **Third-party dependency failure** | Low | Medium | Razorpay: webhook retries + manual verification screen in admin. Email: retry with logged failure — an email failure never fails an order. R2: images cached at the edge for a year. |
| 13 | **Data loss** | Very Low | Critical | Platform automated backups **plus** an independent weekly `pg_dump` to R2. Restore rehearsed before launch. Forward-only migrations in git. Manual backup required before any destructive migration. |
| 14 | **Over-simplification blocking growth** | Low | Medium | *(New in v2.0)* Every deferred component has a documented trigger, effort estimate, and cost (§21.3), and a designed insertion point (`ICacheService`, `ISearchService`, `IPaymentGateway`, `ICurrentTenant`). Simplification is reversible by construction. |
| 15 | **SaaS reuse blocked by hardcoded branding** | Medium | Medium | *(New in v2.0)* Level 1 configurability is Phase 1 scope, plus a CI check for hardcoded brand strings, currency symbols, and hex colours. |
| 16 | **.NET 8 reaching end of support (Nov 2026)** | **Certain** | Medium | *(Corrected in v2.0)* .NET 8's LTS window closes ~3 months after this document's date. **Either target `net10.0` from the start, or schedule the one-day upgrade as a non-negotiable Phase 2 item.** See the advisory in §3.2. PostgreSQL, React, and the S3 API are decade-safe regardless. |
| 17 | **Insufficient testing** | Medium | High | Tests written alongside (often before) implementation. 90% target on domain money logic; integration tests on every endpoint's happy path and auth failure; Razorpay test-mode coverage of all payment paths. |

---

## 27. Appendix — Architecture Diagrams (Text)

### 27.1 Request Flow (Read — Catalog)

```
Customer → Cloudflare CDN → (edge hit? return HTML/JSON) → Railway → API
  → CorrelationIdMiddleware → Serilog request logging
    → SecurityHeadersMiddleware
      → RateLimiter
        → OutputCache middleware  → (hit? return cached response)
          → ProductsController.GetBySlug("modern-sofa")
            → IProductService.GetBySlugAsync(slug)
              → ICacheService.GetOrCreateAsync("gharcraft:products:modern-sofa", 10m)
                 → (miss) IProductRepository.GetBySlugWithDetailsAsync
                    → EF Core (AsNoTracking, single query with Includes)
                      → PostgreSQL  [idx_products_slug]
                    → product.ToDetailDto()   (hand-written mapper)
                 → store in IMemoryCache
              → Result<ProductDetailDto>.Success
            → result.ToActionResult()  → 200 OK
          → OutputCache stores response (TTL 60s, tags: products, product:modern-sofa)
        → CDN caches (60s + stale-while-revalidate 300s)
  → Customer

Latency: ~2ms (CDN hit) · ~5ms (output cache) · ~15ms (memory cache)
         · ~120ms (full DB path)
```

### 27.2 Request Flow (Write — Place Order & Pay)

```
Customer → API
  → [Authorize] → OrdersController.Place(PlaceOrderRequest)
    → ValidationFilter (FluentValidation)
      → IOrderService.PlaceOrderAsync(userId, request, idempotencyKey)
        → Load cart with items + variants
        → Verify cart not empty; verify ownership
        → Re-read CURRENT prices from the database   ← never trust the client
        → Validate stock for every variant
        → Validate coupon                             [Phase 2]
        → TaxCalculator.Calculate(items, TaxRules)    ← config-driven, not 18% hardcoded
        → Compute subtotal, discount, tax, shipping, total
        → BEGIN TRANSACTION
             Create Order (snapshot addresses as jsonb)
             Create OrderItems (snapshot name, SKU, price, image)
             Create Payment (Pending, idempotencyKey)
             Reserve stock
           COMMIT
        → IPaymentGateway.CreateOrderAsync(total, "INR", idempotencyKey)
        → Result<OrderCreatedDto>.Success(orderId, razorpayOrderId, amount)
    → 201 Created
  → Frontend opens Razorpay Checkout with razorpayOrderId

... customer pays ...

Razorpay → POST /api/v1/webhooks/razorpay
  → Verify HMAC signature                    (fail → 400, logged)
  → Idempotency: gateway_txn_id already processed?  (yes → 200, no-op)
  → Load Payment by gateway order id
  → Re-verify amount == Order.TotalAmount    (mismatch → alert + manual review)
  → BEGIN TRANSACTION
       Payment  → Completed, PaidAt set
       Order    → Confirmed
       Inventory→ deducted (InventoryRecord written)
       Cart     → cleared
     COMMIT
  → INotificationService.SendOrderConfirmationAsync(order)   (failure logged, never fails the order)
  → Invalidate product/inventory caches
  → 200 OK
```

### 27.3 Image Request Flow

```
Browser parses <picture>, picks the best (format, width) for its viewport & DPR
  → GET https://cdn.gharcraft.com/products/aria-table/v3/640.avif
    → Cloudflare edge
       ├── HIT  (99%+ after warm-up) → served in ~10–30ms, ₹0
       └── MISS → origin fetch from R2 (zero egress cost)
                → cached at edge for 1 year (immutable)
  → Decode; BlurHash placeholder is replaced with zero layout shift
```

### 27.4 Complete System Context Diagram

```
                          ┌──────────────────────┐
                          │    Admin (Browser)    │
                          │ • Product management  │
                          │ • Order management    │
                          │ • Content editing     │
                          │ • Brand & settings    │
                          └──────────┬───────────┘
                                     │
                          ┌──────────▼───────────┐
                          │   Customer (Browser)  │
                          │ • Browse & search     │
                          │ • Cart & checkout     │
                          │ • Account & orders    │
                          └──────────┬───────────┘
                                     │ HTTPS
                    ┌────────────────▼─────────────────┐
                    │           CLOUDFLARE             │
                    │  DNS · CDN · WAF · SSL · Analytics│
                    └───┬──────────────────────────┬───┘
                        │                          │
            ┌───────────▼──────────┐   ┌───────────▼────────────┐
            │  Cloudflare Pages    │   │   Railway / Render     │
            │  React + Vite SPA    │──▶│   GharCraft.Api        │
            │  + SSR functions     │   │   ASP.NET Core 8 LTS   │
            │  (SEO routes)        │   │                        │
            └──────────────────────┘   │  Catalog │ Shopping    │
                                       │  Identity│ Content     │
                                       │  Platform│ Notifications│
                                       │                        │
                                       │  IMemoryCache          │
                                       │  BackgroundService     │
                                       └──┬──────────┬──────┬───┘
                                          │          │      │
                    ┌─────────────────────▼┐  ┌──────▼───┐ ┌▼──────────────┐
                    │   PostgreSQL 16      │  │Cloudflare│ │ Email provider│
                    │   (Railway / Neon)   │  │    R2    │ │ (Resend/Brevo)│
                    │   FTS · pg_trgm      │  │ (images) │ │  • order conf │
                    │   Materialized views │  └────┬─────┘ │  • pwd reset  │
                    └──────────────────────┘       │       └───────────────┘
                                                   │
                                          ┌────────▼────────┐
                                          │ Cloudflare CDN  │
                                          │  (image edge)   │
                                          └─────────────────┘

                    ┌─────────────┐        ┌──────────────────┐
                    │  Razorpay   │        │  Stripe          │
                    │  ✅ Day 1   │        │  ⏳ Phase 3      │
                    └─────────────┘        └──────────────────┘
```

### 27.5 Clean Architecture Dependency Rule (Reference)

```
        ┌──────────────────────────────────────────┐
        │                  Api                     │  ← knows everything
        │   Controllers · Middleware · Program.cs  │
        └───────────────┬──────────────────────────┘
                        │
        ┌───────────────▼──────────────────────────┐
        │              Application                 │  ← knows Domain only
        │   Services · DTOs · Validators · Ports   │
        └───────────────┬──────────────────────────┘
                        │
        ┌───────────────▼──────────────────────────┐
        │                Domain                    │  ← knows nothing
        │   Entities · VOs · Rules · Interfaces    │
        └───────────────▲──────────────────────────┘
                        │ implements
        ┌───────────────┴──────────────────────────┐
        │            Infrastructure                │  ← knows Domain + Application
        │   EF Core · R2 · Razorpay · Cache · Mail │
        └──────────────────────────────────────────┘

Compile-time enforcement: project references only point inward.
Infrastructure is referenced solely by Api, and only in Program.cs.
```

---

## 28. Open Questions

> [!IMPORTANT]
> Decisions still needed. v1.0's questions are carried forward with v2.0 recommendations attached, so each can be answered with a yes/no rather than an investigation.

| # | Question | v2.0 Recommendation | Blocks |
|---|----------|--------------------|--------|
| 1 | **Hosting split** — Railway or Render for the backend? | **Railway** — usage-based pricing, co-located PostgreSQL, fastest GitHub→prod path. Render if you prefer a flat $7/month. | Week 1 |
| 2 | **Brand name final?** — is "GharCraft" locked? | Affects domain purchase, seed data, and email sender identity. Because branding is data (§20.2), a change is cheap — but the *domain* is not. | Week 1 |
| 3 | **Target geography** — India-only at launch? | **Assume India-only:** INR, Razorpay, GST tax rules, India-focused SEO. Multi-currency stays in Phase 3. | Week 2 (tax model) |
| 4 | **Tax model** — flat GST or per-category HSN rates? | The `TaxRule` schema supports both. Confirm whether furniture categories carry different GST slabs (they can — 12% vs 18%). | Week 4 |
| 5 | **Shipping** — flat rate, weight-based, or free above a threshold? | **Free above ₹X, flat below** is simplest for MVP. Carrier integration is Phase 3. | Week 4 |
| 6 | **Guest checkout** — allowed, or account required? | **Allow guest checkout** — it measurably lifts conversion. It is the #3 item on the cut list if the timeline tightens. | Week 4 |
| 7 | **Product variants** — do launch SKUs have fabric/size/colour options? | If most products are single-SKU, variants become #2 on the cut list and week 2 shortens. | Week 2 |
| 8 | **Email provider** — Resend or Brevo? | **Resend** — best developer experience, 3,000/month free. Brevo if you also want marketing campaigns in the same tool later. | Week 5 |
| 9 | **SSR approach** — Cloudflare Pages Functions, or build-time prerender? | **Attempt Pages Functions (Option B, §14.2), timeboxed to 3 days**, with build-time prerender as the fallback. | Week 7 |
| 10 | **Catalog size at launch** | If > 50 SKUs, build the CSV importer in Phase 1 instead of Phase 2. | Week 2 |
| 11 | **Photography** — ready, or to be produced? | **Start immediately regardless.** This is the most likely non-technical launch blocker (§26 risk 11). | Week 1 |
| 12 | **SaaS ambition timing** — is a second client already in view? | If yes, budget one extra sprint after launch for Level 2 white-labelling (§20.4). If no, Level 1 configurability alone is sufficient. | Post-launch |
| 13 | **Admin UI** — custom React admin (as specified), or a template? | **Custom, using the same Tailwind primitives** — a template would fight the white-label theming model and add a dependency. | Week 6 |
| 14 | **.NET 8 or .NET 10?** — .NET 8 LTS support ends Nov 2026 (§3.2) | **If no code exists yet, target `net10.0` and skip the migration.** Otherwise build on 8 and schedule the one-day upgrade in Phase 2. Architecture is identical either way. | **Week 1 — decide first** |

---

## Document Control

| Version | Date | Author | Summary |
|---------|------|--------|---------|
| 1.0 | Aug 3, 2026 | Principal Software Architect | Initial enterprise-grade architecture (.NET 9, MediatR/CQRS, Redis, Azure, 5 projects, 4 roles, 18-week roadmap) |
| **2.0** | **Aug 4, 2026** | **Principal Software Architect** | **Solo-developer / low-cost / SaaS-ready revision.** .NET 8 LTS; MediatR removed; `IMemoryCache` replaces Redis; 4 projects; 2 roles; Cloudflare R2 + Pages + Railway + Neon replace Azure; Serilog simplified; Razorpay only; two transactional emails; React/Vite/TS/Tailwind frontend specified; SEO (§14), Images (§15), Cost Optimization (§17), SaaS Readiness (§20) and AI-Assisted Delivery (§25) added; roadmap restructured into 3 phases; MVP infrastructure cost reduced from ~₹7,650 to ~₹950/month |

*This document is the single source of truth for GharCraft's architecture. Every deferred component in it carries an explicit re-entry trigger (§21.3) — simplification here is a scheduling decision, not a technical debt.*

