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

- [Docker](https://docs.docker.com/get-started/introduction/get-docker-desktop/#explanation)

## Quick Start

**Linux/macOS:**
```bash
./start.sh
```

**Windows (PowerShell):**
```powershell
.\start.ps1
```

Services will be available at:
- **Frontend**: http://localhost:4200
- **Backend API**: http://localhost:5000
- **GraphRag API**: http://localhost:8111
- **Aspire Dashboard**: http://localhost:18888

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
