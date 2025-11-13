import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {AdventureService} from '../../services/adventure.service';
import {GameScene} from '../../models/adventure.model';

@Component({
  selector: 'app-game-panel',
  standalone: false,
  templateUrl: './game-panel.component.html',
  styleUrl: './game-panel.component.css'
})
export class GamePanelComponent implements OnInit, OnDestroy {
  adventureId: string | null = null;
  adventureName: string = '';
  currentScene: GameScene | null = null;
  isLoading = false;
  isInitialLoad = true;
  isRegenerating = false;
  error: string | null = null;
  customAction = '';
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adventureService: AdventureService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.adventureId = this.route.snapshot.paramMap.get('id');

    if (!this.adventureId) {
      this.router.navigate(['/adventures']);
      return;
    }

    this.loadAdventureName();
    this.loadFirstScene();
  }

  /**
   * Load the adventure name
   */
  loadAdventureName(): void {
    if (!this.adventureId) return;

    this.adventureService.getAllAdventures()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (adventures) => {
          const adventure = adventures.find(a => a.adventureId === this.adventureId);
          if (adventure) {
            this.adventureName = adventure.name;
          }
        },
        error: (err) => {
          console.error('Error loading adventure name:', err);
          this.adventureName = 'Your Adventure';
        }
      });
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
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (scene) => {
          console.log('New scene received:', scene);
          this.currentScene = scene;
          this.customAction = ''; // Reset custom action
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error submitting action:', err);
          this.error = 'Failed to submit your choice. Please try again.';
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Handle custom action submission
   */
  onCustomActionSubmit(): void {
    if (!this.customAction.trim()) return;
    this.onChoiceSelected(this.customAction);
  }

  /**
   * Handle Enter key press in custom action input
   */
  onCustomActionKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onCustomActionSubmit();
    }
  }

  /**
   * Regenerate the current scene
   */
  onRegenerateScene(): void {
    if (!this.adventureId || this.isLoading || this.isRegenerating || !this.currentScene) return;

    this.isRegenerating = true;
    this.error = null;

    this.adventureService.regenerateScene(this.adventureId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isRegenerating = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (scene) => {
          console.log('Regenerated scene received:', scene);
          this.currentScene = scene;
          this.customAction = ''; // Reset custom action
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error regenerating scene:', err);
          this.error = 'Failed to regenerate the scene. Please try again.';
          this.cdr.detectChanges();
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
