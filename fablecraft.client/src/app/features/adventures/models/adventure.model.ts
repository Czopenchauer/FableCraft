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

export interface AdventureDto {
  name: string;
  firstSceneDescription: string;
  referenceTime: string;
  authorNotes: string;
  character: CharacterDto;
  worldbookId?: string | null;
  trackerDefinitionId: string;
  fastLlmConfig: string;
  complexLlmConfig: string;
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

// Backend API response structure for GameScene
export interface GameSceneApiResponse {
  previousScene: string | null;
  nextScene: string | null;
  sceneId: string;
  generationOutput: SceneGenerationOutput | null;
  canRegenerate: boolean;
  enrichmentStatus: EnrichmentStatus;
}

export interface SceneGenerationOutput {
  sceneId: string;
  submittedAction: string | null;
  generatedScene: GeneratedScene;
  narrativeDirectorOutput: NarrativeDirectorOutput | null;
  tracker: TrackerDto | null;
  newLore: LoreInfo[] | null;
}

export interface GeneratedScene {
  scene: string;
  choices: string[];
}

export interface NarrativeDirectorOutput {
  [key: string]: any;
}

export interface TrackerDto {
  story: StoryTracker;
  mainCharacter: MainCharacterTrackerDto;
  charactersOnScene: string[];
  characters: CharacterStateDto[];
}

export interface StoryTracker {
  [key: string]: any;
}

export interface MainCharacterTrackerDto {
  tracker: CharacterTracker;
  development: CharacterDevelopmentTracker;
  description: string;
}

export interface CharacterTracker {
  [key: string]: any;
}

export interface CharacterDevelopmentTracker {
  [key: string]: any;
}

export interface CharacterStateDto {
  characterId: string;
  name: string;
  description: string;
  state: any;
  tracker: CharacterTracker;
  development: CharacterDevelopmentTracker;
}

// Flattened client-side model for easier use in components
export interface GameScene {
  previousScene: string | null;
  nextScene: string | null;
  sceneId: string;
  text: string;
  choices: string[] | null;
  selectedChoice: string | null;
  canRegenerate: boolean;
  tracker: TrackerDto | null;
  narrativeDirectorOutput: NarrativeDirectorOutput | null;
  enrichmentStatus: EnrichmentStatus;
  newLore: LoreInfo[] | null;
}

export enum EnrichmentStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Failed = 'Failed'
}

// Backend SceneEnrichmentOutput structure
export interface SceneEnrichmentResult {
  sceneId: string;
  tracker: TrackerDto;
  newLore: LoreInfo[];
}

export interface LoreInfo {
  title: string;
  summary: string;
}
