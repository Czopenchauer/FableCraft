export type CharacterImportance = 'arc_important' | 'significant' | 'background';

// Lightweight list item for sidebar
export interface CharacterListItem {
  characterId: string;
  name: string;
  importance: CharacterImportance;
}

// Full detail with pagination counts
export interface CharacterDetail {
  characterId: string;
  name: string;
  importance: CharacterImportance;
  description: string;
  characterState: CharacterStats;
  characterTracker: CharacterTracker | null;
  characterMemories: CharacterMemory[];
  relationships: CharacterRelationship[];
  sceneRewrites: CharacterSceneRewrite[];
  totalMemoriesCount: number;
  totalSceneRewritesCount: number;
}

export interface CharacterStats {
  character_identity?: CharacterIdentity;
  motivations?: any;
  routine?: any;
  [key: string]: any;
}

export interface CharacterIdentity {
  name?: string;
  [key: string]: any;
}

export interface CharacterTracker {
  name: string;
  location: string;
  appearance?: string;
  generalBuild?: string;
  [key: string]: any;
}

export interface CharacterMemory {
  id: string;
  memoryContent: string;
  sceneTracker: SceneTracker;
  salience: number;
  data: Record<string, any> | null;
}

export interface CharacterRelationship {
  id: string;
  targetCharacterName: string;
  dynamic: string | null;
  data: Record<string, any>;
  sequenceNumber: number;
  updateTime: string | null;
}

export interface CharacterSceneRewrite {
  id: string;
  content: string;
  sequenceNumber: number;
  sceneTracker: SceneTracker;
}

export interface SceneTracker {
  time: string;
  location: string;
  weather: string;
  charactersPresent: string[];
  [key: string]: any;
}

// Paginated response wrapper
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  offset: number;
}

// Update request types
export interface UpdateProfileRequest {
  description: string;
}

export interface UpdateTrackerRequest {
  tracker: CharacterTracker;
}

export interface UpdateMemoryRequest {
  summary: string;
  salience: number;
  data: Record<string, any> | null;
}

export interface UpdateRelationshipRequest {
  dynamic: string | null;
  data: Record<string, any>;
}

export interface UpdateImportanceRequest {
  importance: CharacterImportance;
}
