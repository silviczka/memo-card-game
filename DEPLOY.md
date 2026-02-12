# Deployment

This app is **Blazor WebAssembly + .NET API**. Two deployment patterns:

---

## Option 1: Single host (easiest)

Run the API project; it serves both the API and the Blazor client. Deploy that one project to any host that runs .NET.

**Suggested hosts (free tiers):**

- **Railway** — Connect GitHub, select the repo, set root to `src/MemoCardGame.Api` (or use the Dockerfile from repo root). Add a start command: `dotnet run --project src/MemoCardGame.Api` (or let it use the Dockerfile).
- **Fly.io** — `fly launch` in repo root (use the existing Dockerfile), or deploy the API folder.
- **Render** — New Web Service, connect repo, build: `dotnet publish src/MemoCardGame.Api -c Release -o out`, run: `./out/MemoCardGame.Api`.

No code or config changes needed; the API and client are on the same origin.

---

## Option 2: Frontend on Vercel, API elsewhere (like “React + Vercel”)

Blazor WASM builds to **static files** (HTML, JS, CSS). Vercel can serve them like a React SPA.

1. **Deploy the API** to Railway / Fly.io / Render (see above). Note the API URL (e.g. `https://your-app.railway.app`).

2. **Point the client at that API:**  
   In `src/MemoCardGame.Client/wwwroot/`, add or edit `appsettings.Production.json`:
   ```json
   { "ApiBaseUrl": "https://your-api-url.railway.app" }
   ```
   (No trailing slash.)

3. **Build the client:**
   ```bash
   dotnet publish src/MemoCardGame.Client -c Release
   ```
   Output: `src/MemoCardGame.Client/bin/Release/net8.0/publish/wwwroot/`.

4. **Deploy to Vercel:**  
   Vercel’s default image doesn’t include .NET, so build the client on your machine (or in CI), then deploy the output as a static site:
   ```bash
   cd src/MemoCardGame.Client
   dotnet publish -c Release
   cd bin/Release/net8.0/publish/wwwroot
   npx vercel --prod
   ```
   Or: connect the repo to Vercel and use a GitHub Action (or similar) that runs `dotnet publish`, uploads `publish/wwwroot`, and deploys that folder.

5. **CORS:** On the API, allow the Vercel origin, e.g. in `Program.cs`:
   ```csharp
   policy.WithOrigins("https://your-app.vercel.app").AllowAnyMethod().AllowAnyHeader();
   ```

---

## Summary

| Goal                         | Use                          |
|-----------------------------|------------------------------|
| Easiest, one place to deploy | Option 1 (single .NET host)  |
| Blazor on Vercel, API elsewhere | Option 2 (Vercel + Railway/Fly/Render) |
