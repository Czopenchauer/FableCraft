import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  TrackerDefinitionDto,
  TrackerDefinitionResponseDto,
  TrackerStructure
} from '../models/tracker-definition.model';

@Injectable({
  providedIn: 'root'
})
export class TrackerDefinitionService {
  private readonly apiUrl = `${environment.apiUrl}/api/TrackerDefinition`;

  constructor(private http: HttpClient) {}

  getAllDefinitions(): Observable<TrackerDefinitionResponseDto[]> {
    return this.http.get<TrackerDefinitionResponseDto[]>(this.apiUrl);
  }

  getDefinitionById(id: string): Observable<TrackerDefinitionResponseDto> {
    return this.http.get<TrackerDefinitionResponseDto>(`${this.apiUrl}/${id}`);
  }

  createDefinition(definition: TrackerDefinitionDto): Observable<TrackerDefinitionResponseDto> {
    return this.http.post<TrackerDefinitionResponseDto>(this.apiUrl, definition);
  }

  updateDefinition(id: string, definition: TrackerDefinitionDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, definition);
  }

  deleteDefinition(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getDefaultStructure(): Observable<TrackerStructure> {
    return this.http.get<TrackerStructure>(`${this.apiUrl}/default-structure`);
  }

  visualizeTracker(structure: TrackerStructure): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/visualize`, structure);
  }
}
