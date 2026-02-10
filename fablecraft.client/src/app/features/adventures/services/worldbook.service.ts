import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  CopyWorldbookDto,
  IndexStartResponse,
  IndexStatusResponse,
  LorebookResponseDto,
  PendingChangesResponse,
  WorldbookDto,
  WorldbookResponseDto,
  WorldbookUpdateDto
} from '../models/worldbook.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorldbookService {
  private readonly apiUrl = `${environment.apiUrl}/api/Worldbook`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all worldbooks
   */
  getAllWorldbooks(): Observable<WorldbookResponseDto[]> {
    return this.http.get<WorldbookResponseDto[]>(this.apiUrl);
  }

  /**
   * Get a single worldbook by ID
   */
  getWorldbookById(id: string): Observable<WorldbookResponseDto> {
    return this.http.get<WorldbookResponseDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new worldbook
   */
  createWorldbook(worldbook: WorldbookDto): Observable<WorldbookResponseDto> {
    return this.http.post<WorldbookResponseDto>(this.apiUrl, worldbook);
  }

  /**
   * Update an existing worldbook
   */
  updateWorldbook(id: string, worldbook: WorldbookUpdateDto): Observable<WorldbookResponseDto> {
    return this.http.put<WorldbookResponseDto>(`${this.apiUrl}/${id}`, worldbook);
  }

  /**
   * Delete a worldbook
   */
  deleteWorldbook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Start indexing a worldbook
   */
  startIndexing(id: string): Observable<IndexStartResponse> {
    return this.http.post<IndexStartResponse>(`${this.apiUrl}/${id}/index`, {});
  }

  /**
   * Get indexing status for a worldbook
   */
  getIndexStatus(id: string): Observable<IndexStatusResponse> {
    return this.http.get<IndexStatusResponse>(`${this.apiUrl}/${id}/index/status`);
  }

  /**
   * Get pending changes for a worldbook
   */
  getPendingChanges(id: string): Observable<PendingChangesResponse> {
    return this.http.get<PendingChangesResponse>(`${this.apiUrl}/${id}/pending-changes`);
  }

  /**
   * Revert all pending changes for a worldbook
   */
  revertAllChanges(id: string): Observable<WorldbookResponseDto> {
    return this.http.post<WorldbookResponseDto>(`${this.apiUrl}/${id}/revert`, {});
  }

  /**
   * Revert a single lorebook to its indexed state
   */
  revertLorebook(worldbookId: string, lorebookId: string): Observable<LorebookResponseDto> {
    return this.http.post<LorebookResponseDto>(`${this.apiUrl}/${worldbookId}/lorebooks/${lorebookId}/revert`, {});
  }

  /**
   * Copy a worldbook with optional indexed volume
   */
  copyWorldbook(id: string, dto: CopyWorldbookDto): Observable<WorldbookResponseDto> {
    return this.http.post<WorldbookResponseDto>(`${this.apiUrl}/${id}/copy`, dto);
  }

  /**
   * Get visualization URL for an indexed worldbook
   */
  getVisualizationUrl(id: string): Observable<VisualizationResponse> {
    return this.http.get<VisualizationResponse>(`${this.apiUrl}/${id}/visualization`);
  }
}

export interface VisualizationResponse {
  worldbookId: string;
  visualizationUrl: string;
}
