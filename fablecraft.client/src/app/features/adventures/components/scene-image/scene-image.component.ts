import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {SceneImage} from '../../models/adventure.model';
import {SceneImageService} from '../../services/scene-image.service';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-scene-image',
  standalone: false,
  templateUrl: './scene-image.component.html',
  styleUrl: './scene-image.component.css'
})
export class SceneImageComponent implements OnChanges, OnDestroy {
  @Input() adventureId: string | null = null;
  @Input() sceneId: string | null = null;
  @Input() isCurrentScene = false;
  @Output() imageGenerated = new EventEmitter<SceneImage>();

  images: SceneImage[] = [];
  selectedImage: SceneImage | null = null;
  isLoading = false;
  isGenerating = false;
  showGallery = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private sceneImageService: SceneImageService,
    private toastService: ToastService
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['adventureId'] || changes['sceneId']) && this.adventureId && this.sceneId) {
      this.loadImages();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadImages(): void {
    if (!this.adventureId || !this.sceneId) return;

    this.isLoading = true;
    this.error = null;

    this.sceneImageService.getImagesForScene(this.adventureId, this.sceneId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (images) => {
          this.images = images;
          this.selectedImage = images.find(img => img.isSelected) || images[0] || null;
        },
        error: (err) => {
          console.error('Failed to load images', err);
          this.error = 'Failed to load images';
        }
      });
  }

  generateImage(): void {
    if (!this.adventureId || !this.sceneId || this.isGenerating) return;

    this.isGenerating = true;
    this.error = null;

    this.sceneImageService.generateImage(this.adventureId, this.sceneId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isGenerating = false)
      )
      .subscribe({
        next: (image) => {
          // Check if generation is still in progress
          if (image.status === 'Generating' || image.status === 'Pending') {
            // Poll for completion
            this.pollForCompletion(image.id);
          } else {
            this.handleImageResult(image);
          }
        },
        error: (err) => {
          console.error('Failed to generate image', err);
          this.error = err.error?.error || 'Failed to generate image';
          this.toastService.show('error', this.error || 'Unknown error');
        }
      });
  }

  private pollForCompletion(imageId: string): void {
    // For now, just reload images - in production you might want WebSocket or SSE
    setTimeout(() => this.loadImages(), 2000);
  }

  private handleImageResult(image: SceneImage): void {
    if (image.status === 'Completed') {
      this.images.unshift(image);
      this.selectedImage = image;
      this.imageGenerated.emit(image);
      this.toastService.show('success', 'Image generated successfully');
    } else if (image.status === 'Failed') {
      this.error = image.errorMessage || 'Generation failed';
      this.toastService.show('error', this.error);
    }
    // Reload to get the latest state
    this.loadImages();
  }

  selectImage(image: SceneImage): void {
    if (!this.adventureId || !this.sceneId || image.status !== 'Completed') return;

    this.sceneImageService.selectImage(this.adventureId, this.sceneId, image.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updated) => {
          // Update local state
          this.images.forEach(img => img.isSelected = img.id === updated.id);
          this.selectedImage = updated;
        },
        error: (err) => {
          console.error('Failed to select image', err);
          this.toastService.show('error', 'Failed to select image');
        }
      });
  }

  deleteImage(image: SceneImage, event: Event): void {
    event.stopPropagation();
    if (!this.adventureId || !this.sceneId) return;

    if (!confirm('Delete this image version?')) return;

    this.sceneImageService.deleteImage(this.adventureId, this.sceneId, image.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.images = this.images.filter(img => img.id !== image.id);
          if (this.selectedImage?.id === image.id) {
            this.selectedImage = this.images.find(img => img.isSelected) || this.images[0] || null;
          }
          this.toastService.show('success', 'Image deleted');
        },
        error: (err) => {
          console.error('Failed to delete image', err);
          this.toastService.show('error', 'Failed to delete image');
        }
      });
  }

  toggleGallery(): void {
    this.showGallery = !this.showGallery;
  }

  get hasImages(): boolean {
    return this.images.length > 0;
  }

  get completedImages(): SceneImage[] {
    return this.images.filter(img => img.status === 'Completed');
  }

  formatDuration(ms: number): string {
    if (ms < 1000) return `${ms}ms`;
    const seconds = Math.round(ms / 1000);
    return `${seconds}s`;
  }
}
