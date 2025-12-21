import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '../../../../environments/environment';

export interface EmulateMainCharacterRequest {
  instruction: string;
}

export interface EmulateMainCharacterResponse {
  text: string;
}

@Injectable({
  providedIn: 'root'
})
export class CharacterService {
  private readonly apiUrl = `${environment.apiUrl}/api/Character`;

  constructor(private http: HttpClient) {
  }

  emulateMainCharacter(adventureId: string, instruction: string): Observable<EmulateMainCharacterResponse> {
    return this.http.post<EmulateMainCharacterResponse>(
      `${this.apiUrl}/${adventureId}/emulate-main-character`,
      {instruction} as EmulateMainCharacterRequest
    );
  }
}
