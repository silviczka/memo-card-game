# Memo Card Game

A browser-based memory game built with .NET 8. Game rules, scoring, and state are handled on the server, while the client interacts through a REST API.

## Features

- Configurable board size (4×4, 6×6, 8×8) and optional attempt limit
- Server-enforced rules: two flips per turn, no flipping matched cards, invalid moves rejected
- Persistence with EF Core and SQLite (game state can be saved and resumed)
- Blazor WebAssembly UI served by the same ASP.NET Core application

## Tech Stack

- .NET 8
- ASP.NET Core Minimal API
- Blazor WebAssembly (client)
- Entity Framework Core with SQLite

## Getting Started

Prerequisite: .NET 8 SDK

```bash
cd src/MemoCardGame.Api
dotnet run
```

Open the URL shown in the terminal, for example `http://localhost:5000` or `https://localhost:5001`.

## API

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/games` | Create a new game. Body: `{ "boardSize": 4, "maxAttempts": 20 }` (optional). |
| GET | `/games/{id}` | Return the current game state. Unflipped cards are masked. |
| POST | `/games/{id}/flip` | Flip a card. Body: `{ "cardId": "<guid>" }`. |
| POST | `/games/{id}/resolve` | Resolve the current turn after two cards are flipped. |

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
- **MemoCardGame.Client** — Blazor WebAssembly frontend (`Pages`, `wwwroot`, `Shared` components)
- **MemoCardGame.Tests** — Unit tests

## Deployment

See [docs/DEPLOY.md](docs/DEPLOY.md) for deployment options. The repository root includes a `Dockerfile` that publishes `MemoCardGame.Api`.

## License

MIT
