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
   * Generates a new image for a scene.
   */
  generateImage(adventureId: string, sceneId: string): Observable<SceneImage> {
    return this.http.post<SceneImage>(
      `${this.baseUrl}/${adventureId}/scene/${sceneId}/images`,
      {}
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
