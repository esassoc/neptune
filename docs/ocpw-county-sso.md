# OCPW County SSO — Auth0 ↔ Microsoft Entra Federation

*Written August 2026 for NPT-1002. Satisfies AC 18 (document the claims Neptune requires from Entra for County IT). Audience: County IT, and future Neptune devs touching the login path.*

> **Deliberately not recorded here:** the OCPW Entra tenant ID, the issuer URL that embeds it, and any connection credentials. NPT-1002 AC 19 requires that no client secrets, tenant IDs or connection credentials be committed to the repo. Read them from the `OCPW` connection settings in the Auth0 dashboard, or from OCPW's published OIDC discovery document. Please don't helpfully paste them back in.

County staff sign in to OC Stormwater Tools with their existing Orange County Public Works Microsoft Entra credentials, instead of a Neptune-specific password. Email/password login remains the primary method for everyone else — jurisdiction staff, consultants, external viewers.

## Configuration summary

| | |
|---|---|
| Auth0 connection name | **`OCPW`** |
| Connection type | Auth0 **Enterprise → OpenID Connect** (generic OIDC, *not* the native Entra ID / Azure AD connection type) |
| Entra tenant / issuer | OCPW's Microsoft Entra tenant. Tenant ID and issuer URL are in the Auth0 `OCPW` connection settings — see the note above. |
| Login routing | Email-domain Home Realm Discovery — a user entering an `@pw.oc.gov` address is routed to Entra automatically. There is no "OCPW Staff" button. |
| Federated logout | Not propagated upstream (see [Sign-out behavior](#sign-out-behavior)) |

Auth0 user IDs for this connection are pipe-delimited — the strategy, the connection name, then the Entra subject:

```
oidc|OCPW|<sub>
```

**There is a single Auth0 tenant (`ocstormwatertools.us.auth0.com`) serving all environments.** `Neptune.API/Startup.cs` hardcodes `Authority` and `Audience` (`OCSTApi`) rather than reading them from configuration, so dev, QA and prod all validate against the same tenant. County IT therefore needs **one** Entra app registration, with redirect URIs covering every environment — not one per environment.

The tenant is US **Government Community Cloud (GCC)** (`tenant_region_sub_scope: "GCC"`) but sits on commercial `login.microsoftonline.com` with the commercial Graph userinfo endpoint — GCC moderate, not GCC High — so no special endpoint handling is required. GCC tenants often carry stricter conditional-access policies; if *some* County users fail while others succeed, look there before suspecting Neptune.

## Claims Neptune requires

Neptune validates the **access token**, and reads four claims:

| OIDC claim | Mapped .NET claim type | Used for |
|---|---|---|
| `sub` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | `Person.GlobalID` — the identity key |
| `email` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | `Person.Email`, **and account linking** (see below) |
| `given_name` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname` | `Person.FirstName` |
| `family_name` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname` | `Person.LastName` |

Those are the exact strings in `ClaimsConstants.cs`, given in full so they can be copied or grepped when troubleshooting a token.

`Neptune.Models/Helpers/ClaimsConstants.cs` uses the legacy WS-Federation claim URIs and relies on the JWT handler's **default inbound claim-type mapping**, which converts the plain OIDC names above into those URIs automatically. So the Auth0 action must set **plain** claim names (`email`, `given_name`, `family_name`) — namespaced custom claims are not read by anything.

### These claims already arrive — no Entra optional-claims change needed

OCPW's OIDC discovery document lists `claims_supported` **without** `given_name` or `family_name`. That describes the **ID token** only, and is misleading here: Auth0 also calls the userinfo endpoint (`https://graph.microsoft.com/oidc/userinfo`), which returns `given_name`, `family_name`, `email`, `name` and `picture`. Verified against a real County profile.

**Do not diagnose a missing-name problem from the discovery document.** Inspect the actual Auth0 user profile instead.

### `name` is deliberately not forwarded

Entra sends the combined display name as **"Last, First"** (e.g. `"Silva, Jasmine"`). `People.UpdateClaims` reads only givenname/surname and ignores `name`, so this is harmless today. Any future change that derives first/last from `name` must handle the comma form, or every County user's name will be reversed.

## The Post Login action

A **single** Auth0 Post Login action handles all connections. Do not split it back into multiple actions — the previous three-step flow is what broke federated login.

Two behaviors are gated on connection type via `event.connection.strategy === "auth0"` (i.e. "is this a database user?"). The gate is deliberately written as *include database* rather than *exclude Entra/Okta*, so it needs no change when another IdP is added:

1. **Email-verification wall — database signups only.** Federated users have **no `email_verified` field at all** in their Auth0 profile (absent, not `false`), because Auth0 only sets it when the upstream IdP asserts it and Entra does not. Applying a verification check to them denies every County login. This was the original defect: a standalone marketplace "Require Email Verification" action in the flow blocked all federated users.
2. **Name-collection form** (`ap_fM32bSgX3ifwS7bqyp5pey`) — rendered only for database signups still missing a first or last name. County users get their names from Entra.

**The Action persists the collected names; the form does not.** As of 2026-08-19 the Action writes `first_name`/`last_name` to `user_metadata` itself, and the form's `UPDATE_USER` flow node has been removed. That node used to do the write, and it is capable of writing nothing while every layer reports success — the tells are absences, not errors (empty `artifacts` in the flow output, and no `API Operation: Update a User` in the tenant log). `Person` 1446, a March 2026 database signup with NULL names, is what that looks like in the data. Do not move this write back into a flow; `Build/auth0/README.md` has the full account.

There is also a guard denying any *federated* login that arrives with no `email` claim, with a message directing the user to support. Entra emits `email` only when the directory has `mail` populated; without it, account linking silently creates a duplicate account (below), so failing loudly is preferable.

### Auth0 changes cannot be staged

Because one tenant serves every environment, **editing the Post Login action is immediately live in dev, QA and prod**, and it affects email/password users as well as County users. Treat any edit as a production change: verify a database login and a County login straight after. Auth0 Actions retain version history, so a revert is available.

## Account linking — how existing users keep their access

`People.UpdateClaims` (`Neptune.EFModels/Entities/People.cs`) resolves the caller in this order:

1. Match `Person.GlobalID` to the token's `sub`.
2. **If that misses, match `Person.Email` to the token's `email`** — then overwrite `GlobalID` with the new `sub`.
3. If both miss, create a new `Person` with `RoleID = Unassigned`.

Step 2 is what makes the County cutover work: an existing County user signing in through Entra for the first time is found by email and **keeps their PersonID, Role, StormwaterJurisdiction, Organization and flags**. Users who were pre-created by an admin but never logged in are also picked up this way.

### The mismatch case — the one real failure mode

If the Entra-asserted email does **not** equal the user's stored `Person.Email`, step 2 misses and they get a **new Unassigned account**, while their original account is stranded. There is no admin-visible merge path.

County users' Auth0 database accounts have been deleted, so there is no password fallback for a user in this state — the mismatch must be fixed by an administrator.

Detect it with:

```sql
SELECT PersonID, FirstName, LastName, Email, GlobalID, CreateDate
FROM dbo.Person
WHERE Email LIKE '%@pw.oc.gov' AND RoleID = 3  -- Unassigned
ORDER BY CreateDate DESC;
```

Anything appearing there after a County user's first sign-in is a stranded user. Resolve by moving the Role and jurisdiction assignments to the new row, or by re-pointing `GlobalID` on the original row and deleting the duplicate.

Expected volume is low: at cutover all County users were on the single `pw.oc.gov` domain with no mixed consultant or personal addresses. But local-part formats do vary (surnames with spaces or hyphens, numeric disambiguators for duplicate names), so a handful of mismatches is plausible.

## Sign-out behavior

Neptune sign-out clears the Neptune and Auth0 sessions but **does not propagate upstream to Entra**. A County user who signs out and signs in again is returned straight to Neptune with no credential prompt, because their County session is still valid.

This is intended behavior, not a defect. Expect it to be reported as "sign-out isn't working."

## Consequences of domain-based routing

Because an `@pw.oc.gov` address is always routed to Entra, County staff cannot use a Neptune password with their County email. Two implications:

- **The OCPW Entra directory is the authority on who gets in.** County users' Auth0 database accounts have been deleted, so a `pw.oc.gov` user who is not in the directory has no way to authenticate at all. **This is accepted by design** — OCPW chose Entra as the identity source, so directory membership is their decision to manage, not something Neptune works around. Neptune deliberately does not maintain a parallel password path for County staff.

  The practical consequence is a support one: a locked-out County user must be **added to the OCPW directory by County IT**. There is no Neptune-side remedy — no password reset will help, because the account no longer exists. Support should route these to County IT rather than treating them as Neptune access issues.
- The `GlobalID` ping-pong described in NPT-1002's Technical Notes — where alternating between password and SSO login rewrites `GlobalID` each time — no longer occurs in practice, since only one path is reachable for a County address.

### Orphaned `Person` rows

A County user who is not in the directory leaves an active `Person` row that can never be authenticated against. These are harmless operationally, but two things are worth knowing:

- They still appear in User Management and still receive any notification emails their flags enable (`ReceiveSupportEmails`, `ReceiveRSBRevisionRequestEmails`). Setting `IsActive = false` is reasonable housekeeping once a row is known to be orphaned.
- **Email reuse is a role-inheritance path.** Account linking matches on email, so if County IT ever reassigns a `pw.oc.gov` address to a different person, that person would sign in and inherit the orphaned row's Role and jurisdiction assignments. Low probability — the directory appears to disambiguate duplicates with numeric suffixes — but it is a reason to deactivate or clear the Role on rows known to be abandoned rather than leaving them indefinitely.

## For County IT — checklist

1. One Entra app registration, with redirect/callback URIs for every Neptune environment (single Auth0 tenant).
2. Grant the `openid`, `profile` and `email` scopes.
3. Ensure each County user's directory `mail` attribute is populated, so the `email` claim is emitted — Neptune's account linking depends on it, and the address must match the user's existing Neptune email.
4. No optional claims configuration is required for `given_name` / `family_name`; Graph userinfo supplies them.
5. Notify Neptune admins before recreating the app registration. `subject_types_supported` is `pairwise`, so every user's `sub` would change and all County users would re-enter through the email-match path.
