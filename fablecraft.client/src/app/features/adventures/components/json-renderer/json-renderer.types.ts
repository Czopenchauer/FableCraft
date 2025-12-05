// Type definitions for JSON renderer component

export type JsonValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | JsonObject
  | JsonArray;

export interface JsonObject {
  [key: string]: JsonValue;
}

export type JsonArray = JsonValue[];

// Enum for value types
export enum ValueType {
  STRING = 'string',
  NUMBER = 'number',
  BOOLEAN = 'boolean',
  NULL = 'null',
  UNDEFINED = 'undefined',
  OBJECT = 'object',
  ARRAY = 'array',
  ARRAY_OF_PRIMITIVES = 'array-of-primitives',
  ARRAY_OF_OBJECTS = 'array-of-objects'
}

// Render node interface
export interface RenderNode {
  key: string;
  value: JsonValue;
  type: ValueType;
  isConstant: boolean;
  level: number;
}

// Configuration for constant field detection
export interface ConstantFieldConfig {
  paths: string[];      // Dot-notation paths like "story.time", "mainCharacter.name"
  patterns: RegExp[];   // Regex patterns for matching keys
}
