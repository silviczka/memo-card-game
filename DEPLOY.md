# Deployment

This project can be deployed in two simple ways.

## Option 1: One host for everything

Deploy `MemoCardGame.Api` to one host. It serves:

- the REST API
- the Blazor WebAssembly client

This is the simplest setup.
Typical choices for this setup are Koyeb, Render, or Railway.

### Config

Keep `ApiBaseUrl` empty in:

- `src/MemoCardGame.Client/wwwroot/appsettings.json`
- `src/MemoCardGame.Client/wwwroot/appsettings.Production.json`

That makes the client call the same origin as the page.

### Example

Koyeb can build and run the root `Dockerfile`.

## Option 2: Static client + separate API

Use this when you want:

- the client on a static host
- the API on another host

This works well for Blazor WebAssembly because the published client is a set of static files.

Typical choices:

- Client host: Vercel, Netlify, Cloudflare Pages
- API host: Koyeb, Render, Railway

### Steps

1. Deploy the API and note its public URL.
2. Set `ApiBaseUrl` in `src/MemoCardGame.Client/wwwroot/appsettings.Production.json` to that API URL.
3. Set `Cors:AllowedOrigins` in `src/MemoCardGame.Api/appsettings.Production.json` to your frontend URL.
4. Publish the client:

```bash
dotnet publish src/MemoCardGame.Client -c Release
```

Output:

`src/MemoCardGame.Client/bin/Release/net8.0/publish/wwwroot/`

5. Deploy the contents of that `wwwroot/` folder to your static host.

For example, on Vercel you deploy the published frontend output and Vercel serves it as a static site.

## PostgreSQL / Neon

You only need Neon (or another PostgreSQL host) if you want PostgreSQL instead of SQLite.
PostgreSQL is usually a better choice for production because it handles concurrent users better, fits remote hosting more naturally, and avoids the common SQLite-on-container problem where local database files may not persist across redeploys unless you add persistent storage.

Set `ConnectionStrings__Default` on the API service environment.
If it is missing, the app uses SQLite instead.

Example:

```text
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

## GitHub vs live site

Pushing to GitHub updates the code in the repository.
It does **not** automatically change the live app unless your host deploys that new commit.

### If you use Option 1

- Push to GitHub
- Redeploy the API service, or let your API host auto-deploy from Git

Many API hosts such as Koyeb, Render, and Railway can watch your GitHub repository and start a new deployment automatically after a push. In that setup, GitHub Actions are optional.

Even frontend-only changes need a redeploy here, because the client files are included in the API app.

### If you use Option 2

- Push to GitHub
- Republish and redeploy the client when UI changes
  For example, if your client is on Vercel and auto-deploy from Git is enabled, a new push can trigger that deploy automatically. Users see the new UI after Vercel finishes that deployment.
- Redeploy the API only when backend code, CORS, or environment settings change

The same idea applies to the API host: if auto-deploy from Git is enabled, a push can trigger the API deployment automatically. Without auto-deploy, you need to redeploy the API manually.

## GitHub Actions (optional)

This repository does not currently include a GitHub Actions deployment workflow.

You only need GitHub Actions if you want a custom CI/CD pipeline. Common reasons are:

- run tests before deployment
- build artifacts in GitHub
- deploy through a provider CLI or API instead of the host's built-in Git integration

Typical setup:

1. Add a workflow file in `.github/workflows/`.
2. Trigger it on `push` to `main`.
3. Run `dotnet build` and `dotnet test`.
4. If successful, call the deployment step for your host.

If your host already supports auto-deploy from GitHub and that is enough for your needs, you can usually skip GitHub Actions.

### When do you need Neon changes?

Only when database configuration or schema changes.
You do **not** need Neon changes for CSS, Razor, or other frontend-only work.
