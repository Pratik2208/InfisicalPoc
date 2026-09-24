# Infisical + .NET Web API — Reachability POC

This is a minimal .NET 8 Web API plus a GitHub Actions workflow that proves
secrets stored in your Infisical Cloud project are reachable at runtime by
the app, without any secrets ever being committed to the repo.

## What's inside

```
InfisicalPoc/
├── .github/workflows/verify-infisical-secrets.yml   # CI workflow
├── src/InfisicalPoc.Api/
│   ├── Program.cs             # minimal API with /api/secret-check
│   ├── appsettings.json
│   ├── Properties/launchSettings.json
│   └── InfisicalPoc.Api.csproj
└── .gitignore
```

The API exposes:
- `GET /` — liveness check
- `GET /api/secret-check` — looks up a config key (default `DATABASE_PASSWORD`)
  via `IConfiguration` (which reads environment variables automatically) and
  reports whether it was found and how long the value is. It never echoes the
  actual secret value.

## One-time setup in Infisical

1. Confirm you already have a secret in your project (e.g. `DATABASE_PASSWORD`)
   under some environment, e.g. `dev`.
2. Create a **Machine Identity**: Org Settings → Machine Identities → Create.
   - Set auth method to **Universal Auth**.
   - Attach it to your project with read access to the `dev` environment.
   - Copy the generated **Client ID** and **Client Secret**.
3. Note your project's **slug** (Project Settings) — you'll put this in the workflow.

## One-time setup in GitHub

1. Push this repo to GitHub.
2. Go to **Settings → Secrets and variables → Actions → New repository secret**
   and add:
   - `INFISICAL_CLIENT_ID`
   - `INFISICAL_CLIENT_SECRET`
3. Edit `.github/workflows/verify-infisical-secrets.yml` and replace:
   - `env-slug: "dev"` with your actual environment slug
   - `project-slug: "your-project-slug"` with your actual project slug
4. If your secret isn't named `DATABASE_PASSWORD`, either rename it in Infisical,
   or set an additional GitHub secret / workflow env var `SECRET_KEY_NAME`
   pointing at whatever key you actually created, and pass it through in the
   workflow's "Run API in background" step, e.g.:
   ```yaml
   env:
     SECRET_KEY_NAME: ${{ secrets.SECRET_KEY_NAME }}
   ```

## Running the check

- Push to `main`, or trigger manually from the **Actions** tab
  ("verify-infisical-secrets" → **Run workflow**).
- The job will:
  1. Authenticate to Infisical with the machine identity and inject secrets
     as environment variables into the job.
  2. Build the .NET API.
  3. Run it in the background.
  4. Curl `/api/secret-check` and fail the job if the secret wasn't found.

## Running locally (optional)

```bash
export DATABASE_PASSWORD="whatever-value-for-local-testing"
cd src/InfisicalPoc.Api
dotnet run
curl http://localhost:5080/api/secret-check
```

(For real local dev, you'd typically use the Infisical CLI —
`infisical run -- dotnet run` — to inject secrets locally instead of
hardcoding them, but that's outside the scope of this POC.)

## Next steps beyond this POC

- Swap Universal Auth for **OIDC auth** in the workflow so no long-lived
  credentials are stored in GitHub Secrets at all.
- Add the `Infisical.Sdk` NuGet package to the API itself if you want the app
  to pull secrets directly from Infisical at startup/runtime rather than
  relying on CI to inject them as env vars (useful for non-CI environments
  like a container running in production).
