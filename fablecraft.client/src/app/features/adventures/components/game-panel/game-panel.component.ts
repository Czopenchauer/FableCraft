import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {AdventureService} from '../../services/adventure.service';
import {CharacterService} from '../../services/character.service';
import {RagChatService, RagDatasetType, RagChatMessage} from '../../services/rag-chat.service';
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
  isRegeneratingEnrichment = false;
  enrichmentFailed = false;
  enrichmentData: SceneEnrichmentResult | null = null;
  customAction = '';
  private previousTrackerData: any = null;

  // Character tracker tabs
  activeCharacterTab: string = 'protagonist';
  // Nested tabs for character details (state, development, description)
  activeNestedTab: string = 'state';

  // Settings modal state
  showSettingsModal = false;

  // Lore modal state
  showLoreModal = false;

  // Regenerate enrichment dropdown state
  showRegenerateDropdown = false;
  selectedAgents: Set<string> = new Set(['SceneTracker', 'MainCharacterTracker', 'CharacterTracker']);

  // Available agents for regeneration
  readonly availableAgents = [
    { id: 'SceneTracker', label: 'Scene Tracker', description: 'Time, location, weather, characters present' },
    { id: 'MainCharacterTracker', label: 'Protagonist State', description: 'Main character tracker & description' },
    { id: 'CharacterTracker', label: 'Side Characters', description: 'All side character trackers' }
  ];

  // Character emulation state
  private readonly EMULATION_VISIBLE_KEY = 'game-panel-emulation-visible';
  showEmulationBox = false;
  emulationInstruction = '';
  emulationResponse = '';
  isEmulating = false;

  // RAG Knowledge Chat state
  private readonly RAG_CHAT_VISIBLE_KEY = 'game-panel-rag-chat-visible';
  showRagChatBox = false;
  ragChatQuery = '';
  ragChatDataset: RagDatasetType = 'world';
  ragChatMessages: RagChatMessage[] = [];
  isRagChatLoading = false;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adventureService: AdventureService,
    private characterService: CharacterService,
    private ragChatService: RagChatService,
    private cdr: ChangeDetectorRef,
    private toastService: ToastService
  ) {
    this.loadEmulationVisibility();
    this.loadRagChatVisibility();
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
   * Regenerate enrichment for specific agents
   */
  onRegenerateEnrichment(agentsToRegenerate: string[]): void {
    if (!this.adventureId || !this.currentScene || this.isRegeneratingEnrichment || this.isEnriching) return;

    // Store the current tracker data for fallback
    this.previousTrackerData = this.currentScene.tracker ? JSON.parse(JSON.stringify(this.currentScene.tracker)) : null;

    this.isRegeneratingEnrichment = true;

    this.adventureService.regenerateEnrichment(this.adventureId, this.currentScene.sceneId, agentsToRegenerate)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isRegeneratingEnrichment = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (enrichment) => {
          console.log('Regenerated enrichment received:', enrichment);
          this.enrichmentData = enrichment;

          // Update the current scene's tracker with regenerated data
          if (this.currentScene) {
            this.currentScene.tracker = enrichment.tracker;
          }

          this.previousTrackerData = null; // Clear the backup
          this.toastService.success('Enrichment regenerated successfully.');
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error regenerating enrichment:', err);

          // Restore previous tracker data on failure
          if (this.currentScene && this.previousTrackerData) {
            this.currentScene.tracker = this.previousTrackerData;
          }

          this.previousTrackerData = null;
          this.toastService.error('Failed to regenerate enrichment. Previous values restored.');
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Check if regenerate enrichment button should be shown
   */
  canRegenerateEnrichment(): boolean {
    return !!(this.currentScene?.tracker && !this.isEnriching && !this.isRegeneratingEnrichment && !this.isLoading && !this.isRegenerating);
  }

  /**
   * Toggle the regenerate dropdown menu
   */
  toggleRegenerateDropdown(): void {
    this.showRegenerateDropdown = !this.showRegenerateDropdown;
  }

  /**
   * Close the regenerate dropdown
   */
  closeRegenerateDropdown(): void {
    this.showRegenerateDropdown = false;
  }

  /**
   * Toggle agent selection
   */
  toggleAgentSelection(agentId: string): void {
    if (this.selectedAgents.has(agentId)) {
      this.selectedAgents.delete(agentId);
    } else {
      this.selectedAgents.add(agentId);
    }
  }

  /**
   * Check if an agent is selected
   */
  isAgentSelected(agentId: string): boolean {
    return this.selectedAgents.has(agentId);
  }

  /**
   * Regenerate with selected agents
   */
  regenerateSelectedAgents(): void {
    if (this.selectedAgents.size === 0) {
      this.toastService.warning('Please select at least one agent to regenerate.');
      return;
    }

    this.closeRegenerateDropdown();
    this.onRegenerateEnrichment(Array.from(this.selectedAgents));
  }

  /**
   * Select all agents
   */
  selectAllAgents(): void {
    this.availableAgents.forEach(agent => this.selectedAgents.add(agent.id));
  }

  /**
   * Deselect all agents
   */
  deselectAllAgents(): void {
    this.selectedAgents.clear();
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
   * Handle key down in custom action textarea
   * Enter submits, Shift+Enter adds a new line
   */
  onCustomActionKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
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
  getCharactersOnScene(): string[] {
    if (!this.currentScene?.tracker?.scene?.charactersPresent) return [];
    return this.currentScene.tracker.scene.charactersPresent;
  }

  /**
   * Get character data by name from the characters array
   */
  getCharacterByName(name: string): any | null {
    if (!this.currentScene?.tracker?.characters) return null;
    return this.currentScene.tracker.characters.find(c => c.name === name) || null;
  }

  /**
   * Set the active character tab
   */
  setActiveCharacterTab(tabId: string): void {
    this.activeCharacterTab = tabId;
    // Reset nested tab when switching characters
    this.activeNestedTab = 'state';
  }

  /**
   * Check if a tab is active
   */
  isActiveCharacterTab(tabId: string): boolean {
    return this.activeCharacterTab === tabId;
  }

  /**
   * Set the active nested tab (description, state, abilities)
   */
  setActiveNestedTab(tabId: string): void {
    this.activeNestedTab = tabId;
  }

  /**
   * Check if a nested tab is active
   */
  isActiveNestedTab(tabId: string): boolean {
    return this.activeNestedTab === tabId;
  }

  /**
   * Reset to protagonist tab when scene changes
   */
  private resetCharacterTab(): void {
    this.activeCharacterTab = 'protagonist';
    this.activeNestedTab = 'state';
  }

  /**
   * Open the settings modal
   */
  openSettingsModal(): void {
    this.showSettingsModal = true;
  }

  /**
   * Close the settings modal
   */
  closeSettingsModal(): void {
    this.showSettingsModal = false;
  }

  /**
   * Handle settings saved event
   */
  onSettingsSaved(): void {
    // Optionally reload data or show feedback
    this.closeSettingsModal();
  }

  /**
   * Open the lore management modal
   */
  openLoreModal(): void {
    this.showLoreModal = true;
  }

  /**
   * Close the lore management modal
   */
  closeLoreModal(): void {
    this.showLoreModal = false;
  }

  /**
   * Load emulation box visibility from localStorage
   */
  private loadEmulationVisibility(): void {
    const stored = localStorage.getItem(this.EMULATION_VISIBLE_KEY);
    this.showEmulationBox = stored === 'true';
  }

  /**
   * Toggle emulation box visibility
   */
  toggleEmulationBox(): void {
    this.showEmulationBox = !this.showEmulationBox;
    localStorage.setItem(this.EMULATION_VISIBLE_KEY, String(this.showEmulationBox));
  }

  /**
   * Submit emulation instruction
   */
  onEmulateMainCharacter(): void {
    if (!this.adventureId || !this.emulationInstruction.trim() || this.isEmulating) return;

    this.isEmulating = true;
    this.emulationResponse = '';

    this.characterService.emulateMainCharacter(this.adventureId, this.emulationInstruction.trim())
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isEmulating = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response) => {
          this.emulationResponse = response.text;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error emulating character:', err);
          this.toastService.error('Failed to emulate character. Please try again.');
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Handle keydown in emulation instruction textarea
   */
  onEmulationKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onEmulateMainCharacter();
    }
  }

  /**
   * Clear emulation response
   */
  clearEmulationResponse(): void {
    this.emulationResponse = '';
  }

  /**
   * Load RAG chat box visibility from localStorage
   */
  private loadRagChatVisibility(): void {
    const stored = localStorage.getItem(this.RAG_CHAT_VISIBLE_KEY);
    this.showRagChatBox = stored === 'true';
  }

  /**
   * Toggle RAG chat box visibility
   */
  toggleRagChatBox(): void {
    this.showRagChatBox = !this.showRagChatBox;
    localStorage.setItem(this.RAG_CHAT_VISIBLE_KEY, String(this.showRagChatBox));
  }

  /**
   * Set the RAG chat dataset type
   */
  setRagChatDataset(dataset: RagDatasetType): void {
    this.ragChatDataset = dataset;
  }

  /**
   * Submit RAG chat query
   */
  onRagChatSubmit(): void {
    if (!this.adventureId || !this.ragChatQuery.trim() || this.isRagChatLoading) return;

    const messageId = Date.now().toString();
    const message: RagChatMessage = {
      id: messageId,
      query: this.ragChatQuery.trim(),
      datasetType: this.ragChatDataset,
      isLoading: true
    };

    this.ragChatMessages.push(message);
    this.isRagChatLoading = true;
    const queryText = this.ragChatQuery.trim();
    this.ragChatQuery = '';

    this.ragChatService.search(this.adventureId, queryText, this.ragChatDataset)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isRagChatLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response) => {
          const idx = this.ragChatMessages.findIndex(m => m.id === messageId);
          if (idx !== -1) {
            this.ragChatMessages[idx] = {
              ...this.ragChatMessages[idx],
              response,
              isLoading: false
            };
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error in RAG chat:', err);
          const idx = this.ragChatMessages.findIndex(m => m.id === messageId);
          if (idx !== -1) {
            this.ragChatMessages[idx] = {
              ...this.ragChatMessages[idx],
              error: 'Failed to get response. Please try again.',
              isLoading: false
            };
          }
          this.toastService.error('RAG search failed. Please try again.');
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Handle keydown in RAG chat input
   */
  onRagChatKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onRagChatSubmit();
    }
  }

  /**
   * Clear RAG chat messages
   */
  clearRagChatMessages(): void {
    this.ragChatMessages = [];
  }
}
