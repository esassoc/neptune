/**
 * post-login Action — ocstormwatertools tenant
 * =============================================================================
 * REFERENCE COPY. Nothing deploys this file. What actually runs is the Action in
 * the Auth0 dashboard (Actions -> Triggers -> post-login). Edit there first, then
 * update this copy. See Build/auth0/README.md and docs/ocpw-county-sso.md.
 *
 * SHARED TENANT, and the sharing runs two ways:
 *   - one Action serves BOTH OC Stormwater Tools (this app) and OC Smart Watershed
 *     Network (esassoc/nebula). An edit here changes both products.
 *   - one tenant serves dev, QA and prod. Any edit is immediately live in all
 *     three. Verify a database login AND a County login straight after.
 * Auth0 Actions keep version history, so a revert is available.
 * =============================================================================
 *
 * Three things it does, and the first is the one that was missing until 2026-08-19:
 *
 *   1. THE ACTION WRITES user_metadata. The form only collects. The form's own
 *      UPDATE_USER flow step used to do this write, and that step can fail with no
 *      error anywhere — see Build/auth0/README.md for how it presents.
 *   2. The verification wall, scoped to database connections only. Federated users
 *      have NO email_verified field at all, so an unscoped wall denies every County
 *      login. That was the original defect this Action was consolidated to fix.
 *   3. An emailless-federated guard, because People.UpdateClaims relinks by email:
 *      an emailless federated login would create a duplicate Unassigned Person and
 *      strand the real one.
 *
 * api.prompt.render() SUSPENDS the login. Auth0 resumes at onContinuePostLogin, not
 * here, so the claims set before the render are discarded and the continuation sets
 * them again.
 *
 * NOT a secret: a Form id is a tenant-scoped resource identifier, like a client id.
 */

const FORM_ID = "ap_fM32bSgX3ifwS7bqyp5pey";

/** Names we already hold: root profile (Entra via Graph userinfo) then user_metadata. */
const existingNames = (event) => {
  const md = event.user.user_metadata || {};
  return {
    givenName: event.user.given_name || md.first_name || "",
    familyName: event.user.family_name || md.last_name || "",
  };
};

const setClaims = (event, api, givenName, familyName) => {
  // Plain claim names — ASP.NET's default inbound map converts these to the
  // .../emailaddress, .../givenname and .../surname URIs in ClaimsConstants.cs.
  api.accessToken.setCustomClaim("email", event.user.email);
  api.accessToken.setCustomClaim("given_name", givenName || "");
  api.accessToken.setCustomClaim("family_name", familyName || "");
};

exports.onExecutePostLogin = async (event, api) => {
  const isDatabaseUser = event.connection.strategy === "auth0";

  // Email-verification wall — database signups only.
  if (isDatabaseUser && !event.user.email_verified) {
    api.access.deny(
      `Thanks! We sent a verification link to ${event.user.email}. Please click it, then sign in.`
    );
    return;
  }

  // Insurance against an emailless federated login creating a duplicate account.
  // Message is app-neutral on purpose: this tenant serves two apps, and telling a
  // Smart Watershed Network user to contact Stormwater Tools support is a dead end.
  if (!isDatabaseUser && !event.user.email) {
    api.access.deny("Your account is missing an email address. Please contact support.");
    return;
  }

  const { givenName, familyName } = existingNames(event);

  setClaims(event, api, givenName, familyName);

  // Name form: database signups that still lack names. render() SUSPENDS the login
  // and Auth0 resumes at onContinuePostLogin — the claims set just above are
  // discarded on that path, which is why the continuation sets them again.
  if (isDatabaseUser && (!givenName || !familyName)) {
    api.prompt.render(FORM_ID);
  }
};

exports.onContinuePostLogin = async (event, api) => {
  // Both shapes kept deliberately: one of them resolves, and Auth0's documented
  // shape has moved before.
  const submitted = event.prompt?.fields ?? event.prompt?.form?.fields ?? {};
  const existing = existingNames(event);

  const givenName = submitted.first_name || existing.givenName;
  const familyName = submitted.last_name || existing.familyName;

  // THE WRITE. This is what the form's flow was being trusted to do.
  if (givenName) api.user.setUserMetadata("first_name", givenName);
  if (familyName) api.user.setUserMetadata("last_name", familyName);

  // No wall here: this path is only reachable for database users who already
  // passed it in onExecutePostLogin, before the render.
  setClaims(event, api, givenName, familyName);
};
