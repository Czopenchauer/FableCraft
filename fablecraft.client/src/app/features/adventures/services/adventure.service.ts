import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';
import {
  AdventureCreationStatus,
  AdventureDefaultsDto,
  AdventureDto,
  AdventureListItemDto,
  AvailableLorebookDto,
  ComponentStatus,
  DirectoryListingDto,
  GameScene,
  GameSceneApiResponse,
  GeneratedLorebookDto,
  GenerateLorebookDto,
  SceneEnrichmentResult
} from '../models/adventure.model';
import {AdventureSettingsResponseDto, UpdateAdventureSettingsDto} from '../models/adventure-settings.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdventureService {
  private readonly apiUrl = `${environment.apiUrl}/api/Adventure`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all adventures
   */
  getAllAdventures(): Observable<AdventureListItemDto[]> {
    return this.http.get<AdventureListItemDto[]>(this.apiUrl);
  }

  /**
   * Get default adventure settings (default prompt path, available agents, available directories)
   */
  getDefaults(): Observable<AdventureDefaultsDto> {
    return this.http.get<AdventureDefaultsDto>(`${this.apiUrl}/defaults`);
  }

  /**
   * Get available prompt directories for a given path (for directory browser)
   */
  getPromptDirectories(path?: string): Observable<DirectoryListingDto> {
    const options = path ? {params: {path}} : {};
    return this.http.get<DirectoryListingDto>(`${this.apiUrl}/prompt-directories`, options);
  }

  /**
   * Create a new adventure (maps server status to client status)
   */
  createAdventure(adventure: AdventureDto): Observable<AdventureCreationStatus> {
    return this.http
      .post<ServerAdventureCreationStatus>(`${this.apiUrl}/create-adventure`, adventure)
      .pipe(map(mapServerStatusToClient));
  }

  /**
   * Get adventure creation status
   */
  getAdventureStatus(adventureId: string): Observable<AdventureCreationStatus> {
    return this.http
      .get<ServerAdventureCreationStatus>(`${this.apiUrl}/status/${adventureId}`)
      .pipe(map(mapServerStatusToClient));
  }

  /**
   * Get supported lorebooks
   */
  getSupportedLorebooks(): Observable<AvailableLorebookDto[]> {
    return this.http.get<AvailableLorebookDto[]>(`${this.apiUrl}/lorebook`);
  }

  /**
   * Generate lorebook content
   */
  generateLorebook(dto: GenerateLorebookDto): Observable<GeneratedLorebookDto> {
    return this.http.post<GeneratedLorebookDto>(`${this.apiUrl}/generate-lorebook`, dto);
  }

  /**
   * Retry adventure creation
   */
  retryCreateAdventure(adventureId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/retry-create-adventure/${adventureId}`, {});
  }

  /**
   * Delete an adventure
   */
  deleteAdventure(adventureId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${adventureId}`);
  }

  // ============== Game Play API Methods ==============

  /**
   * Get current scene for an adventure
   */
  getCurrentScene(adventureId: string): Observable<GameScene> {
    return this.http.get<GameSceneApiResponse>(`${environment.apiUrl}/api/Play/${adventureId}/current-scene`)
      .pipe(map(mapApiResponseToGameScene));
  }

  /**
   * Get a specific scene by ID
   */
  getScene(adventureId: string, sceneId: string): Observable<GameScene> {
    return this.http.get<GameSceneApiResponse>(`${environment.apiUrl}/api/Play/${adventureId}/scene/${sceneId}`)
      .pipe(map(mapApiResponseToGameScene));
  }

  /**
   * Submit a player action (choice selection)
   */
  submitAction(adventureId: string, actionText: string): Observable<GameScene> {
    return this.http.post<GameSceneApiResponse>(`${environment.apiUrl}/api/Play/${adventureId}/submit`, {
      adventureId,
      actionText
    })
      .pipe(map(mapApiResponseToGameScene));
  }

  /**
   * Delete the last scene from an adventure
   */
  deleteLastScene(adventureId: string, sceneId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/Play/${adventureId}/scene/${sceneId}`);
  }

  /**
   * Regenerate the last scene of an adventure
   */
  regenerateScene(adventureId: string, sceneId: string): Observable<GameScene> {
    return this.http.post<GameSceneApiResponse>(`${environment.apiUrl}/api/Play/${adventureId}/scene/${sceneId}/regenerate`, {})
      .pipe(map(mapApiResponseToGameScene));
  }

  /**
   * Enrich a scene with tracker, characters, locations, and lore
   */
  enrichScene(adventureId: string, sceneId: string): Observable<SceneEnrichmentResult> {
    return this.http.post<SceneEnrichmentResult>(`${environment.apiUrl}/api/Play/${adventureId}/scene/${sceneId}/enrich`, {});
  }

  /**
   * Regenerate enrichment for specific agents
   */
  regenerateEnrichment(adventureId: string, sceneId: string, agentsToRegenerate: string[]): Observable<SceneEnrichmentResult> {
    return this.http.post<SceneEnrichmentResult>(
      `${environment.apiUrl}/api/Play/${adventureId}/scene/${sceneId}/enrich/regenerate`,
      {adventureId, sceneId, agentsToRegenerate}
    );
  }

  // ============== Adventure Settings API Methods ==============

  /**
   * Get adventure settings (agent LLM presets and prompt path)
   */
  getAdventureSettings(adventureId: string): Observable<AdventureSettingsResponseDto> {
    return this.http.get<AdventureSettingsResponseDto>(`${this.apiUrl}/${adventureId}/settings`);
  }

  /**
   * Update adventure settings (agent LLM presets and prompt path)
   */
  updateAdventureSettings(adventureId: string, settings: UpdateAdventureSettingsDto): Observable<AdventureSettingsResponseDto> {
    return this.http.put<AdventureSettingsResponseDto>(`${this.apiUrl}/${adventureId}/settings`, settings);
  }
}

// ===== Types and mappers for server/client contract bridging =====

type ServerComponentStage = string | null | undefined;

interface ServerAdventureCreationStatus {
  adventureId: string;
  ragProcessing?: ServerComponentStage;
  sceneGeneration?: ServerComponentStage;
}

function mapStageToComponentStatus(stage: ServerComponentStage): ComponentStatus {
  if (!stage) return 'Pending';
  const normalized = String(stage);
  if (normalized === 'Pending' || normalized === 'InProgress' || normalized === 'Completed' || normalized === 'Failed') {
    return normalized as ComponentStatus;
  }
  return 'InProgress';
}

function mapServerStatusToClient(server: ServerAdventureCreationStatus): AdventureCreationStatus {
  return {
    adventureId: server.adventureId,
    componentStatuses: {
      ragProcessing: mapStageToComponentStatus(server.ragProcessing),
      sceneGeneration: mapStageToComponentStatus(server.sceneGeneration)
    }
  };
}

// ===== GameScene API Response Mapper =====

function mapApiResponseToGameScene(response: GameSceneApiResponse): GameScene {
  const genOutput = response.generationOutput;
  return {
    previousScene: response.previousScene,
    nextScene: response.nextScene,
    sceneId: response.sceneId,
    text: genOutput?.generatedScene?.scene ?? '',
    choices: genOutput?.generatedScene?.choices ?? null,
    selectedChoice: genOutput?.submittedAction ?? null,
    canRegenerate: response.canRegenerate,
    canDelete: response.canDelete,
    tracker: genOutput?.tracker ?? null,
    narrativeDirectorOutput: genOutput?.narrativeDirectorOutput ?? null,
    enrichmentStatus: response.enrichmentStatus,
    newLore: genOutput?.newLore ?? null,
    metadata: genOutput?.metadata ?? null
  };
}
