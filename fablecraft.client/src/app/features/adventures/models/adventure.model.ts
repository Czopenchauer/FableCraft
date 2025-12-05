export interface Adventure {
  id: string;
  title: string;
  description: string;
  status: AdventureStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface AdventureListItemDto {
  adventureId: string;
  name: string;
  lastScenePreview?: string;
  created: string;
  lastPlayed?: string;
}

export interface CharacterDto {
  name: string;
  description: string;
}

export interface LorebookEntryDto {
  description: string;
  content: string;
  category: string;
  order: number;
  contentType: ContentType;
}

export interface AdventureDto {
  name: string;
  firstSceneDescription: string;
  authorNotes: string;
  referenceTime: string;
  trackerStructure: string;
  character: CharacterDto;
  lorebook: LorebookEntryDto[];
}

export interface AvailableLorebookDto {
  category: string;
  description: string;
  priority: number;
}

export interface AdventureCreationStatus {
  adventureId: string;
  componentStatuses: Record<string, ComponentStatus>;
}

export enum AdventureStatus {
  Creating = 'Creating',
  Processing = 'Processing',
  Ready = 'Ready',
  Failed = 'Failed'
}

export type ComponentStatus = 'Pending' | 'InProgress' | 'Completed' | 'Failed';

export interface GenerateLorebookDto {
  // For generation we only need a subset of fields from lorebook entries
  lorebooks: LorebookDraft[];
  category: string;
  additionalInstruction?: string;
}

export interface GeneratedLorebookDto {
  content: string;
}

// Matches server ContentType enum (System.Text.Json serializes enum names as strings)
// Matches server ContentType enum (OpenAPI specifies integer enum [0,1])
export enum ContentType {
  json = 0,
  txt = 1
}

// Lightweight draft used for lorebook content generation requests only
export interface LorebookDraft {
  category: string;
  description: string;
  content: string;
}

export interface LorebookGenerationState {
  status: 'pending' | 'generating' | 'completed' | 'error';
  content?: string;
  error?: string;
}

export interface GameScene {
  sceneId: string;
  text: string;
  choices: string[] | null;
  // In the server DTO, this is nullable string; client uses null when no choice selected
  selectedChoice: string | null;
  canRegenerate: boolean;
  tracker: any;
  narrativeDirectorOutput: any;
}

export interface SceneEnrichmentResult {
  sceneId: string;
  tracker: any;
  newCharacters: CharacterInfo[];
  newLocations: LocationInfo[];
  newLore: LoreInfo[];
}

export interface CharacterInfo {
  characterId: string;
  name: string;
  description: string;
}

export interface LocationInfo {
  name: string;
  description: string;
}

export interface LoreInfo {
  title: string;
  summary: string;
}
