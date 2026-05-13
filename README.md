# OAuthDemoLeap

An ASP.NET Core 8.0 Web API demonstrating the OAuth 2.0 Authorization Code flow with PKCE.

## Prerequisites

- .NET 8.0 SDK
- A registered client at `auth.dev.leap.services` with:
  - Client ID: `PVLUM9TIKCASF2BG`
  - Redirect URI: `https://localhost:4200/callback`

## Setup

1. Clone the repository.
2. `appsettings.json` contains the `OAuthConfiguration` values sourced from the IdP's OpenID Connect discovery document at `https://auth.dev.leap.services/.well-known/openid-configuration`.

## How to run

```bash
dotnet run --launch-profile https
```

Then open `https://localhost:4200/login` in a browser to start the login flow. Swagger UI is disabled.

## Endpoints

| Method | Path        | Description                                                            |
| ------ | ----------- | ---------------------------------------------------------------------- |
| GET    | `/login`    | Starts the OAuth flow, generates state and pkce, redirects user to IdP |
| GET    | `/callback` | Handles IdP redirect, exchanges code for tokens, validates token       |
| GET    | `/me`       | Returns claims from the id_token                                       |
| GET    | `/api/data` | Protected resource requiring valid access token                        |
| GET    | `/logout`   | Clears server session and redirects to IdP logout endpoint             |

## How to test

1. **Start the login flow**
   - Open `https://localhost:4200/login` in a browser
   - You will be redirected to the IdP login page if you've not logged in
   - Enter your credentials and log in

2. **Complete the callback**
   - After login, the IdP redirects back to `/callback` — you will see `code` and `state` parameters in the URL (e.g. `/callback?code=...&state=...`)
   - The server exchanges the authorization code for tokens and validates the id_token
   - You will see a `200 OK` response if successful

3. **View user claims**
   - Navigate to `https://localhost:4200/me`
   - Returns a JSON object of claims decoded from the id_token (e.g. `sub`, `email`, `firstName`)

4. **Access protected resource**
   - Navigate to `https://localhost:4200/api/data`
   - Returns `200 OK` with `"You're authorized"` if the access token is valid
   - Returns `401 Unauthorized` with `"You're unauthorized to access the resouces"` if not logged in or the token has expired

5. **Logout**
   - Navigate to `https://localhost:4200/logout`
   - Clears the server session and redirects to the IdP logout endpoint

## Design Choices

### State Parameter

A cryptographically random state value is generated per login request and stored in the server-side session. It is verified in `/callback` to prevent CSRF attacks.

### Token Validation

The id_token is validated in `/callback` using `Microsoft.IdentityModel.JsonWebTokens`. Validation checks:

- Signature (against JWKS fetched from the IdP)
- Issuer
- Audience
- Token lifetime (expiry)

This is the only third-party library used, for token validation which would be unsafe to implement manually.

### Token Storage

Tokens are stored exclusively in **server-side session** (backed by `IDistributedMemoryCache`). The browser only receives a session cookie (HttpOnly, not accessible to JavaScript).

**Why:** If tokens are stored in the browser (such as localStorage or sessionStorage), malicious JavaScript injected through an XSS attack may be able to access and steal them. Storing tokens on the server side is safer because the frontend never has direct access to the tokens.

## Trade-offs

- **In-memory cache only:** The default `IDistributedMemoryCache` is per-process. Tokens are lost on server restart and this does not work across multiple instances. This was chosen for simplicity in a demo context, as configuring a distributed cache (e.g., Redis) or external database is out of scope. A production deployment would replace this with a distributed cache.
- **No token refresh:** The access token is stored but its expiry is not tracked. Once expired, the user must log in again. The IdP did not return a refresh token in the token response, so automatic token renewal is not possible. A production implementation would request the `offline_access` scope to obtain a refresh token, and exchange it for new tokens when the access token expires.
- **Logout implementation:** The IdP's discovery document exposes an `end_session_endpoint`, and since a complete authentication lifecycle should include logout, a `/logout` endpoint was implemented. It clears the server session and redirects to the IdP's `end_session_endpoint` with `id_token_hint` to terminate the IdP session. Without terminating the IdP session, the IdP would silently re-authenticate the user on the next `/login` visit.
- **Full claims exposure:** `/me` returns all claims from the id_token directly. In a production environment, a DTO would be used to expose only the fields the application needs (e.g. `sub`, `email`, `firstName`), avoiding accidental exposure of internal claims. This was kept as-is for development visibility.
- **Detailed error responses:** Error responses include specific messages (e.g. `"Invalid state"`, `"Session expired"`) to aid development and demonstration. In a production environment, these would be replaced with generic messages to avoid leaking implementation details to potential attackers.
- **Claims from id_token:** `/me` returns claims decoded from the id_token
  rather than calling the UserInfo endpoint. This avoids an additional
  network request on every `/me` call. The trade-off is that claims reflect the user's profile at login time. A production implementation with stricter freshness
  requirements would call the UserInfo endpoint instead.
  - **No unit tests:** The service layer is either thin wrappers over HTTP calls (`TokenExchangeService`) or delegates entirely to a third-party library (`TokenValidationService`). The most testable logic is in the controller, but mocking `HttpContext.Session` adds more boilerplate than the logic being tested. The OAuth flow was verified manually end-to-end.
