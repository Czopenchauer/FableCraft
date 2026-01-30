# ADR-001: Cognee Knowledge Graph Isolation per Adventure

**Status:** Accepted  
**Date:** 2025-01-21  
**Decision Makers:** Development Team  

## Context

FableCraft uses Cognee to build knowledge graphs from worldbook content. The `cognify` operation processes text through LLMs to extract entities, relationships, and embeddings—a computationally expensive operation that can take hours for large worldbooks.

Currently, each adventure shares a single Cognee instance. The world dataset is heavily edited during gameplay as it serves as an information exchange bus between characters. This creates a problem when users want to start new adventures from the same worldbook without re-running cognify.

## Problem Statement

**How do we enable multiple adventures to fork from a shared, pre-cognified worldbook template while allowing each adventure's knowledge graph to evolve independently?**

Requirements:
1. Avoid re-running `cognify` for each new adventure (hours of processing time, LLM costs)
2. Each adventure must have isolated graph state (modifications don't affect other adventures)
3. Solution must work within Docker-based deployment
4. Performance must remain acceptable (IO-bound operations are a concern)
5. Bonus: Enable sharing of pre-cognified templates between users/installations

## Options Considered

### Option 1: Cognee Native Dataset Copy

**Description:** Use Cognee's built-in functionality to copy/clone datasets.

**Advantages:**
- Would be the cleanest solution if it existed

**Disadvantages:**
- Feature does not exist in Cognee
- Requested from Cognee team, but no timeline for implementation
- Would require upstream changes we don't control

**Verdict:** Not viable — feature doesn't exist.

---

### Option 2: Direct Database Manipulation

**Description:** Connect directly to Kuzu (graph DB) and LanceDB (vector store) to copy nodes/edges with updated dataset labels.

**Advantages:**
- Technically possible
- Would give full control over data

**Disadvantages:**
- Requires deep knowledge of Cognee's internal schema
- Schema is undocumented and may change between versions
- Must handle both graph DB and vector store separately
- High maintenance burden, fragile to Cognee updates

**Verdict:** Too fragile — internal schema dependency is a maintenance nightmare.

---

### Option 3: Re-add Files Under New Dataset Name

**Description:** Keep chunk files on disk, call `/add` and `/cognify` with new dataset names for each adventure.

**Advantages:**
- Uses public Cognee API
- Files aren't duplicated (reuse existing chunks)

**Disadvantages:**
- Still runs `cognify` — the expensive operation we're trying to avoid
- Defeats the purpose of having a template

**Verdict:** Doesn't solve the core problem — still incurs cognify cost.

---

### Option 4: Shared Template Dataset + Per-Adventure Delta

**Description:** Structure as read-only template dataset plus adventure-specific additions. Search queries hit both datasets.

```
Search scope for Adventure B:
  - template_world (read-only, shared base)
  - adventureB_world (adventure-specific additions)
```

**Advantages:**
- No template copying needed
- Only cognify delta content

**Disadvantages:**
- Cannot modify template content per-adventure (only add)
- Requires changes to search logic (query multiple datasets)
- World dataset is heavily modified, not just extended

**Verdict:** Architectural mismatch — our use case modifies existing content, not just adds.

---

### Option 5: Bind Mounts with Symlink Switching

**Description:** Store Cognee data in bind-mounted directories. Use symlinks to switch between adventure directories. Restart Cognee to pick up new path.

```
./adventures/
├── template/
├── active -> {adventureId}/  # symlink
└── {adventureId}/
```

**Advantages:**
- Simple conceptually
- File copy is fast (no LLM costs)
- Full isolation per adventure

**Disadvantages:**
- Bind mounts have severe IO overhead on Docker Desktop (Mac/Windows)
- Cognify takes hours with bind mounts vs minutes with native volumes

**Verdict:** IO performance is a dealbreaker — bind mounts are too slow.

---

### Option 6: Named Volumes with Container Recreation

**Description:** Use Docker named volumes (native performance). Create a volume per adventure by copying from template volume. Recreate Cognee container with different volume mount when switching adventures.

```
Docker Volumes:
├── cognee-template           # Pre-cognified worldbook
├── cognee-adventure-{uuid}   # Per-adventure copies
└── ...

Switch = Stop container → Create with new volume → Start
```

**Advantages:**
- Native Docker volume performance (no IO overhead)
- Full isolation per adventure
- Volume copy is fast (volume-to-volume, no host filesystem)
- Can export/import volumes as tar.gz for sharing
- Uses public Docker API (Docker.DotNet)

**Disadvantages:**
- Requires container recreation (~5-10 seconds per switch)
- Backend becomes container orchestrator (more responsibility)
- More complex than symlink approach
- Requires Docker socket access

**Verdict:** Best balance of performance, isolation, and maintainability.

---

### Option 7: One Cognee Container Per Adventure

**Description:** Spin up a dedicated Cognee container for each adventure, all running simultaneously.

**Advantages:**
- No switching delay
- Full isolation

**Disadvantages:**
- A single Cognee instance consumes ~2GB RAM — running multiple instances is not feasible
- Port management complexity
- Overkill for single-user application with one active adventure

**Verdict:** Resource requirements make this impractical.

---

## Decision

**Chosen: Option 6 — Named Volumes with Container Recreation**

### Rationale

1. **Performance:** Named volumes use Docker's native storage driver, avoiding the catastrophic IO overhead of bind mounts. This is critical since cognify was taking hours with bind mounts.

2. **Isolation:** Each adventure gets a completely independent copy of the knowledge graph. Modifications in one adventure cannot affect others.

3. **No Cognee Changes Required:** Solution works entirely through Docker volume management and public Cognee API. No dependency on internal Cognee implementation details.

4. **Acceptable Switch Time:** Container recreation takes ~5-10 seconds. For a single-user application where adventure switches are infrequent (user explicitly starts/loads an adventure), this is acceptable UX.

5. **Bonus: Shareability:** Docker volumes can be exported to tar.gz files and imported elsewhere. This enables:
   - Shipping pre-cognified worldbooks with the application
   - Users sharing their adventures
   - Backup and restore functionality

6. **Maintainability:** Docker.DotNet is a stable .NET Foundation library. The Docker API for volume and container management is well-documented and unlikely to change.

### Trade-offs Accepted

- **Backend Complexity:** The backend now manages Cognee's container lifecycle. This is additional responsibility but gives us full control.

- **Switch Latency:** ~5-10 seconds per adventure switch. Acceptable for our use case (infrequent, user-initiated).

- **Docker Socket Access:** Backend needs `/var/run/docker.sock` mount. Acceptable for single-user self-hosted application. Docker.DotNet communicates with the Docker daemon via this socket.

## Consequences

### Positive

- New adventures start instantly from template (no cognify)
- Each adventure has isolated, modifiable knowledge graph
- Pre-cognified templates can be distributed with the app
- Users can export/share adventures
- No dependency on Cognee internal implementation

### Negative

- Backend is responsible for container lifecycle management
- Docker.DotNet dependency added
- Docker socket access required (security consideration for multi-tenant)
- ~5-10 second delay when switching adventures

### Neutral

- `graph-rag-api` service removed from docker-compose (managed by backend)
- New `AdventureContextService` class added
- Configuration for Cognee container settings required

## Implementation

See `cognee-adventure-context-switching.md` for detailed implementation guide including:
- Docker Compose changes
- `AdventureContextService` implementation
- Volume export/import functionality
- Usage examples

## References

- [Cognee Documentation](https://docs.cognee.ai/)
- [Docker.DotNet GitHub](https://github.com/dotnet/Docker.DotNet)
- [Docker Volumes Documentation](https://docs.docker.com/storage/volumes/)
