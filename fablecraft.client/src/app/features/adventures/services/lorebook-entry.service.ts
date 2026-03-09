import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  AdventureLoreResponseDto,
  BulkCreateLorebookEntriesDto,
  CreateLorebookEntryDto,
  LorebookEntryResponseDto,
  UpdateLorebookEntryDto
} from '../models/lorebook-entry.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LorebookEntryService {
  private readonly apiUrl = `${environment.apiUrl}/api/LorebookEntry`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all lorebook entries for an adventure including world settings
   */
  getAdventureLore(adventureId: string): Observable<AdventureLoreResponseDto> {
    return this.http.get<AdventureLoreResponseDto>(`${this.apiUrl}/adventure/${adventureId}`);
  }

  /**
   * Get a single lorebook entry by ID
   */
  getEntryById(id: string): Observable<LorebookEntryResponseDto> {
    return this.http.get<LorebookEntryResponseDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new lorebook entry for an adventure
   */
  createEntry(adventureId: string, entry: CreateLorebookEntryDto): Observable<LorebookEntryResponseDto> {
    return this.http.post<LorebookEntryResponseDto>(`${this.apiUrl}/adventure/${adventureId}`, entry);
  }

  /**
   * Create multiple lorebook entries for an adventure (bulk import)
   */
  bulkCreateEntries(adventureId: string, entries: CreateLorebookEntryDto[]): Observable<LorebookEntryResponseDto[]> {
    const dto: BulkCreateLorebookEntriesDto = {entries};
    return this.http.post<LorebookEntryResponseDto[]>(`${this.apiUrl}/adventure/${adventureId}/bulk`, dto);
  }

  /**
   * Update an existing lorebook entry
   */
  updateEntry(id: string, entry: UpdateLorebookEntryDto): Observable<LorebookEntryResponseDto> {
    return this.http.put<LorebookEntryResponseDto>(`${this.apiUrl}/${id}`, entry);
  }

  /**
   * Delete a lorebook entry
   */
  deleteEntry(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
