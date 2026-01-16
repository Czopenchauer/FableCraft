import {Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {LlmLogListResponseDto} from '../models/llm-log.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LlmLogService {
  private readonly apiUrl = `${environment.apiUrl}/api/LlmLog`;

  constructor(private http: HttpClient) {
  }

  getLogsBySceneId(sceneId: string, offset: number = 0, limit: number = 50): Observable<LlmLogListResponseDto> {
    const params = new HttpParams()
      .set('offset', offset.toString())
      .set('limit', limit.toString());

    return this.http.get<LlmLogListResponseDto>(`${this.apiUrl}/scene/${sceneId}`, {params});
  }
}
