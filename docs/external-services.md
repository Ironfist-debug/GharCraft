# GharCraft — External Services & Account Setup

This document tracks every third-party service GharCraft depends on: what it does, who needs to own the account, how to set it up, and what credentials to hand over to the development team.

**Keep this file updated** whenever a new service is added. Every row in the tables below must stay in sync with what is live in Railway.

---

## How to hand over credentials

> **Never paste API keys, passwords, or secrets into chat, email, or WhatsApp.**
> Use one of these instead:
> - Share via Railway environment variables directly (preferred — dev team never sees the raw value)
> - Use a password manager's secure-share feature (1Password, Bitwarden)
> - If in person: type it directly into Railway's dashboard together

---

## Service registry

| # | Service | Purpose | Account owner | Status |
|---|---------|---------|---------------|--------|
| 1 | **Railway** | Backend hosting + PostgreSQL database | Client or Dev team | ✅ Live |
| 2 | **Resend** | Transactional email (password reset, order confirmation) | Client (owns sender domain) | ⚙️ Needs setup |
| 3 | **Cloudflare Pages** | Frontend hosting + CDN | Client | 🔜 Phase 1 |
| 4 | **Cloudflare DNS** | Domain routing + SSL | Client | 🔜 Phase 1 |
| 5 | **Razorpay** | Payment gateway | Client (must be Indian business entity) | 🔜 Phase 1 |
| 6 | **Cloudflare R2** | Product image storage + CDN | Client | 🔜 Phase 1 |
| 7 | **UptimeRobot** | Uptime monitoring + alerts | Dev team | 🔜 Launch |

---

## 1. Railway — Backend hosting

**Who owns it:** Dev team (or client, if they prefer). Railway is the server that runs the API and the PostgreSQL database.

### If dev team sets it up

1. Go to [railway.app](https://railway.app) and sign in with GitHub.
2. Click **New Project → Deploy from GitHub repo → GharCraft**.
3. Railway auto-detects the `Dockerfile` at the repo root and builds the API.
4. Add a PostgreSQL database: **New Service → Database → PostgreSQL**.
5. Railway automatically injects `DATABASE_URL` into the API service — no manual wiring needed.
6. Set the environment variables listed in the table below.

### If client sets it up

Send them these steps:
1. Create a Railway account at [railway.app](https://railway.app).
2. Add the dev team member's GitHub account as a **Member** on the Railway project (Settings → Members).
3. Share the Railway project URL so dev can configure environment variables.

### Environment variables to configure

Set these in Railway → your service → **Variables**:

| Variable | Example value | Notes |
|----------|--------------|-------|
| `Jwt__Secret` | `a-random-string-min-32-chars` | Generate with: `openssl rand -base64 48` |
| `AdminSeed__Email` | `admin@yourstore.com` | First admin account email |
| `AdminSeed__Password` | `StrongPassword@2026!` | Change immediately after first login |
| `Cors__AllowedOrigins__0` | `https://yourstore.com` | Frontend domain (add more with `__1`, `__2`, …) |
| `Frontend__BaseUrl` | `https://yourstore.com` | Used in password-reset email links |
| `Resend__ApiKey` | `re_xxxxxxxxxxxx` | From Resend dashboard (see §2 below) |
| `Email__FromAddress` | `GharCraft <noreply@yourstore.com>` | Must match a verified Resend domain |
| `Email__AppName` | `GharCraft` | Appears in email subject lines |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Already set in Dockerfile; override here if needed |

> `DATABASE_URL` is injected automatically by Railway — do **not** set it manually.

---

## 2. Resend — Transactional email

**Who owns it:** **Client** — because the sending domain (`noreply@yourstore.com`) must be a domain the client owns. Dev team cannot verify someone else's domain.

**What it's used for right now:**
- Password reset emails

**What it will be used for later:**
- Order confirmation emails (Phase 1)

**Free tier:** 100 emails/day, 3,000/month — sufficient for launch. Paid plans start at $20/month.

### Setup steps (client does this, takes ~15 minutes)

**Step 1 — Create a Resend account**

1. Go to [resend.com](https://resend.com) and click **Sign up**.
2. Sign up with your business email (e.g. `owner@yourstore.com`).
3. Verify your email address.

**Step 2 — Add and verify your sending domain**

> You need access to your domain's DNS settings (usually in Cloudflare, GoDaddy, or your registrar).

1. In the Resend dashboard, go to **Domains → Add Domain**.
2. Enter your store domain (e.g. `yourstore.com`).
3. Resend shows you 3 DNS records to add (two `TXT` records + one `MX` record).
4. Add these records in your domain registrar or Cloudflare.
5. Click **Verify** in Resend — DNS propagation takes 5–30 minutes.
6. Once verified, the domain shows a green ✅.

**Step 3 — Create an API key**

1. In Resend, go to **API Keys → Create API Key**.
2. Name it `GharCraft Production`.
3. Permission: **Sending access** (not full access).
4. Copy the key — it starts with `re_`. **You will only see it once.**
5. Immediately add it to Railway as `Resend__ApiKey` (see §1 above).

**Step 4 — Tell the dev team**

Share these values (via Railway directly, or a secure channel):
- The API key (`re_...`)
- The `From` address you want: e.g. `GharCraft <noreply@yourstore.com>`

### How dev team verifies it's working

Once the key is set in Railway, hit the forgot-password endpoint with a registered email. If Resend is configured correctly, the email arrives within seconds. The Resend dashboard → **Emails** tab shows the delivery log.

---

## 3. Cloudflare Pages — Frontend hosting

> 🔜 **Phase 1 — not set up yet**

**Who owns it:** Client (or dev team — Cloudflare Pages is free for unlimited bandwidth).

- Used to host the React/Vite frontend.
- Cloudflare automatically provides SSL and a global CDN.
- Connects to GitHub for auto-deploy on push.

*Setup instructions will be added when frontend development begins.*

---

## 4. Cloudflare DNS — Domain & SSL

> 🔜 **Phase 1 — not set up yet**

**Who owns it:** Client (they own the domain).

- Transfer nameservers to Cloudflare (or add the site if already on Cloudflare).
- Routes `api.yourstore.com` → Railway, `yourstore.com` → Cloudflare Pages.
- SSL is automatic and free.

*Setup instructions will be added when domain is finalised.*

---

## 5. Razorpay — Payment gateway

> 🔜 **Phase 1 — not set up yet**

**Who owns it:** **Client only.** Razorpay requires a verified Indian business entity (GST number, bank account, PAN). Dev team cannot create this on the client's behalf.

**Action required early:** Start the Razorpay KYC process as soon as possible — it takes 2–5 business days and blocks the payment feature.

| Credential | Where it goes |
|-----------|--------------|
| Razorpay Key ID | `Razorpay__KeyId` in Railway |
| Razorpay Key Secret | `Razorpay__KeySecret` in Railway |
| Webhook Secret | `Razorpay__WebhookSecret` in Railway |

*Detailed setup steps will be added when payments are implemented.*

---

## 6. Cloudflare R2 — Image storage

> 🔜 **Phase 1 — not set up yet**

**Who owns it:** Client (lives inside their Cloudflare account).

- Free tier: 10 GB storage, 1 million read operations/month.
- Used to store product photos in multiple resolutions.
- Accessed via an S3-compatible API from the backend.

*Setup instructions will be added when the image upload feature is implemented.*

---

## 7. UptimeRobot — Monitoring

> 🔜 **Launch checklist item**

**Who owns it:** Dev team (or client — free plan covers 50 monitors at 5-minute intervals).

- Monitors `/healthz` on the Railway URL.
- Sends an alert (email/SMS) if the API goes down.
- Free at [uptimerobot.com](https://uptimerobot.com).

---

## Checklist — before going live

- [ ] Railway: all environment variables set (§1)
- [ ] Resend: domain verified, API key in Railway (§2)
- [ ] Razorpay: KYC approved, test mode working, webhook verified (§5)
- [ ] Cloudflare: DNS pointing to Railway API and Cloudflare Pages (§3, §4)
- [ ] R2: bucket created, IAM token generated, test upload working (§6)
- [ ] UptimeRobot: monitor live and alert email confirmed (§7)
- [ ] Real ₹1 transaction in Razorpay live mode, then refunded
