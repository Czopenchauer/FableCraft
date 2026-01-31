import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  GraphRagSettingsDto,
  GraphRagSettingsResponseDto,
  GraphRagSettingsSummaryDto
} from '../models/graph-rag-settings.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GraphRagSettingsService {
  private readonly apiUrl = `${environment.apiUrl}/api/GraphRagSettings`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all GraphRAG settings
   */
  getAll(): Observable<GraphRagSettingsResponseDto[]> {
    return this.http.get<GraphRagSettingsResponseDto[]>(this.apiUrl);
  }

  /**
   * Get all GraphRAG settings (summary only - for dropdowns)
   */
  getAllSummary(): Observable<GraphRagSettingsSummaryDto[]> {
    return this.http.get<GraphRagSettingsSummaryDto[]>(`${this.apiUrl}/summary`);
  }

  /**
   * Get a single GraphRAG settings by ID
   */
  getById(id: string): Observable<GraphRagSettingsResponseDto> {
    return this.http.get<GraphRagSettingsResponseDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new GraphRAG settings
   */
  create(settings: GraphRagSettingsDto): Observable<GraphRagSettingsResponseDto> {
    return this.http.post<GraphRagSettingsResponseDto>(this.apiUrl, settings);
  }

  /**
   * Update existing GraphRAG settings
   */
  update(id: string, settings: GraphRagSettingsDto): Observable<GraphRagSettingsResponseDto> {
    return this.http.put<GraphRagSettingsResponseDto>(`${this.apiUrl}/${id}`, settings);
  }

  /**
   * Delete GraphRAG settings
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
