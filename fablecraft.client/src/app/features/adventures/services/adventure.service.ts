import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';
import {
  AdventureCreationStatus,
  AdventureDto,
  AdventureListItemDto,
  AvailableLorebookDto,
  ComponentStatus,
  GameScene,
  GeneratedLorebookDto,
  GenerateLorebookDto
} from '../models/adventure.model';
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
   * Get latest scene for an adventure
   */
  generateFirstScene(adventureId: string): Observable<GameScene> {
    const url = `${environment.apiUrl}/api/Play/${adventureId}?Take=1`;
    // API may return either a single GameScene or an array when Take=1.
    // Normalize to a single GameScene for the UI to consume consistently.
    return this.http.get<any>(url).pipe(
      map((resp) => Array.isArray(resp) ? resp[0] as GameScene : resp as GameScene)
    );
  }

  /**
   * Submit a player action (choice selection)
   */
  submitAction(adventureId: string, actionText: string): Observable<GameScene> {
    return this.http.post<GameScene>(`${environment.apiUrl}/api/Play/submit`, {adventureId, actionText});
  }

  /**
   * Delete the last scene from an adventure
   */
  deleteLastScene(adventureId: string, sceneId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/Play/delete/${adventureId}/scene/${sceneId}`);
  }

  /**
   * Regenerate the last scene of an adventure
   */
  regenerateScene(adventureId: string, sceneId: string): Observable<GameScene> {
    return this.http.post<GameScene>(`${environment.apiUrl}/api/Play/regenerate/${adventureId}/scene/${sceneId}`, {});
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
