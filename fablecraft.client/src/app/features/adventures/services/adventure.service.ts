import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Adventure,
  AdventureDto,
  AdventureCreationStatus,
  GenerateLorebookDto,
  AvailableLorebookDto
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
   * Note: This endpoint doesn't exist in the backend yet
   * You'll need to implement GET /api/adventure in AdventureController.cs
   */
  getAllAdventures(): Observable<Adventure[]> {
    return this.http.get<Adventure[]>(this.apiUrl);
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
  generateLorebook(dto: GenerateLorebookDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/generate-lorebook`, dto);
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
   * Delete an adventure
   */
  deleteAdventure(adventureId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${adventureId}`);
  }
}
