import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {LlmPresetDto, LlmPresetResponseDto, TestConnectionResponseDto} from '../models/llm-preset.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LlmPresetService {
  private readonly apiUrl = `${environment.apiUrl}/api/LlmPreset`;

  constructor(private http: HttpClient) {
  }

  /**
   * Get all LLM presets
   */
  getAllPresets(): Observable<LlmPresetResponseDto[]> {
    return this.http.get<LlmPresetResponseDto[]>(this.apiUrl);
  }

  /**
   * Get a single LLM preset by ID
   */
  getPresetById(id: string): Observable<LlmPresetResponseDto> {
    return this.http.get<LlmPresetResponseDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new LLM preset
   */
  createPreset(preset: LlmPresetDto): Observable<LlmPresetResponseDto> {
    return this.http.post<LlmPresetResponseDto>(this.apiUrl, preset);
  }

  /**
   * Update an existing LLM preset
   */
  updatePreset(id: string, preset: LlmPresetDto): Observable<LlmPresetResponseDto> {
    return this.http.put<LlmPresetResponseDto>(`${this.apiUrl}/${id}`, preset);
  }

  /**
   * Delete an LLM preset
   */
  deletePreset(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Test connection to an LLM preset
   */
  testConnection(preset: LlmPresetDto): Observable<TestConnectionResponseDto> {
    return this.http.post<TestConnectionResponseDto>(`${this.apiUrl}/test`, preset);
  }
}
