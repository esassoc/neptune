# Auth0 artifacts — reference copies only

**Nothing in this folder deploys. The Auth0 dashboard is what actually runs.**

They are checked in so the Auth0 configuration this app cannot function without is *readable in the
repo* — reviewable in a diff — instead of existing only inside the `ocstormwatertools` tenant. Change
the dashboard first, then update the file here in the same PR. When something authentication-shaped
behaves oddly, do not trust these files: open the dashboard and compare.

The federation runbook — Entra connection, claims, account linking, domain routing — is
[`docs/ocpw-county-sso.md`](../../docs/ocpw-county-sso.md). Read it before touching the Action.

## Two kinds of sharing, both of which make edits riskier than they look

- **One Action serves two products.** OC Stormwater Tools (this app) and OC Smart Watershed Network
  (`esassoc/nebula`) share it. There is no way to change one without changing the other, so there is
  no staging one and watching it.
- **One tenant serves dev, QA and prod.** Any edit is immediately live in all three, for
  email/password users as well as County users.

Verify a database login *and* a County login straight after any change. Auth0 Actions keep version
history, so a revert is available.

## `post-login-action.js`

Writes the collected names to `user_metadata`, applies the email-verification wall to database signups
only, denies emailless federated logins, sets the `email` / `given_name` / `family_name` claims on the
**access token**, and renders the name form (`ap_fM32bSgX3ifwS7bqyp5pey`) for database signups that
still lack a name.

`name` stays deliberately unforwarded: `People.UpdateClaims` ignores it and Entra sends it as
"Last, First".

## Why the Action does the write, and not the form

**Changed in the tenant on 2026-08-19:** the Action now writes `user_metadata` itself and the form's
`UPDATE_USER` flow node has been removed. The form is kept — it collects the two fields and resumes the
auth flow, which is all it should ever have done.

The history is worth keeping, because the failure is invisible and someone will be tempted to move the
write back into a flow.

`onContinuePostLogin` used to read the names out of `user_metadata` and trust the form's own flow to
have written them. In the Biochar Atlas tenant that exact arrangement — correctly configured, with the
submitted names visible in the flow execution's recorded *input* — wrote nothing across four
consecutive signups. Both tells were absences: empty `artifacts` in the flow output, and no
`API Operation: Update a User` in the tenant log. `Flows Execution Completed` means the flow ran, not
that it did anything. Forms and flows also publish separately, so an unpublished change there fails
silently too.

**The sequencing mattered here more than elsewhere.** The old continuation read names *only* from
`user_metadata`, so removing the flow node first would have left nothing to read and broken name
collection outright. The Action had to learn the write before the flow lost the job.

## Confirming it works, and the row that suggested it did not

A real signup is the proof: the tenant log should carry `API Operation: Update a User`, the user's
`user_metadata` should hold `first_name` and `last_name`, and signing out and back in should not show
the form again.

This query finds accounts whose names never arrived. `Person` 1446 — a database signup from
2026-03-05, still active in May, with `FirstName` and `LastName` both `NULL` — is what prompted the
fix:

```sql
SELECT PersonID, Email, FirstName, LastName, CreateDate, LastActivityDate
FROM dbo.Person
WHERE (FirstName IS NULL OR FirstName = '' OR LastName IS NULL OR LastName = '')
  AND GlobalID IS NOT NULL          -- has signed in through Auth0
ORDER BY CreateDate DESC;
```

Anyone in that list heals themselves on their next login: the Action re-renders the form whenever a
name is missing, and now persists what it collects. `People.UpdateClaims` overwrites a stored name
whenever a claim carries one.

Names on **County/Entra** users prove nothing about this — they come from the root profile via Graph
userinfo and never depended on the form.

## `update-names-form.json` — not here yet

Needs an export from the tenant: Forms → the name form → Export, top-right of the form editor. With the
flow node removed it should now show `"flows": {}` and `"connections": {}` — that is what a corrected
form looks like, and **an export carrying a flow node is a regression, not a richer form.**

The field ids must stay `first_name` and `last_name`: the Action reads the submission by exactly those
keys and falls back silently to empty names if they are renamed.
