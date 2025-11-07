import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { AdventureService } from '../../services/adventure.service';
import { GameScene } from '../../models/adventure.model';

@Component({
  selector: 'app-game-panel',
  standalone: false,
  templateUrl: './game-panel.component.html',
  styleUrl: './game-panel.component.css'
})
export class GamePanelComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  adventureId: string | null = null;
  currentScene: GameScene | null = null;
  isLoading = false;
  isInitialLoad = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adventureService: AdventureService
  ) {}

  ngOnInit(): void {
    this.adventureId = this.route.snapshot.paramMap.get('id');

    if (!this.adventureId) {
      this.router.navigate(['/adventures']);
      return;
    }

    this.loadFirstScene();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load the first scene of the adventure
   */
  loadFirstScene(): void {
    if (!this.adventureId) return;

    this.isLoading = true;
    this.error = null;

    this.adventureService.generateFirstScene(this.adventureId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.isInitialLoad = false;
        })
      )
      .subscribe({
        next: (scene) => {
          this.currentScene = scene;
        },
        error: (err) => {
          console.error('Error loading first scene:', err);
          this.error = 'Failed to load the scene. Please try again.';
        }
      });
  }

  /**
   * Handle player choice selection
   */
  onChoiceSelected(choice: string): void {
    if (!this.adventureId || this.isLoading) return;

    this.isLoading = true;
    this.error = null;

    this.adventureService.submitAction(this.adventureId, choice)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (scene) => {
          this.currentScene = scene;
        },
        error: (err) => {
          console.error('Error submitting action:', err);
          this.error = 'Failed to submit your choice. The action submission endpoint is not yet implemented in the backend.';
        }
      });
  }

  /**
   * Regenerate the current scene
   */
  onRegenerateScene(): void {
    if (!this.adventureId || this.isLoading || !this.currentScene) return;

    this.isLoading = true;
    this.error = null;

    this.adventureService.regenerateScene(this.adventureId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (scene) => {
          this.currentScene = scene;
        },
        error: (err) => {
          console.error('Error regenerating scene:', err);
          this.error = 'Failed to regenerate the scene. Please try again.';
        }
      });
  }

  /**
   * Delete the last scene and go back
   */
  onDeleteLastScene(): void {
    if (!this.adventureId || this.isLoading || !this.currentScene) return;

    if (!confirm('Are you sure you want to delete the last scene? This action cannot be undone.')) {
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.adventureService.deleteLastScene(this.adventureId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: () => {
          // After deleting, we should reload or go back
          // For now, let's navigate back to adventures list
          this.router.navigate(['/adventures']);
        },
        error: (err) => {
          console.error('Error deleting scene:', err);
          this.error = 'Failed to delete the scene. Please try again.';
        }
      });
  }

  /**
   * Navigate back to adventures list
   */
  goToAdventureList(): void {
    this.router.navigate(['/adventures']);
  }

  /**
   * Retry loading the scene after an error
   */
  retryLoad(): void {
    if (this.isInitialLoad) {
      this.loadFirstScene();
    }
  }
}
