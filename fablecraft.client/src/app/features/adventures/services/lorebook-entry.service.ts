import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {LorebookEntryResponseDto} from '../models/lorebook-entry.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LorebookEntryService {
  private readonly apiUrl = `${environment.apiUrl}/api/LorebookEntry`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all lorebook entries for an adventure
   */
  getEntriesByAdventure(adventureId: string): Observable<LorebookEntryResponseDto[]> {
    return this.http.get<LorebookEntryResponseDto[]>(`${this.apiUrl}/adventure/${adventureId}`);
  }

  /**
   * Get a single lorebook entry by ID
   */
  getEntryById(id: string): Observable<LorebookEntryResponseDto> {
    return this.http.get<LorebookEntryResponseDto>(`${this.apiUrl}/${id}`);
  }
}
