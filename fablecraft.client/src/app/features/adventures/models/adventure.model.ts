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

export interface AdventureAgentLlmPresetDto {
  llmPresetId: string;
  agentName: string;
}

export interface ExtraLoreEntryDto {
  title: string;
  content: string;
  category: string;
}

export interface AdventureDto {
  name: string;
  firstSceneDescription: string;
  referenceTime: string;
  mainCharacter: CharacterDto;
  worldbookId?: string | null;
  trackerDefinitionId: string;
  promptPath: string;
  agentLlmPresets: AdventureAgentLlmPresetDto[];
  extraLoreEntries?: ExtraLoreEntryDto[];
}

export interface AdventureDefaultsDto {
  defaultPromptPath: string;
  availableAgents: string[];
}

export interface DirectoryListingDto {
  currentPath: string;
  parentPath: string | null;
  directories: DirectoryEntryDto[];
}

export interface DirectoryEntryDto {
  fullPath: string;
  name: string;
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
  canDelete: boolean;
  enrichmentStatus: EnrichmentStatus;
}

export interface SceneGenerationOutput {
  sceneId: string;
  submittedAction: string | null;
  generatedScene: GeneratedScene;
  narrativeDirectorOutput: NarrativeDirectorOutput | null;
  tracker: TrackerDto | null;
  newLore: LoreInfo[] | null;
  metadata: SceneMetadataDto | null;
}

export interface GeneratedScene {
  scene: string;
  choices: string[];
}

export interface NarrativeDirectorOutput {
  [key: string]: any;
}

export interface TrackerDto {
  scene: SceneTracker;
  mainCharacter: MainCharacterTrackerDto;
  characters: CharacterStateDto[];
}

export interface SceneTracker {
  time: string;
  location: string;
  weather: string;
  charactersPresent: string[];

  [key: string]: any;
}

export interface MainCharacterTrackerDto {
  tracker: CharacterTracker;
  description: string;
}

export interface CharacterTracker {
  [key: string]: any;
}

export interface CharacterStateDto {
  characterId: string;
  name: string;
  description: string;
  state: any;
  tracker: CharacterTracker;
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
  canDelete: boolean;
  canEdit: boolean;
  tracker: TrackerDto | null;
  narrativeDirectorOutput: NarrativeDirectorOutput | null;
  enrichmentStatus: EnrichmentStatus;
  newLore: LoreInfo[] | null;
  metadata: SceneMetadataDto | null;
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

// Scene metadata interfaces
export interface SceneMetadataDto {
  resolutionOutput: string | null;
  gatheredContext: GatheredContextDto | null;
  writerObservation: { [key: string]: any } | null;
  chroniclerState: ChroniclerStoryStateDto | null;
  writerGuidance: string | null;
}

export interface GatheredContextDto {
  worldContext: GatheredContextItemDto[];
  narrativeContext: GatheredContextItemDto[];
}

export interface GatheredContextItemDto {
  topic: string;
  content: string;
}

// ChroniclerStoryState uses snake_case from backend JsonPropertyName attributes
// and JsonExtensionData which flattens additional properties
export interface ChroniclerStoryStateDto {
  world_momentum?: any[];
  dramatic_questions?: DramaticQuestionDto[];
  promises?: PromiseDto[];
  active_threads?: ActiveThreadDto[];
  stakes?: StakeDto[];
  windows?: WindowDto[];
  [key: string]: any; // Allow additional properties
}

export interface DramaticQuestionDto {
  question: string;
  introduced: string;
  tension_level: string;
  resolution_proximity: string;
}

export interface PromiseDto {
  setup: string;
  introduced: string;
  time_since: string;
  payoff_readiness: string;
}

export interface ActiveThreadDto {
  name: string;
  status: string;
  momentum: string;
  last_touched: string;
}

export interface StakeDto {
  what: string;
  condition: string;
  deadline: string;
  failure_consequence: string;
}

export interface WindowDto {
  opportunity: string;
  closes: string;
  if_missed: string;
}
