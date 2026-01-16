import {Component, Input, OnChanges, SimpleChanges} from '@angular/core';
import {CharacterService} from '../../services/character.service';
import {ToastService} from '../../../../core/services/toast.service';
import {CharacterDetail, CharacterImportance, CharacterListItem} from '../../models/character.model';

@Component({
  selector: 'app-characters-tab',
  standalone: false,
  templateUrl: './characters-tab.component.html',
  styleUrl: './characters-tab.component.css'
})
export class CharactersTabComponent implements OnChanges {
  @Input() adventureId: string | null = null;
  @Input() isActive = false;

  isLoading = false;
  characters: CharacterListItem[] = [];
  selectedCharacterId: string | null = null;
  selectedCharacter: CharacterDetail | null = null;
  isLoadingDetail = false;

  private hasLoaded = false;

  constructor(
    private characterService: CharacterService,
    private toastService: ToastService
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Load characters when becoming active for the first time
    if (changes['isActive'] && this.isActive && this.adventureId && !this.hasLoaded) {
      this.loadCharacters();
    }
  }

  selectCharacter(characterId: string): void {
    if (this.selectedCharacterId === characterId) return;

    this.selectedCharacterId = characterId;
    this.loadCharacterDetail(characterId);
  }

  getImportanceBadgeClass(character: CharacterListItem): string {
    switch (character.importance) {
      case 'arc_important':
        return 'importance-arc';
      case 'significant':
        return 'importance-significant';
      case 'background':
        return 'importance-background';
      default:
        return '';
    }
  }

  getImportanceLabel(importance: CharacterImportance): string {
    switch (importance) {
      case 'arc_important':
        return 'Arc';
      case 'significant':
        return 'Sig';
      case 'background':
        return 'BG';
      default:
        return importance;
    }
  }

  canEditImportance(character: CharacterDetail): boolean {
    return character.importance !== 'background';
  }

  onCharacterUpdated(): void {
    // Refresh the character list to reflect any changes
    if (this.adventureId) {
      this.characterService.getCharacterList(this.adventureId).subscribe({
        next: (characters) => {
          this.characters = characters;
        }
      });
    }

    // Refresh current character detail if selected
    if (this.selectedCharacterId && this.adventureId) {
      this.loadCharacterDetail(this.selectedCharacterId);
    }
  }

  openMainCharacterGraph(): void {
    if (this.adventureId) {
      window.open(`/visualization/${this.adventureId}_main_character/cognify_graph_visualization.html`, '_blank');
    }
  }

  openCharacterGraph(characterId: string): void {
    if (this.adventureId) {
      window.open(`/visualization/${this.adventureId}_${characterId}/cognify_graph_visualization.html`, '_blank');
    }
  }

  private loadCharacters(): void {
    if (!this.adventureId) return;

    this.isLoading = true;

    this.characterService.getCharacterList(this.adventureId).subscribe({
      next: (characters) => {
        this.characters = characters;
        this.isLoading = false;
        this.hasLoaded = true;

        // Auto-select first character if available
        if (characters.length > 0 && !this.selectedCharacterId) {
          this.selectCharacter(characters[0].characterId);
        }
      },
      error: (err) => {
        console.error('Error loading characters:', err);
        this.toastService.error('Failed to load characters.');
        this.isLoading = false;
      }
    });
  }

  private loadCharacterDetail(characterId: string): void {
    if (!this.adventureId) return;

    this.isLoadingDetail = true;
    this.selectedCharacter = null;

    this.characterService.getCharacterDetail(this.adventureId, characterId).subscribe({
      next: (character) => {
        this.selectedCharacter = character;
        this.isLoadingDetail = false;
      },
      error: (err) => {
        console.error('Error loading character detail:', err);
        this.toastService.error('Failed to load character details.');
        this.isLoadingDetail = false;
      }
    });
  }
}
