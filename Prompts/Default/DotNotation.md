# Dot-Notation for State Updates

Specify partial updates to nested structures using dot-separated paths.

## Core Principle

The path is the target. The value **replaces everything at that path**—other fields at the same level are untouched.
```json
{
  "character.mood": "Tense, distracted"
}
```
This overwrites `character.mood`. Other fields under `character` remain unchanged.

## Path Syntax

| Type | Syntax | Example |
|------|--------|---------|
| Nested field | `parent.child` | `"config.display.theme": "dark"` |
| Deep nested | `parent.child.grandchild` | `"character.status.health": 85` |

## Operations

**Update field**
```json
{ "location": "Castle courtyard", "weather": "Overcast" }
```

**Update nested field**
```json
{ "character.status.health": 85 }
```

**Update array** (always provide complete array)
```json
{
  "skills": [
    { "name": "Perception", "level": 3, "xp": 120 },
    { "name": "Stealth", "level": 1, "xp": 0 }
  ]
}
```

**No changes**
```json
{ "tracker_update": {} }
```

## Critical Rules

1. **Always provide complete values.** Partial objects or arrays at a path = data loss for omitted content.
2. **Use the deepest specific path.** Update `character.stats.strength`, not all of `character`.
3. **Arrays are replaced wholesale.** No element-level patching—output the full array with all elements.
4. **Null deletes.** Setting a path to `null` removes it.