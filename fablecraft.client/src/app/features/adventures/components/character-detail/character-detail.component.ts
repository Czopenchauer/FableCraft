import {Component, EventEmitter, Input, Output} from '@angular/core';
import {CharacterService} from '../../services/character.service';
import {ToastService} from '../../../../core/services/toast.service';
import {
  CharacterDetail,
  CharacterImportance,
  CharacterMemory,
  CharacterRelationship
} from '../../models/character.model';

interface EditModalState {
  isOpen: boolean;
  title: string;
  data: any;
  type: 'profile' | 'tracker' | 'memory' | 'relationship' | null;
  itemId?: string;
}

@Component({
  selector: 'app-character-detail',
  standalone: false,
  templateUrl: './character-detail.component.html',
  styleUrl: './character-detail.component.css'
})
export class CharacterDetailComponent {
  @Input() character!: CharacterDetail;
  @Input() adventureId: string | null = null;
  @Input() canEditImportance = false;
  @Output() characterUpdated = new EventEmitter<void>();

  // Importance edit state
  pendingImportance: CharacterImportance | null = null;
  isSavingImportance = false;

  // JSON editor modal state
  editModal: EditModalState = {
    isOpen: false,
    title: '',
    data: null,
    type: null
  };

  // Pagination state
  isLoadingMoreMemories = false;
  isLoadingMoreRewrites = false;

  // Saving state
  isSaving = false;

  constructor(
    private characterService: CharacterService,
    private toastService: ToastService
  ) {
  }

  get hasImportanceChanges(): boolean {
    return this.pendingImportance !== null && this.pendingImportance !== this.character.importance;
  }

  get hasMoreMemories(): boolean {
    return this.character.characterMemories.length < this.character.totalMemoriesCount;
  }

  get hasMoreRewrites(): boolean {
    return this.character.sceneRewrites.length < this.character.totalSceneRewritesCount;
  }

  // Importance handling
  onImportanceChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.pendingImportance = select.value as CharacterImportance;
  }

  saveImportance(): void {
    if (!this.adventureId || !this.pendingImportance) return;

    this.isSavingImportance = true;
    this.characterService.updateCharacterImportance(
      this.adventureId,
      this.character.characterId,
      this.pendingImportance
    ).subscribe({
      next: () => {
        this.character.importance = this.pendingImportance!;
        this.pendingImportance = null;
        this.isSavingImportance = false;
        this.toastService.success('Importance updated');
        this.characterUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating importance:', err);
        this.toastService.error('Failed to update importance');
        this.isSavingImportance = false;
      }
    });
  }

  discardImportance(): void {
    this.pendingImportance = null;
  }

  // Edit Profile (description + character stats)
  editProfile(): void {
    this.editModal = {
      isOpen: true,
      title: 'Edit Character State',
      data: {
        description: this.character.description,
        characterStats: this.character.characterState
      },
      type: 'profile'
    };
  }

  // Edit Tracker
  editTracker(): void {
    this.editModal = {
      isOpen: true,
      title: 'Edit Tracker',
      data: this.character.characterTracker,
      type: 'tracker'
    };
  }

  // Edit Relationship
  editRelationship(relationship: CharacterRelationship): void {
    this.editModal = {
      isOpen: true,
      title: `Edit Relationship with ${relationship.targetCharacterName}`,
      data: {
        dynamic: relationship.dynamic,
        data: relationship.data
      },
      type: 'relationship',
      itemId: relationship.id
    };
  }

  // Edit Memory
  editMemory(memory: CharacterMemory): void {
    this.editModal = {
      isOpen: true,
      title: 'Edit Memory',
      data: {
        summary: memory.memoryContent,
        salience: memory.salience,
        data: memory.data
      },
      type: 'memory',
      itemId: memory.id
    };
  }

  // Handle save from JSON editor modal
  onModalSave(data: any): void {
    if (!this.adventureId) return;

    this.isSaving = true;

    switch (this.editModal.type) {
      case 'profile':
        this.saveProfile(data);
        break;
      case 'tracker':
        this.saveTracker(data);
        break;
      case 'memory':
        this.saveMemory(data);
        break;
      case 'relationship':
        this.saveRelationship(data);
        break;
    }
  }

  onModalCancel(): void {
    this.closeModal();
  }

  // Load more memories
  loadMoreMemories(): void {
    if (!this.adventureId || this.isLoadingMoreMemories) return;

    this.isLoadingMoreMemories = true;
    const offset = this.character.characterMemories.length;

    this.characterService.getCharacterMemories(
      this.adventureId,
      this.character.characterId,
      offset,
      20
    ).subscribe({
      next: (response) => {
        this.character.characterMemories = [
          ...this.character.characterMemories,
          ...response.items
        ];
        this.isLoadingMoreMemories = false;
      },
      error: (err) => {
        console.error('Error loading more memories:', err);
        this.toastService.error('Failed to load more memories');
        this.isLoadingMoreMemories = false;
      }
    });
  }

  // Load more scene rewrites
  loadMoreRewrites(): void {
    if (!this.adventureId || this.isLoadingMoreRewrites) return;

    this.isLoadingMoreRewrites = true;
    const offset = this.character.sceneRewrites.length;

    this.characterService.getCharacterSceneRewrites(
      this.adventureId,
      this.character.characterId,
      offset,
      20
    ).subscribe({
      next: (response) => {
        this.character.sceneRewrites = [
          ...this.character.sceneRewrites,
          ...response.items
        ];
        this.isLoadingMoreRewrites = false;
      },
      error: (err) => {
        console.error('Error loading more rewrites:', err);
        this.toastService.error('Failed to load more scene rewrites');
        this.isLoadingMoreRewrites = false;
      }
    });
  }

  getImportanceLabel(importance: CharacterImportance): string {
    switch (importance) {
      case 'arc_important':
        return 'Arc Important';
      case 'significant':
        return 'Significant';
      case 'background':
        return 'Background';
      default:
        return importance;
    }
  }

  private saveProfile(data: { description: string; characterStats: any }): void {
    this.characterService.updateCharacterProfile(
      this.adventureId!,
      this.character.characterId,
      data
    ).subscribe({
      next: () => {
        this.character.description = data.description;
        this.character.characterState = data.characterStats;
        this.closeModal();
        this.toastService.success('Character state updated');
        this.characterUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating character state:', err);
        this.toastService.error('Failed to update character state');
        this.isSaving = false;
      }
    });
  }

  private saveTracker(tracker: any): void {
    this.characterService.updateCharacterTracker(
      this.adventureId!,
      this.character.characterId,
      tracker
    ).subscribe({
      next: () => {
        this.character.characterTracker = tracker;
        this.closeModal();
        this.toastService.success('Tracker updated');
        this.characterUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating tracker:', err);
        this.toastService.error('Failed to update tracker');
        this.isSaving = false;
      }
    });
  }

  private saveMemory(data: any): void {
    const memoryId = this.editModal.itemId!;
    this.characterService.updateCharacterMemory(
      this.adventureId!,
      this.character.characterId,
      memoryId,
      data
    ).subscribe({
      next: () => {
        // Update the memory in local array
        const memory = this.character.characterMemories.find(m => m.id === memoryId);
        if (memory) {
          memory.memoryContent = data.summary;
          memory.salience = data.salience;
          memory.data = data.data;
        }
        this.closeModal();
        this.toastService.success('Memory updated');
        this.characterUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating memory:', err);
        this.toastService.error('Failed to update memory');
        this.isSaving = false;
      }
    });
  }

  private saveRelationship(data: any): void {
    const relationshipId = this.editModal.itemId!;
    this.characterService.updateCharacterRelationship(
      this.adventureId!,
      this.character.characterId,
      relationshipId,
      data
    ).subscribe({
      next: () => {
        // Update the relationship in local array
        const relationship = this.character.relationships.find(r => r.id === relationshipId);
        if (relationship) {
          relationship.dynamic = data.dynamic;
          relationship.data = data.data;
        }
        this.closeModal();
        this.toastService.success('Relationship updated');
        this.characterUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating relationship:', err);
        this.toastService.error('Failed to update relationship');
        this.isSaving = false;
      }
    });
  }

  private closeModal(): void {
    this.editModal = {
      isOpen: false,
      title: '',
      data: null,
      type: null
    };
    this.isSaving = false;
  }
}
