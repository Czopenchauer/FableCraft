import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {AdventureService} from '../../services/adventure.service';
import {GameScene, SceneEnrichmentResult} from '../../models/adventure.model';
import {ToastService} from '../../../../core/services/toast.service';

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
  isEnriching = false;
  enrichmentFailed = false;
  enrichmentData: SceneEnrichmentResult | null = null;
  customAction = '';

  // Character tracker tabs
  activeCharacterTab: string = 'protagonist';

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adventureService: AdventureService,
    private cdr: ChangeDetectorRef,
    private toastService: ToastService
  ) {
  }

  ngOnInit(): void {
    this.adventureId = this.route.snapshot.paramMap.get('id');

    if (!this.adventureId) {
      this.router.navigate(['/adventures']);
      return;
    }

    this.loadAdventureName();
    this.loadCurrentScene();
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
   * Load the current scene of the adventure
   */
  loadCurrentScene(): void {
    if (!this.adventureId) return;

    this.isLoading = true;

    this.adventureService.getCurrentScene(this.adventureId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.isInitialLoad = false;
        })
      )
      .subscribe({
        next: (scene) => {
          // API returns the current GameScene
          this.currentScene = scene || null;

          // Check if tracker is missing and trigger enrichment if needed
          if (this.currentScene && !this.currentScene.tracker) {
            this.enrichSceneData(this.currentScene.sceneId);
          }
        },
        error: (err) => {
          console.error('Error loading current scene:', err);
          this.toastService.error('Failed to load the scene. Please try again.');
        }
      });
  }

  /**
   * Handle player choice selection
   */
  onChoiceSelected(choice: string): void {
    if (!this.adventureId || this.isLoading || this.isEnriching || this.isActionBlocked()) return;

    this.isLoading = true;
    this.enrichmentData = null; // Reset enrichment data
    this.enrichmentFailed = false; // Reset enrichment failure state

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
          this.resetCharacterTab(); // Reset to protagonist tab
          this.cdr.detectChanges();

          // Immediately start enriching the scene
          this.enrichSceneData(scene.sceneId);
        },
        error: (err) => {
          console.error('Error submitting action:', err);
          this.toastService.error('Failed to submit your choice. Please try again.');
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Enrich the scene with tracker and additional content
   */
  private enrichSceneData(sceneId: string): void {
    if (!this.adventureId) return;

    this.isEnriching = true;
    this.enrichmentFailed = false;

    this.adventureService.enrichScene(this.adventureId, sceneId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isEnriching = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (enrichment) => {
          console.log('Scene enrichment received:', enrichment);
          this.enrichmentData = enrichment;

          // Update the current scene's tracker with enriched data
          if (this.currentScene) {
            this.currentScene.tracker = enrichment.tracker;
          }

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error enriching scene:', err);
          this.enrichmentFailed = true;
          this.toastService.warning('Scene enrichment failed. The scene is still playable.');
        }
      });
  }

  /**
   * Retry enrichment after failure
   */
  retryEnrichment(): void {
    if (!this.currentScene || this.isEnriching) return;
    this.enrichSceneData(this.currentScene.sceneId);
  }

  /**
   * Check if actions should be blocked
   */
  isActionBlocked(): boolean {
    // Block if enrichment is ongoing, tracker is null, or viewing a historical scene
    return this.isEnriching || !this.currentScene?.tracker || !this.isCurrentScene();
  }

  /**
   * Check if a choice was the one selected
   */
  isSelectedChoice(choice: string): boolean {
    return this.currentScene?.selectedChoice === choice;
  }

  /**
   * Check if the selected choice was a custom action (not in predefined choices)
   */
  isCustomActionSelected(): boolean {
    if (!this.currentScene?.selectedChoice) return false;

    const predefinedChoices = this.currentScene.choices || [];
    return !predefinedChoices.includes(this.currentScene.selectedChoice);
  }

  /**
   * Handle custom action submission
   */
  onCustomActionSubmit(): void {
    if (!this.customAction.trim() || this.isActionBlocked()) return;
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

    this.adventureService.regenerateScene(this.adventureId, this.currentScene.sceneId)
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
          this.toastService.error('Failed to regenerate the scene. Please try again.');
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

    this.adventureService.deleteLastScene(this.adventureId, this.currentScene.sceneId)
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
          this.toastService.error('Failed to delete the scene. Please try again.');
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
   * Navigate to the previous scene
   */
  goToPreviousScene(): void {
    if (!this.adventureId || !this.currentScene?.previousScene || this.isLoading) return;

    this.loadScene(this.currentScene.previousScene);
  }

  /**
   * Navigate to the next scene
   */
  goToNextScene(): void {
    if (!this.adventureId || !this.currentScene?.nextScene || this.isLoading) return;

    this.loadScene(this.currentScene.nextScene);
  }

  /**
   * Navigate to the current (latest) scene
   */
  goToCurrentScene(): void {
    if (!this.adventureId || this.isLoading) return;

    this.loadCurrentScene();
  }

  /**
   * Load a specific scene by ID
   */
  private loadScene(sceneId: string): void {
    if (!this.adventureId) return;

    this.isLoading = true;
    this.enrichmentData = null;
    this.enrichmentFailed = false;

    this.adventureService.getScene(this.adventureId, sceneId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (scene) => {
          this.currentScene = scene || null;
          this.resetCharacterTab(); // Reset to protagonist tab

          // Check if tracker is missing and trigger enrichment if needed
          if (this.currentScene && !this.currentScene.tracker) {
            this.enrichSceneData(this.currentScene.sceneId);
          }

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error loading scene:', err);
          this.toastService.error('Failed to load the scene. Please try again.');
        }
      });
  }

  /**
   * Check if currently viewing the latest scene
   */
  isCurrentScene(): boolean {
    return !this.currentScene?.nextScene;
  }

  /**
   * Get the list of characters present in the scene for tabs
   */
  getCharactersOnScene(): any[] {
    if (!this.currentScene?.tracker?.charactersPresent) return [];
    return this.currentScene.tracker.charactersPresent;
  }

  /**
   * Get character data by name from the characters array
   */
  getCharacterByName(name: string): any | null {
    if (!this.currentScene?.tracker?.characters) return null;
    return this.currentScene.tracker.characters.find((c: any) => c.name === name) || null;
  }

  /**
   * Set the active character tab
   */
  setActiveCharacterTab(tabId: string): void {
    this.activeCharacterTab = tabId;
  }

  /**
   * Check if a tab is active
   */
  isActiveCharacterTab(tabId: string): boolean {
    return this.activeCharacterTab === tabId;
  }

  /**
   * Reset to protagonist tab when scene changes
   */
  private resetCharacterTab(): void {
    this.activeCharacterTab = 'protagonist';
  }
}
