# Dot-Notation for State Updates

Specify partial updates to nested structures using dot-separated paths.

## How It Works

The key is the path. The value **replaces the entire object at that path**.

```json
{
  "emotional_landscape.current_state": {
    "primary_emotion": "anxious",
    "secondary_emotions": ["calculating"],
    "intensity": "strong",
    "cause": "exposure risk"
  }
}
```

This replaces `emotional_landscape.current_state` entirely. Other fields under `emotional_landscape` are untouched.

## Rules

1. **Complete objects at each path.** Partial objects cause data loss.
2. **Use the deepest specific path.** Update `goals.immediate_intention`, not all of `goals`.
3. **Scalar values are valid.** `"character_arc.progress_percentage": 45`
4. **Arrays use brackets.** `"Skills[Persuasion].XP.Current": 215`
5. **Empty object if nothing changed.** `"profile_updates": {}`

## Examples

**Single nested update:**
```json
{
  "emotional_landscape.current_state": {
    "primary_emotion": "anxious",
    "intensity": "strong",
    "cause": "exposure risk"
  }
}
```

**Scalar value:**
```json
{
  "character_arc.progress_percentage": 45
}
```

**Array element (bracket notation):**
```json
{
  "Skills[Persuasion].XP.Current": 215
}
```

**Multiple updates:**
```json
{
  "emotional_landscape.current_state": { ... },
  "goals_and_motivations.immediate_intention": "lay low until heat passes",
  "character_arc.current_stage": "hunted"
}
```

**Nothing changed:**
```json
{
  "profile_updates": {}
}
```
