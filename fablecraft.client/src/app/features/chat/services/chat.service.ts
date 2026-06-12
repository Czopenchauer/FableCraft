import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  ChatSessionDto,
  ChatSessionResponseDto,
  ChatSessionWithMessagesDto,
  ChatMessageResponseDto,
  UpdateChatSessionPresetDto
} from '../models/chat.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly apiUrl = `${environment.apiUrl}/api/chat`;

  constructor(private http: HttpClient) {
  }

  getSessions(): Observable<ChatSessionResponseDto[]> {
    return this.http.get<ChatSessionResponseDto[]>(`${this.apiUrl}/sessions`);
  }

  getSession(id: string): Observable<ChatSessionWithMessagesDto> {
    return this.http.get<ChatSessionWithMessagesDto>(`${this.apiUrl}/sessions/${id}`);
  }

  createSession(dto: ChatSessionDto): Observable<ChatSessionResponseDto> {
    return this.http.post<ChatSessionResponseDto>(`${this.apiUrl}/sessions`, dto);
  }

  updatePreset(id: string, dto: UpdateChatSessionPresetDto): Observable<ChatSessionResponseDto> {
    return this.http.put<ChatSessionResponseDto>(`${this.apiUrl}/sessions/${id}/preset`, dto);
  }

  deleteSession(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/sessions/${id}`);
  }

  deleteLatestMessage(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/sessions/${id}/messages/latest`);
  }

  sendMessage(sessionId: string, content: string): Observable<ChatMessageResponseDto> {
    return this.http.post<ChatMessageResponseDto>(`${this.apiUrl}/sessions/${sessionId}/messages`, {content});
  }
}