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
| Array element by property | `array["value"]` | `"items[\"Sword\"].durability": 45` |
| Deep nested + array | `parent.array["value"].field` | `"inventory.slots[\"Main hand\"].equipped": true` |

## Operations

**Update field**
```json
{ "location": "Castle courtyard", "weather": "Overcast" }
```

**Update nested field**
```json
{ "character.status.health": 85 }
```

**Update array element**
```json
{ "skills[\"Perception\"].level": 3 }
```

**Add array element** (use new identifier)
```json
{
  "skills[\"Stealth\"]": {
    "name": "Stealth",
    "level": 1,
    "xp": 0
  }
}
```

**Delete array element**
```json
{ "skills[\"Obsolete\"]": null }
```

**No changes**
```json
{ "tracker_update": {} }
```

## Critical Rules

1. **Always provide complete objects.** Partial objects at a path = data loss for omitted fields.
2. **Use the deepest specific path.** Update `character.stats.strength`, not all of `character`.
3. **Null deletes.** Setting a path to `null` removes it.