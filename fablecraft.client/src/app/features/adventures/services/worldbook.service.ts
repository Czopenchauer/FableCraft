import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorldbookDto, WorldbookResponseDto } from '../models/worldbook.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorldbookService {
  private readonly apiUrl = `${environment.apiUrl}/api/Worldbook`;

  constructor(private http: HttpClient) {}

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
  updateWorldbook(id: string, worldbook: WorldbookDto): Observable<WorldbookResponseDto> {
    return this.http.put<WorldbookResponseDto>(`${this.apiUrl}/${id}`, worldbook);
  }

  /**
   * Delete a worldbook
   */
  deleteWorldbook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
