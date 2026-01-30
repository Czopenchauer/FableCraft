import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {AdventureService} from '../../services/adventure.service';
import {CharacterService} from '../../services/character.service';
import {RagChatMessage, RagChatService, RagDatasetType} from '../../services/rag-chat.service';
import {GameScene, SceneEnrichmentResult, TrackerDto} from '../../models/adventure.model';
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
  // Character tracker tabs
  activeCharacterTab: string = 'protagonist';
  // Nested tabs for character details (state, development, description)
  activeNestedTab: string = 'state';
  // Settings modal state
  showSettingsModal = false;
  // Adventure State modal state (Lore + Characters)
  showAdventureStateModal = false;
  // Scene Edit modal state
  showSceneEditModal = false;
  // Regenerate enrichment dropdown state
  showRegenerateDropdown = false;
  selectedAgents: Set<string> = new Set(['SceneTracker', 'MainCharacterTracker', 'CharacterTracker']);
  // Available agents for regeneration
  readonly availableAgents = [
    {id: 'SceneTracker', label: 'Scene Tracker', description: 'Time, location, weather, characters present'},
    {id: 'MainCharacterTracker', label: 'Protagonist State', description: 'Main character tracker & description'},
    {id: 'CharacterTracker', label: 'Side Characters', description: 'All side character trackers'}
  ];
  showEmulationBox = false;
  emulationInstruction = '';
  emulationResponse = '';
  isEmulating = false;
  showRagChatBox = false;
  ragChatQuery = '';
  ragChatDataset: RagDatasetType = 'world';
  ragChatMessages: RagChatMessage[] = [];
  isRagChatLoading = false;
  private previousTrackerData: any = null;
  // For stale display and diff comparison
  previousTrackerForDiff: TrackerDto | null = null;
  showTrackerDiff = false;
  hasDiffAvailable = false;
  // Character emulation state
  private readonly EMULATION_VISIBLE_KEY = 'game-panel-emulation-visible';
  // RAG Knowledge Chat state
  private readonly RAG_CHAT_VISIBLE_KEY = 'game-panel-rag-chat-visible';
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
    // Reset diff state while loading
    this.showTrackerDiff = false;

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
          } else {
            // Tracker exists, fetch previous scene for diff comparison
            this.fetchPreviousSceneTracker();
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
   * Toggle tracker diff view
   */
  toggleTrackerDiff(): void {
    this.showTrackerDiff = !this.showTrackerDiff;
  }

  /**
   * Compute diff between previous and current tracker, filtered by active tab
   */
  computeTrackerDiff(): DiffItem[] {
    if (!this.previousTrackerForDiff || !this.currentScene?.tracker) {
      return [];
    }

    // Get the appropriate section based on active tab
    if (this.activeCharacterTab === 'scene') {
      return this.deepDiff(
        this.previousTrackerForDiff.scene,
        this.currentScene.tracker.scene,
        'scene'
      );
    } else if (this.activeCharacterTab === 'protagonist') {
      return this.deepDiff(
        this.previousTrackerForDiff.mainCharacter,
        this.currentScene.tracker.mainCharacter,
        'protagonist'
      );
    } else {
      // Find the character by name in both trackers
      const prevChar = this.previousTrackerForDiff.characters?.find(
        c => c.name === this.activeCharacterTab
      );
      const currChar = this.currentScene.tracker.characters?.find(
        c => c.name === this.activeCharacterTab
      );

      if (!prevChar && !currChar) {
        return [];
      }

      if (!prevChar) {
        return [{type: 'added', path: this.activeCharacterTab, newValue: currChar}];
      }

      if (!currChar) {
        return [{type: 'removed', path: this.activeCharacterTab, oldValue: prevChar}];
      }

      return this.deepDiff(prevChar, currChar, this.activeCharacterTab);
    }
  }

  /**
   * Recursively compute diff between two objects
   */
  private deepDiff(prev: any, curr: any, path: string): DiffItem[] {
    const diffs: DiffItem[] = [];

    if (prev === curr) return diffs;

    if (typeof prev !== typeof curr) {
      diffs.push({type: 'modified', path: path || 'root', oldValue: prev, newValue: curr});
      return diffs;
    }

    if (Array.isArray(prev) && Array.isArray(curr)) {
      // Compare arrays
      const maxLen = Math.max(prev.length, curr.length);
      for (let i = 0; i < maxLen; i++) {
        const itemPath = path ? `${path}[${i}]` : `[${i}]`;
        if (i >= prev.length) {
          diffs.push({type: 'added', path: itemPath, newValue: curr[i]});
        } else if (i >= curr.length) {
          diffs.push({type: 'removed', path: itemPath, oldValue: prev[i]});
        } else if (typeof prev[i] === 'object' && typeof curr[i] === 'object' && prev[i] !== null && curr[i] !== null) {
          diffs.push(...this.deepDiff(prev[i], curr[i], itemPath));
        } else if (prev[i] !== curr[i]) {
          diffs.push({type: 'modified', path: itemPath, oldValue: prev[i], newValue: curr[i]});
        }
      }
      return diffs;
    }

    if (typeof prev === 'object' && prev !== null && typeof curr === 'object' && curr !== null) {
      const allKeys = new Set([...Object.keys(prev), ...Object.keys(curr)]);
      for (const key of allKeys) {
        const keyPath = path ? `${path}.${key}` : key;
        if (!(key in prev)) {
          diffs.push({type: 'added', path: keyPath, newValue: curr[key]});
        } else if (!(key in curr)) {
          diffs.push({type: 'removed', path: keyPath, oldValue: prev[key]});
        } else {
          diffs.push(...this.deepDiff(prev[key], curr[key], keyPath));
        }
      }
      return diffs;
    }

    if (prev !== curr) {
      diffs.push({type: 'modified', path: path || 'root', oldValue: prev, newValue: curr});
    }

    return diffs;
  }

  /**
   * Format value for diff display
   */
  formatDiffValue(value: any): string {
    if (value === null || value === undefined) return 'null';
    if (typeof value === 'object') return JSON.stringify(value, null, 2);
    return String(value);
  }

  /**
   * Compute word-level diff between two strings
   * Returns array of segments with type: 'same', 'added', 'removed'
   */
  computeWordDiff(oldStr: string, newStr: string): WordDiffSegment[] {
    if (oldStr === newStr) {
      return [{type: 'same', text: newStr}];
    }

    const oldWords = this.tokenize(oldStr);
    const newWords = this.tokenize(newStr);

    // Use longest common subsequence algorithm
    const lcs = this.computeLCS(oldWords, newWords);
    const segments: WordDiffSegment[] = [];

    let oldIdx = 0;
    let newIdx = 0;
    let lcsIdx = 0;

    while (oldIdx < oldWords.length || newIdx < newWords.length) {
      if (lcsIdx < lcs.length) {
        // Add removed words (in old but not matching LCS)
        while (oldIdx < oldWords.length && oldWords[oldIdx] !== lcs[lcsIdx]) {
          segments.push({type: 'removed', text: oldWords[oldIdx]});
          oldIdx++;
        }
        // Add inserted words (in new but not matching LCS)
        while (newIdx < newWords.length && newWords[newIdx] !== lcs[lcsIdx]) {
          segments.push({type: 'added', text: newWords[newIdx]});
          newIdx++;
        }
        // Add common word
        if (lcsIdx < lcs.length) {
          segments.push({type: 'same', text: lcs[lcsIdx]});
          oldIdx++;
          newIdx++;
          lcsIdx++;
        }
      } else {
        // Remaining words after LCS is exhausted
        while (oldIdx < oldWords.length) {
          segments.push({type: 'removed', text: oldWords[oldIdx]});
          oldIdx++;
        }
        while (newIdx < newWords.length) {
          segments.push({type: 'added', text: newWords[newIdx]});
          newIdx++;
        }
      }
    }

    return this.mergeSegments(segments);
  }

  /**
   * Tokenize string into words, preserving spaces and punctuation
   */
  private tokenize(str: string): string[] {
    // Split by word boundaries but keep delimiters
    return str.split(/(\s+|[.,;:!?'"()\[\]{}])/g).filter(t => t.length > 0);
  }

  /**
   * Compute Longest Common Subsequence
   */
  private computeLCS(arr1: string[], arr2: string[]): string[] {
    const m = arr1.length;
    const n = arr2.length;
    const dp: number[][] = Array(m + 1).fill(null).map(() => Array(n + 1).fill(0));

    for (let i = 1; i <= m; i++) {
      for (let j = 1; j <= n; j++) {
        if (arr1[i - 1] === arr2[j - 1]) {
          dp[i][j] = dp[i - 1][j - 1] + 1;
        } else {
          dp[i][j] = Math.max(dp[i - 1][j], dp[i][j - 1]);
        }
      }
    }

    // Backtrack to find LCS
    const lcs: string[] = [];
    let i = m, j = n;
    while (i > 0 && j > 0) {
      if (arr1[i - 1] === arr2[j - 1]) {
        lcs.unshift(arr1[i - 1]);
        i--;
        j--;
      } else if (dp[i - 1][j] > dp[i][j - 1]) {
        i--;
      } else {
        j--;
      }
    }

    return lcs;
  }

  /**
   * Merge consecutive segments of the same type
   */
  private mergeSegments(segments: WordDiffSegment[]): WordDiffSegment[] {
    if (segments.length === 0) return segments;

    const merged: WordDiffSegment[] = [];
    let current = {...segments[0]};

    for (let i = 1; i < segments.length; i++) {
      if (segments[i].type === current.type) {
        current.text += segments[i].text;
      } else {
        merged.push(current);
        current = {...segments[i]};
      }
    }
    merged.push(current);

    return merged;
  }

  /**
   * Check if a diff item has string values suitable for word diff
   */
  isStringDiff(diff: DiffItem): boolean {
    if (diff.type === 'modified') {
      return typeof diff.oldValue === 'string' && typeof diff.newValue === 'string';
    }
    return false;
  }

  /**
   * Get word diff for a modified string item
   */
  getWordDiff(diff: DiffItem): WordDiffSegment[] {
    if (diff.type === 'modified' && typeof diff.oldValue === 'string' && typeof diff.newValue === 'string') {
      return this.computeWordDiff(diff.oldValue, diff.newValue);
    }
    return [];
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
   * Open the adventure state modal (Lore + Characters)
   */
  openAdventureStateModal(): void {
    this.showAdventureStateModal = true;
  }

  /**
   * Close the adventure state modal
   */
  closeAdventureStateModal(): void {
    this.showAdventureStateModal = false;
  }

  /**
   * Open the scene edit modal
   */
  openSceneEditModal(): void {
    this.showSceneEditModal = true;
  }

  /**
   * Close the scene edit modal
   */
  closeSceneEditModal(): void {
    this.showSceneEditModal = false;
  }

  /**
   * Handle scene saved event from edit modal
   */
  onSceneSaved(scene: GameScene): void {
    this.currentScene = scene;
    this.closeSceneEditModal();
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

  /**
   * Enrich the scene with tracker and additional content
   */
  private enrichSceneData(sceneId: string): void {
    if (!this.adventureId) return;

    this.isEnriching = true;
    this.enrichmentFailed = false;

    // Fetch previous scene tracker first for stale display during enrichment
    this.fetchPreviousSceneTracker();

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

          // Update diff availability now that current scene has tracker
          this.hasDiffAvailable = this.previousTrackerForDiff !== null;

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
   * Fetch the previous scene's tracker for diff comparison and stale display
   */
  private fetchPreviousSceneTracker(): void {
    if (!this.adventureId || !this.currentScene?.previousScene) {
      this.previousTrackerForDiff = null;
      this.hasDiffAvailable = false;
      return;
    }

    this.adventureService.getScene(this.adventureId, this.currentScene.previousScene)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (previousScene) => {
          if (previousScene?.tracker) {
            this.previousTrackerForDiff = previousScene.tracker;
            // Only mark diff as available if current scene also has tracker
            this.hasDiffAvailable = this.currentScene?.tracker !== null;
          } else {
            this.previousTrackerForDiff = null;
            this.hasDiffAvailable = false;
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error fetching previous scene for diff:', err);
          this.previousTrackerForDiff = null;
          this.hasDiffAvailable = false;
        }
      });
  }

  /**
   * Load a specific scene by ID
   */
  private loadScene(sceneId: string): void {
    if (!this.adventureId) return;

    this.isLoading = true;
    this.enrichmentData = null;
    this.enrichmentFailed = false;
    // Reset diff state while loading
    this.showTrackerDiff = false;

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
          } else {
            // Tracker exists, fetch previous scene for diff comparison
            this.fetchPreviousSceneTracker();
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
   * Reset to protagonist tab when scene changes
   */
  private resetCharacterTab(): void {
    this.activeCharacterTab = 'protagonist';
    this.activeNestedTab = 'state';
  }

  /**
   * Load emulation box visibility from localStorage
   */
  private loadEmulationVisibility(): void {
    const stored = localStorage.getItem(this.EMULATION_VISIBLE_KEY);
    this.showEmulationBox = stored === 'true';
  }

  /**
   * Load RAG chat box visibility from localStorage
   */
  private loadRagChatVisibility(): void {
    const stored = localStorage.getItem(this.RAG_CHAT_VISIBLE_KEY);
    this.showRagChatBox = stored === 'true';
  }
}

/**
 * Represents a single diff item between two tracker states
 */
export interface DiffItem {
  type: 'added' | 'removed' | 'modified';
  path: string;
  oldValue?: any;
  newValue?: any;
}

/**
 * Represents a segment in a word-level diff
 */
export interface WordDiffSegment {
  type: 'same' | 'added' | 'removed';
  text: string;
}
