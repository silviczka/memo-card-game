# Deployment

This solution supports two deployment patterns.

## Option 1: Single host

Deploy `MemoCardGame.Api` to a single host. It serves both the REST API and the Blazor WebAssembly client.

For this mode, use an empty `ApiBaseUrl` in `MemoCardGame.Client/wwwroot/appsettings.json` and `appsettings.Production.json` before you publish the API (or Docker build), so the client calls the same origin as the page.

Suggested hosts:

- **Koyeb** — Build from the `Dockerfile` at the repository root.
- **Railway** — Connect GitHub, use `src/MemoCardGame.Api` as the service root, or deploy from the root `Dockerfile`.
- **Fly.io** — Run `fly launch` from the repository root and use the existing `Dockerfile`.
- **Render** — Create a Web Service and publish `src/MemoCardGame.Api`.

Pricing and free-plan policies change frequently, so check the official pages before choosing:

- Railway: [pricing / free trial](https://docs.railway.com/reference/pricing/free-trial)
- Render: [free plan](https://render.com/free)
- Fly.io: [pricing](https://fly.io/docs/about/pricing/)
- Koyeb: [pricing FAQ](https://www.koyeb.com/docs/faqs/pricing)

## Option 2: Static client, API elsewhere

Publish the Blazor WebAssembly client as static files and host the API separately.

1. Deploy the API and note its public URL.

2. Set `ApiBaseUrl` in `src/MemoCardGame.Client/wwwroot/appsettings.Production.json` to that URL before publishing the client.

3. Set `Cors:AllowedOrigins` in `src/MemoCardGame.Api/appsettings.Production.json` to your static frontend origin (replace `https://your-app.vercel.app`).

4. Build the client:
   ```bash
   dotnet publish src/MemoCardGame.Client -c Release
   ```
   Output: `src/MemoCardGame.Client/bin/Release/net8.0/publish/wwwroot/`.

5. Deploy the contents of `src/MemoCardGame.Client/bin/Release/net8.0/publish/wwwroot/` to your static host.

`MemoCardGame.Api/Program.cs` reads `Cors:AllowedOrigins` from configuration; local development keeps `AllowedOrigins` empty in `appsettings.json` so any origin is allowed.

## Summary

| Goal                         | Use                          |
|-----------------------------|------------------------------|
| Easiest setup | Option 1 (single host) |
| Static client and separate API | Option 2 |
