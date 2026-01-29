import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';
import {Subject} from 'rxjs';
import {finalize, takeUntil} from 'rxjs/operators';
import {GameScene} from '../../models/adventure.model';
import {AdventureService} from '../../services/adventure.service';
import {ToastService} from '../../../../core/services/toast.service';

type EditTab = 'narrative' | 'sceneTracker' | 'mainCharacter' | 'characters';

interface CharacterEditState {
  characterId: string;
  characterStateId: string;
  name: string;
  trackerJson: string;
  hasError: boolean;
  errorMessage: string;
}

@Component({
  selector: 'app-scene-edit-modal',
  standalone: false,
  templateUrl: './scene-edit-modal.component.html',
  styleUrl: './scene-edit-modal.component.css'
})
export class SceneEditModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() scene: GameScene | null = null;
  @Input() adventureId: string | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<GameScene>();

  activeTab: EditTab = 'narrative';
  isSaving = false;

  // Narrative editing
  narrativeText = '';

  // Scene tracker editing
  sceneTrackerJson = '';
  sceneTrackerError = false;
  sceneTrackerErrorMessage = '';

  // Main character editing
  mainCharacterTrackerJson = '';
  mainCharacterDescription = '';
  mainCharacterError = false;
  mainCharacterErrorMessage = '';

  // Character editing
  characters: CharacterEditState[] = [];
  selectedCharacterIndex = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private adventureService: AdventureService,
    private toastService: ToastService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
      if (this.isOpen && this.scene) {
        this.initializeFormData();
      }
    }
    if (changes['scene'] && this.isOpen && this.scene) {
      this.initializeFormData();
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeFormData(): void {
    if (!this.scene) return;

    // Initialize narrative
    this.narrativeText = this.scene.text;

    // Initialize scene tracker
    if (this.scene.tracker?.scene) {
      this.sceneTrackerJson = JSON.stringify(this.scene.tracker.scene, null, 2);
    } else {
      this.sceneTrackerJson = '{}';
    }
    this.sceneTrackerError = false;
    this.sceneTrackerErrorMessage = '';

    // Initialize main character tracker
    if (this.scene.tracker?.mainCharacter) {
      this.mainCharacterTrackerJson = JSON.stringify(this.scene.tracker.mainCharacter.tracker, null, 2);
      this.mainCharacterDescription = this.scene.tracker.mainCharacter.description || '';
    } else {
      this.mainCharacterTrackerJson = '{}';
      this.mainCharacterDescription = '';
    }
    this.mainCharacterError = false;
    this.mainCharacterErrorMessage = '';

    // Initialize characters
    this.characters = (this.scene.tracker?.characters || []).map(char => ({
      characterId: char.characterId,
      characterStateId: char.characterId, // Using characterId as the state ID for the API
      name: char.name,
      trackerJson: JSON.stringify(char.tracker, null, 2),
      hasError: false,
      errorMessage: ''
    }));
    this.selectedCharacterIndex = 0;
  }

  setActiveTab(tab: EditTab): void {
    this.activeTab = tab;
  }

  isActiveTab(tab: EditTab): boolean {
    return this.activeTab === tab;
  }

  // Scene tracker validation
  onSceneTrackerChange(): void {
    try {
      JSON.parse(this.sceneTrackerJson);
      this.sceneTrackerError = false;
      this.sceneTrackerErrorMessage = '';
    } catch (e: any) {
      this.sceneTrackerError = true;
      this.sceneTrackerErrorMessage = e.message || 'Invalid JSON';
    }
  }

  // Main character validation
  onMainCharacterTrackerChange(): void {
    try {
      JSON.parse(this.mainCharacterTrackerJson);
      this.mainCharacterError = false;
      this.mainCharacterErrorMessage = '';
    } catch (e: any) {
      this.mainCharacterError = true;
      this.mainCharacterErrorMessage = e.message || 'Invalid JSON';
    }
  }

  // Character tracker validation
  onCharacterTrackerChange(index: number): void {
    const char = this.characters[index];
    if (!char) return;

    try {
      JSON.parse(char.trackerJson);
      char.hasError = false;
      char.errorMessage = '';
    } catch (e: any) {
      char.hasError = true;
      char.errorMessage = e.message || 'Invalid JSON';
    }
  }

  selectCharacter(index: number): void {
    this.selectedCharacterIndex = index;
  }

  hasAnyError(): boolean {
    if (this.activeTab === 'sceneTracker' && this.sceneTrackerError) return true;
    if (this.activeTab === 'mainCharacter' && this.mainCharacterError) return true;
    if (this.activeTab === 'characters') {
      const char = this.characters[this.selectedCharacterIndex];
      if (char?.hasError) return true;
    }
    return false;
  }

  onSave(): void {
    if (this.hasAnyError() || !this.adventureId || !this.scene) return;

    this.isSaving = true;

    switch (this.activeTab) {
      case 'narrative':
        this.saveNarrative();
        break;
      case 'sceneTracker':
        this.saveSceneTracker();
        break;
      case 'mainCharacter':
        this.saveMainCharacter();
        break;
      case 'characters':
        this.saveCharacter();
        break;
    }
  }

  private saveNarrative(): void {
    this.adventureService.updateSceneNarrative(this.adventureId!, this.scene!.sceneId, this.narrativeText)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isSaving = false)
      )
      .subscribe({
        next: (updatedScene) => {
          this.toastService.success('Narrative updated successfully');
          this.saved.emit(updatedScene);
        },
        error: (err) => {
          console.error('Error updating narrative:', err);
          this.toastService.error('Failed to update narrative');
        }
      });
  }

  private saveSceneTracker(): void {
    try {
      const tracker = JSON.parse(this.sceneTrackerJson);
      this.adventureService.updateSceneTracker(this.adventureId!, this.scene!.sceneId, tracker)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.isSaving = false)
        )
        .subscribe({
          next: (updatedScene) => {
            this.toastService.success('Scene tracker updated successfully');
            this.saved.emit(updatedScene);
          },
          error: (err) => {
            console.error('Error updating scene tracker:', err);
            this.toastService.error('Failed to update scene tracker');
          }
        });
    } catch (e) {
      this.isSaving = false;
      this.sceneTrackerError = true;
      this.sceneTrackerErrorMessage = 'Invalid JSON format';
    }
  }

  private saveMainCharacter(): void {
    try {
      const tracker = JSON.parse(this.mainCharacterTrackerJson);
      this.adventureService.updateMainCharacterTracker(
        this.adventureId!,
        this.scene!.sceneId,
        tracker,
        this.mainCharacterDescription
      )
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.isSaving = false)
        )
        .subscribe({
          next: (updatedScene) => {
            this.toastService.success('Main character updated successfully');
            this.saved.emit(updatedScene);
          },
          error: (err) => {
            console.error('Error updating main character:', err);
            this.toastService.error('Failed to update main character');
          }
        });
    } catch (e) {
      this.isSaving = false;
      this.mainCharacterError = true;
      this.mainCharacterErrorMessage = 'Invalid JSON format';
    }
  }

  private saveCharacter(): void {
    const char = this.characters[this.selectedCharacterIndex];
    if (!char) {
      this.isSaving = false;
      return;
    }

    try {
      const tracker = JSON.parse(char.trackerJson);
      this.adventureService.updateCharacterState(
        this.adventureId!,
        this.scene!.sceneId,
        char.characterStateId,
        tracker
      )
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.isSaving = false)
        )
        .subscribe({
          next: (updatedScene) => {
            this.toastService.success(`${char.name}'s tracker updated successfully`);
            this.saved.emit(updatedScene);
          },
          error: (err) => {
            console.error('Error updating character:', err);
            this.toastService.error(`Failed to update ${char.name}'s tracker`);
          }
        });
    } catch (e) {
      this.isSaving = false;
      char.hasError = true;
      char.errorMessage = 'Invalid JSON format';
    }
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
