# FableCraft

An interactive fiction engine that produces NPCs with genuine agency. Characters act rather than react. Antagonists scheme without waiting for player input. The world moves.

## What Makes FableCraft Different

Single-model interactive fiction systems have one model juggling everything, leading to:
- Knowledge bleed between characters (the bartender somehow knows what the villain said in private)
- Memory inconsistency (characters forget important events, remember things they shouldn't)
- Reactive NPCs (everyone waits for the player to act first)
- Forced hooks (plot points feel contrived rather than emergent)

FableCraft solves these problems with:
- **Knowledge boundaries** - Separate knowledge graph per important character
- **Character emulation** - Full psychological context when characters act
- **Off-screen simulation** - Characters live between encounters
- **Narrative awareness** - Story fabric tracking, world momentum, writer guidance
- **Immersive prose** - First-person present-tense narrative

## Features

- **Autonomous NPCs** - Arc-important characters simulate off-screen, pursuing their own goals
- **Per-character knowledge graphs** - No memory bleed; each character only knows what they've experienced
- **Tiered character system** - Arc-important (4-8), significant (10-20), and unlimited background characters
- **Story tracking** - Chronicler tracks dramatic questions, promises, stakes, and world momentum
- **Dynamic world** - Events progress independently of player action
- **Character emulation** - NPCs are emulated with full psychological context during scenes

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) (for Angular frontend)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)
- LLM API key (OpenAI, Anthropic, Azure, Google, etc.)

## Quick Start

### Option A: Docker (Recommended)

The easiest way to run FableCraft with all services:

```bash
# Copy environment template and configure
cp .env.template .env

# Edit .env and set your LLM_API_KEY (required)
# Optionally configure LLM_PROVIDER, LLM_MODEL, etc.

# Start all services
docker-compose up -d
```

Services will be available at:
- **Frontend**: http://localhost:4200
- **Backend API**: http://localhost:5000
- **GraphRag API**: http://localhost:8111
- **Aspire Dashboard**: http://localhost:18888

### Option B: Aspire (Local Development)

For development with hot-reload and debugging:

```bash
# Ensure you have .NET Aspire workload installed
dotnet workload install aspire

# Run the AppHost (orchestrates all services)
dotnet run --project FableCraft.AppHost/FableCraft.AppHost.csproj
```

The Aspire dashboard will open automatically and show all running services.

## Configuration

Copy `.env.template` to `.env` and configure your API keys:

**Required settings:**

| Variable | Description |
|----------|-------------|
| `LLM_API_KEY` | Your LLM provider API key |
| `LLM_PROVIDER` | `gemini`, `openai`, `anthropic`, or `custom` |
| `LLM_MODEL` | Model name (e.g., `gemini/gemini-2.5-flash`, `gpt-4o`) |
| `EMBEDDING_API_KEY` | Your embedding provider API key |
| `EMBEDDING_PROVIDER` | `gemini`, `openai`, or `ollama` |
| `EMBEDDING_MODEL` | Embedding model (e.g., `gemini/gemini-embedding-001`) |

**Example minimal configuration (Gemini):**

```bash
LLM_API_KEY=your-gemini-api-key
LLM_PROVIDER=gemini
LLM_MODEL=gemini/gemini-2.5-flash

EMBEDDING_API_KEY=your-gemini-api-key
EMBEDDING_PROVIDER=gemini
EMBEDDING_MODEL=gemini/gemini-embedding-001
EMBEDDING_DIMENSIONS=3072
```

See `.env.template` for alternative providers (OpenAI, Anthropic, Ollama) and optional settings.

## Project Structure

```
FableCraft/
├── FableCraft.AppHost/      # Aspire orchestration
├── Server/
│   ├── FableCraft.Application/   # Business logic, narrative engine, agents
│   ├── FableCraft.Infrastructure/ # EF Core, persistence, external clients
│   ├── FableCraft.Server/        # ASP.NET Core Web API
│   ├── FableCraft.ServiceDefaults/ # Shared service configuration
│   └── FableCraft.Tests/         # Test projects
├── fablecraft.client/       # Angular frontend
├── GraphRag/                # Knowledge graph service (Python/Cognee)
├── Prompts/                 # LLM prompt templates
├── data/                    # Sample data
└── doc/                     # Documentation
```

## Architecture Overview

FableCraft uses a multi-agent architecture with separate knowledge graphs:

- **World KG** - Shared lore, locations, factions, public events
- **MC KG** - Main character's experiences and memories
- **Character KGs** - Per-character memories and subjective experiences

The narrative engine processes each player action through:
1. **Resolution** - Determines physical outcomes
2. **Writer** - Generates immersive first-person prose
3. **Background Processing** - Updates trackers, simulates NPCs, commits to knowledge graphs

For detailed architecture documentation, see [doc/FableCraftSystemOverview.md](doc/FableCraftSystemOverview.md).