import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '../../../../environments/environment';
import {
  CharacterListItem,
  CharacterDetail,
  CharacterMemory,
  CharacterSceneRewrite,
  CharacterImportance,
  CharacterTracker,
  PaginatedResponse
} from '../models/character.model';

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

  // Existing method
  emulateMainCharacter(adventureId: string, instruction: string): Observable<EmulateMainCharacterResponse> {
    return this.http.post<EmulateMainCharacterResponse>(
      `${this.apiUrl}/${adventureId}/emulate-main-character`,
      {instruction} as EmulateMainCharacterRequest
    );
  }

  // Get lightweight list of all characters for sidebar
  getCharacterList(adventureId: string): Observable<CharacterListItem[]> {
    return this.http.get<CharacterListItem[]>(
      `${this.apiUrl}/${adventureId}/list`
    );
  }

  // Get full character detail with paginated memories and scene rewrites
  getCharacterDetail(
    adventureId: string,
    characterId: string,
    memoriesLimit: number = 20,
    rewritesLimit: number = 20
  ): Observable<CharacterDetail> {
    return this.http.get<CharacterDetail>(
      `${this.apiUrl}/${adventureId}/${characterId}`,
      {
        params: {
          memoriesLimit: memoriesLimit.toString(),
          rewritesLimit: rewritesLimit.toString()
        }
      }
    );
  }

  // Get paginated memories for a character
  getCharacterMemories(
    adventureId: string,
    characterId: string,
    offset: number = 0,
    limit: number = 20
  ): Observable<PaginatedResponse<CharacterMemory>> {
    return this.http.get<PaginatedResponse<CharacterMemory>>(
      `${this.apiUrl}/${adventureId}/${characterId}/memories`,
      {
        params: {
          offset: offset.toString(),
          limit: limit.toString()
        }
      }
    );
  }

  // Get paginated scene rewrites for a character
  getCharacterSceneRewrites(
    adventureId: string,
    characterId: string,
    offset: number = 0,
    limit: number = 20
  ): Observable<PaginatedResponse<CharacterSceneRewrite>> {
    return this.http.get<PaginatedResponse<CharacterSceneRewrite>>(
      `${this.apiUrl}/${adventureId}/${characterId}/scene-rewrites`,
      {
        params: {
          offset: offset.toString(),
          limit: limit.toString()
        }
      }
    );
  }

  // Update character importance
  updateCharacterImportance(
    adventureId: string,
    characterId: string,
    importance: CharacterImportance
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${adventureId}/${characterId}/importance`,
      {importance}
    );
  }

  // Update character profile (description and character stats)
  updateCharacterProfile(
    adventureId: string,
    characterId: string,
    data: { description: string; characterStats: any }
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${adventureId}/${characterId}/profile`,
      data
    );
  }

  // Update character tracker
  updateCharacterTracker(
    adventureId: string,
    characterId: string,
    tracker: CharacterTracker
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${adventureId}/${characterId}/tracker`,
      {tracker}
    );
  }

  // Update a specific character memory
  updateCharacterMemory(
    adventureId: string,
    characterId: string,
    memoryId: string,
    data: { summary: string; salience: number; data: Record<string, any> | null }
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${adventureId}/${characterId}/memories/${memoryId}`,
      data
    );
  }

  // Update a specific character relationship
  updateCharacterRelationship(
    adventureId: string,
    characterId: string,
    relationshipId: string,
    data: { dynamic: string | null; data: Record<string, any> }
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${adventureId}/${characterId}/relationships/${relationshipId}`,
      data
    );
  }
}
