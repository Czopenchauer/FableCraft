import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Adventure,
  AdventureDto,
  AdventureCreationStatus,
  GenerateLorebookDto,
  GeneratedLorebookDto,
  AvailableLorebookDto,
  AdventureListItemDto,
  GameScene
} from '../models/adventure.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdventureService {
  private readonly apiUrl = `${environment.apiUrl}/api/adventure`;

  constructor(private http: HttpClient) { }

  /**
   * Get all adventures
   */
  getAllAdventures(): Observable<AdventureListItemDto[]> {
    return this.http.get<AdventureListItemDto[]>(this.apiUrl);
  }

  /**
   * Get adventure by ID
   * Note: This endpoint doesn't exist in the backend yet
   */
  getAdventure(id: string): Observable<Adventure> {
    return this.http.get<Adventure>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new adventure
   */
  createAdventure(adventure: AdventureDto): Observable<AdventureCreationStatus> {
    return this.http.post<AdventureCreationStatus>(
      `${this.apiUrl}/create-adventure`,
      adventure
    );
  }

  /**
   * Get adventure creation status
   */
  getAdventureStatus(adventureId: string): Observable<AdventureCreationStatus> {
    return this.http.get<AdventureCreationStatus>(
      `${this.apiUrl}/status/${adventureId}`
    );
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
   * Retry knowledge graph processing
   */
  retryKnowledgeGraphProcessing(adventureId: string): Observable<AdventureCreationStatus> {
    return this.http.post<AdventureCreationStatus>(
      `${this.apiUrl}/retry-knowledge-graph/${adventureId}`,
      {}
    );
  }

  /**
   * Retry adventure creation
   */
  retryCreateAdventure(adventureId: string): Observable<AdventureCreationStatus> {
    return this.http.post<AdventureCreationStatus>(
      `${this.apiUrl}/retry-create-adventure/${adventureId}`,
      {}
    );
  }

  /**
   * Delete an adventure
   */
  deleteAdventure(adventureId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${adventureId}`);
  }

  // ============== Game Play API Methods ==============

  /**
   * Generate the first scene for an adventure
   */
  generateFirstScene(adventureId: string): Observable<GameScene> {
    return this.http.get<GameScene>(
      `${environment.apiUrl}/api/play/current-scene/${adventureId}`,
      {}
    );
  }

  /**
   * Submit a player action (choice selection)
   */
  submitAction(adventureId: string, actionText: string): Observable<GameScene> {
    return this.http.post<GameScene>(
      `${environment.apiUrl}/api/play/submit`,
      { adventureId, actionText }
    );
  }

  /**
   * Delete the last scene from an adventure
   */
  deleteLastScene(adventureId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/api/play/delete/${adventureId}`
    );
  }

  /**
   * Regenerate the last scene of an adventure
   */
  regenerateScene(adventureId: string): Observable<GameScene> {
    return this.http.post<GameScene>(
      `${environment.apiUrl}/api/play/regenerate/${adventureId}`,
      {}
    );
  }
}
