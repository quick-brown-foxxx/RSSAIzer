# Specification: Add Config-Driven Authentication (Single-User / Reverse-Proxy / OpenID)

This spec converts our architectural discussion into an actionable, step-by-step checklist for an AI developer.
It assumes the current code base of **RSSAIzer**:

- ASP.NET Core 9, Razor Pages
- Entity Framework Core with repositories & POCO segregation
- Options pattern (`Backend/Options/*.cs`) using `ConfigurationKeyName` attributes
- All business entities live in `RSSAIzer.Backend.Db` and the EF `ApplicationDbContext` inherits **`DbContext`**
- Application is presently single-user; no Identity / authentication code exists.

---

## Common Section (Background & Design Goals)

1. **Dual-Mode Auth**

   - `SingleUserMode = true` → dummy local authentication (no IdP, no proxy).
   - `SingleUserMode = false` → either
     - Reverse proxy passes user claims via headers **or**
     - Full OpenID Connect (code flow).

2. **Minimal Personally Stored Data**

   - Only `UserId` (Guid) & `Email` are persisted; no phone, profile, etc.

3. **Ownership Model**

   - All tenant-scoped tables implement `IUserOwnedEntity { Guid UserId { get; set; } }`.

4. **Configuration**

   - New `AuthOptions` record placed in `Web/Options` with env-var mapping.
   - `.env` / Kubernetes secrets control behaviour; no code changes are required to toggle modes.

5. **Handlers**

   - `SingleUserAuthHandler` (single-user)
   - `ProxyHeaderHandler` (reverse-proxy headers)
   - `OpenIdConnect` (built-in) when Authority is provided.

6. **Verification**

   - Unit tests ensure repositories filter by `CurrentUserId`.
   - `dotnet build` and `dotnet test` succeed.
   - Run `dotnet csharpier .` on solution to ensure code style is consistent.

7. **Adding migrations**
   ```bash
   dotnet ef migrations add MIGRATIONNAME \
   --project RSSAIzer.Backend/RSSAIzer.Backend.csproj \
   --startup-project RSSAIzer.Web/RSSAIzer.Web.csproj
   ```

---

## Steps Section (One Deliverable per Step)

### ✅ Step 1 – Add `AuthOptions`

_Prerequisite_: none
_Tasks_

- [ ] Create `Web/Options/AuthOptions.cs` record mirroring the pattern in [BackendDeploymentOptions](file:///Users/user1/Projects/RSSAIzer/RSSAIzer.Backend/Options/BackendDeploymentOptions.cs).
- [ ] Include fields: `SingleUserMode`, `Authority`, `ClientId`, `ClientSecret`, `ProxyHeaderEmail`, `ProxyHeaderId`, `CookieName`.
- [ ] Add `AuthConsistencyAttribute` to validate the options.
- [ ] Bind `AuthOptions` in `Program.cs`, following patterns of other options.
      _Verification_
- [ ] Read the file and ensure it is well-formatted.
- [ ] `dotnet build` succeeds.

---

### ✅ Step 2 – Introduce ASP.NET Identity Core

_Prerequisite_: Step 1
_Tasks_

- [ ] Add NuGet: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` to `Backend/Backend.csproj`.
- [ ] Create `Infrastructure/Identity/ApplicationUser : IdentityUser<Guid>` (no overrides).
- [ ] Change `ApplicationDbContext` base class to `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and keep existing `DbSet`s.
- [ ] Migration **AddIdentity**; update DB.
      _Verification_
- [ ] Read all modified files and ensure they are well-formatted.
- [ ] `dotnet build` succeeds.
- [ ] `dotnet ef migrations bundle` succeeds;

---

### ✅ Step 3 – Implement `IUserOwnedEntity` & Add `UserId` columns

_Prerequisite_: Step 2
_Tasks_

- [ ] Create `IUserOwnedEntity` interface to [Entities.cs](file:///Users/user1/Projects/RSSAIzer/RSSAIzer.Backend/Db/Entities.cs).
- [ ] Implement it in `FeedEntity`, `DigestEntity`, `PostSummaryEntity`, `SettingsEntity`.
- [ ] Add `UserId` (Guid, non-nullable) + `ApplicationUser? UserNav` to those entities.
- [ ] Configure FKs in `OnModelCreating`.
- [ ] Migration **AddUserOwnership** with default `Guid.Empty`.
      _Verification_
- [ ] Read all modified files and ensure they are well-formatted.
- [ ] `dotnet build` and `dotnet test` succeed.

---

### ✅ Step 4 – User Context & Repository Filtering

_Prerequisite_: Step 3
_Tasks_

- [ ] Create `BackendAuthenticationConfiguration` in `Backend/Infrastructure`, register and populate in `Web/Program.cs`.
- [ ] Create `ICurrentUserContext` + implementation reading `HttpContext.User`.
- [ ] Inject into all repositories; apply `Where(e => e.UserId == current)` for GET/UPDATE queries.
- [ ] In single-user mode, return `Guid.Empty`.
- [ ] Write unit tests for all repositories.
      _Verification_
- [ ] Read all modified files and ensure they are well-formatted.
- [ ] `dotnet build` and `dotnet test` succeed.
- [ ] Unit tests confirm users cannot access others’ data.

---

### ✅ Step 5 – Configure Authentication Pipelines in [Program.cs](file:///Users/user1/Projects/RSSAIzer/RSSAIzer.Backend/Program.cs)

_Prerequisite_: Steps 1-4
_Tasks_

- [ ] Add `AddAuthentication`/`AddAuthorization` as described:
  - single-user → add `SingleUser` scheme & auto-sign-in.
  - authority set → `OpenIdConnect`.
  - else → `ProxyHeader` scheme.
- [ ] Optional cookie naming from options.
      _Verification_
- [ ] Read all modified files and ensure they are well-formatted.
- [ ] `dotnet build` and `dotnet test` succeed.
- [ ] Unit tests confirm users cannot access others’ data.
- [ ] Running with each env set yields correct `AuthenticationSchemeProvider.DefaultAuthenticateScheme`.

---

### ✅ Step 6 – Implement Custom Handlers

_Prerequisite_: Step 5
_Tasks_

- [ ] `SingleUserAuthHandler` – returns fixed `ClaimsPrincipal` (UserId = Guid.Empty, email = single@local).
- [ ] `ProxyHeaderHandler` – reads headers to build claims; reject if absent.
      _Verification_
- [ ] `dotnet build` and `dotnet test` succeed.
- [ ] Read all modified files and ensure they are well-formatted.

---

### ✅ Step 7 – UI: Login/Logout & Layout Indicators

_Prerequisite_: Step 6
_Tasks_

- [ ] Create `AuthController` with `Login` and `Logout` actions.
- [ ] Add `/Auth/Login` that calls `Challenge` (OIDC), cookie sign in (single-user), or no-op (reverse proxy).
- [ ] Add `/Auth/Logout` to sign out cookie/oidc/redirect to logout for reverse proxy.
- [ ] Update `_Layout.cshtml` to show user email & Login/Logout link (uses `@User.Identity.IsAuthenticated`).
      _Verification_
- [ ] Manual browser check by developer: link toggles and email appears.
- [ ] `dotnet build` and `dotnet test` succeed.
- [ ] Read all modified files and ensure they are well-formatted.

---

### ✅ Step 8 – Data Migration / Seeding Script

_Prerequisite_: Step 7
_Tasks_
none

_Verification_

- [ ] Make sure data migrated successfully.
- [ ] Make sure app works with empty DB.
- [ ] Make sure all modes work.

---

### ✅ Step 9 – Documentation & Environment Samples

_Tasks_

- [ ] Create `docs/authentication.md` summarising modes, env-vars, and reverse-proxy headers.
- [ ] Provide sample `.env` for each mode.
      _Verification_
- [ ] Tech-writer review;

---

## Deliverable Acceptance

A step is accepted when:

1. All checkboxes are completed.
2. Automated tests (unit + integration) pass.
3. Linter/formatters show no new issues.
4. Reviewer confirms tasks & verification criteria are met.
