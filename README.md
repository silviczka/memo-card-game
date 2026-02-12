# Memo Card Game

A browser-based memory (card matching) game. Game rules, scoring, and state are handled on the server; the client calls a REST API. Built with .NET 8.

## Features

- Configurable board size (2×2, 4×4, 6×6) and optional attempt limit
- Server-enforced rules: two flips per turn, no flipping matched cards, invalid moves rejected
- Persistence with EF Core and SQLite (game state can be saved and resumed)
- Blazor WebAssembly UI served by the same ASP.NET Core application

## Tech Stack

- .NET 8
- ASP.NET Core Minimal API
- Blazor WebAssembly (client)
- Entity Framework Core with SQLite

## Getting Started

**Prerequisites:** .NET 8 SDK

```bash
cd src/MemoCardGame.Api
dotnet run
```

Open the URL shown in the terminal (e.g. `http://localhost:5000` or `https://localhost:5001`).

## API

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/games` | Create a new game. Body: `{ "boardSize": 4, "maxAttempts": 20 }` (optional). |
| GET | `/games/{id}` | Get current game state. Unflipped cards are masked. |
| POST | `/games/{id}/flip` | Flip a card. Body: `{ "cardId": "<guid>" }`. |
| POST | `/games/{id}/resolve` | Resolve the current turn (after two cards are flipped). |

Error responses use the shape `{ "error": "<message>" }`.

## Tests

From the repository root:

```bash
dotnet test
```

Unit tests cover game rules (flip validation, scoring, game completion).

## Project Structure

- **MemoCardGame.Domain** — Game, Card, Turn, scoring rules (no external dependencies)
- **MemoCardGame.Application** — Game service, factory, move validation, DTOs
- **MemoCardGame.Infrastructure** — Persistence (EF Core, SQLite, repository)
- **MemoCardGame.Api** — Web API and host for the Blazor client
- **MemoCardGame.Client** — Blazor WebAssembly frontend
- **MemoCardGame.Tests** — Unit tests

## Deployment

See [DEPLOY.md](DEPLOY.md) for single-host deployment (Railway, Fly.io, Render) and for hosting the Blazor client on Vercel with the API on a separate .NET host.

## License

MIT
