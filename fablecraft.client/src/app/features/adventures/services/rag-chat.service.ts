import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type RagDatasetType = 'world' | 'main_character';

export interface RagChatRequest {
  query: string;
  datasetType: RagDatasetType;
}

export interface RagChatSource {
  datasetName: string;
  text: string;
}

export interface RagChatResponse {
  answer: string;
  sources: RagChatSource[];
}

export interface RagChatMessage {
  id: string;
  query: string;
  datasetType: RagDatasetType;
  response?: RagChatResponse;
  isLoading: boolean;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RagChatService {
  private readonly apiUrl = `${environment.apiUrl}/api/Character`;

  constructor(private http: HttpClient) {}

  search(adventureId: string, query: string, datasetType: RagDatasetType): Observable<RagChatResponse> {
    return this.http.post<RagChatResponse>(
      `${this.apiUrl}/${adventureId}/rag-chat`,
      { query, datasetType } as RagChatRequest
    );
  }
}
