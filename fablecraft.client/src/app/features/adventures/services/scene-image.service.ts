import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '../../../../environments/environment';
import {SceneImage} from '../models/adventure.model';

@Injectable({
  providedIn: 'root'
})
export class SceneImageService {
  private readonly baseUrl = `${environment.apiUrl}/api/Play`;

  constructor(private http: HttpClient) {
  }

  /**
   * Gets all images for a scene.
   */
  getImagesForScene(adventureId: string, sceneId: string): Observable<SceneImage[]> {
    return this.http.get<SceneImage[]>(
      `${this.baseUrl}/${adventureId}/scene/${sceneId}/images`
    );
  }

  /**
   * Generates a new image for a scene. If `prompt` is provided, the backend
   * skips the AI prompt agent and uses the supplied prompt directly.
   */
  generateImage(
    adventureId: string,
    sceneId: string,
    prompt?: string,
    negativePrompt?: string
  ): Observable<SceneImage> {
    const body: { prompt?: string; negativePrompt?: string } = {};
    if (prompt && prompt.trim()) {
      body.prompt = prompt;
      if (negativePrompt && negativePrompt.trim()) {
        body.negativePrompt = negativePrompt;
      }
    }
    return this.http.post<SceneImage>(
      `${this.baseUrl}/${adventureId}/scene/${sceneId}/images`,
      body
    );
  }

  /**
   * Selects a specific image version as the active image.
   */
  selectImage(adventureId: string, sceneId: string, imageId: string): Observable<SceneImage> {
    return this.http.post<SceneImage>(
      `${this.baseUrl}/${adventureId}/scene/${sceneId}/images/${imageId}/select`,
      {}
    );
  }

  /**
   * Deletes a specific image version.
   */
  deleteImage(adventureId: string, sceneId: string, imageId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${adventureId}/scene/${sceneId}/images/${imageId}`
    );
  }
}
