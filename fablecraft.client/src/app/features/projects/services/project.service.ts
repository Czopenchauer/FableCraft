import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '../../../../environments/environment';
import {
  ProjectDto,
  ProjectResponseDto,
  ProjectUpdateDto,
  ProjectFileDto,
  ProjectFileUpdateDto,
  ProjectFileResponseDto,
  ProjectFileSummaryDto,
  ProjectChatSessionDto,
  ProjectChatSessionResponseDto,
  ProjectChatMessageDto,
  ProjectChatMessageEntry,
  IndexingStatusResponse
} from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly apiUrl = `${environment.apiUrl}/api/projects`;

  constructor(private http: HttpClient) {
  }

  getAllProjects(): Observable<ProjectResponseDto[]> {
    return this.http.get<ProjectResponseDto[]>(this.apiUrl);
  }

  getProject(id: string): Observable<ProjectResponseDto> {
    return this.http.get<ProjectResponseDto>(`${this.apiUrl}/${id}`);
  }

  createProject(dto: ProjectDto): Observable<ProjectResponseDto> {
    return this.http.post<ProjectResponseDto>(this.apiUrl, dto);
  }

  updateProject(id: string, dto: ProjectUpdateDto): Observable<ProjectResponseDto> {
    return this.http.put<ProjectResponseDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteProject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  listFiles(projectId: string, category?: string): Observable<ProjectFileSummaryDto[]> {
    const params: any = {};
    if (category) {
      params.category = category;
    }
    return this.http.get<ProjectFileSummaryDto[]>(`${this.apiUrl}/${projectId}/files`, {params});
  }

  getFile(projectId: string, fileId: string): Observable<ProjectFileResponseDto> {
    return this.http.get<ProjectFileResponseDto>(`${this.apiUrl}/${projectId}/files/${fileId}`);
  }

  createFile(projectId: string, dto: ProjectFileDto): Observable<ProjectFileResponseDto> {
    return this.http.post<ProjectFileResponseDto>(`${this.apiUrl}/${projectId}/files`, dto);
  }

  updateFile(projectId: string, fileId: string, dto: ProjectFileUpdateDto): Observable<ProjectFileResponseDto> {
    return this.http.put<ProjectFileResponseDto>(`${this.apiUrl}/${projectId}/files/${fileId}`, dto);
  }

  deleteFile(projectId: string, fileId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/files/${fileId}`);
  }

  getChatSessions(projectId: string): Observable<ProjectChatSessionResponseDto[]> {
    return this.http.get<ProjectChatSessionResponseDto[]>(`${this.apiUrl}/${projectId}/chat/sessions`);
  }

  getChatSession(projectId: string, sessionId: string): Observable<ProjectChatSessionResponseDto> {
    return this.http.get<ProjectChatSessionResponseDto>(`${this.apiUrl}/${projectId}/chat/sessions/${sessionId}`);
  }

  createChatSession(projectId: string, dto: ProjectChatSessionDto): Observable<ProjectChatSessionResponseDto> {
    return this.http.post<ProjectChatSessionResponseDto>(`${this.apiUrl}/${projectId}/chat/sessions`, dto);
  }

  deleteChatSession(projectId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/chat/sessions/${sessionId}`);
  }

  sendChatMessage(projectId: string, sessionId: string, content: string): Observable<ProjectChatMessageEntry> {
    const dto: ProjectChatMessageDto = {content};
    return this.http.post<ProjectChatMessageEntry>(`${this.apiUrl}/${projectId}/chat/sessions/${sessionId}/messages`, dto);
  }

  startIndexing(projectId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${projectId}/index`, {});
  }

  getIndexingStatus(projectId: string): Observable<IndexingStatusResponse> {
    return this.http.get<IndexingStatusResponse>(`${this.apiUrl}/${projectId}/index/status`);
  }
}