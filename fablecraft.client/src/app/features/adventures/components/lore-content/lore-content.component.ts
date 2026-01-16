import {Component, Input, OnChanges, SimpleChanges} from '@angular/core';
import {LorebookCategoryGroup, LorebookEntryResponseDto} from '../../models/lorebook-entry.model';
import {LorebookEntryService} from '../../services/lorebook-entry.service';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-lore-content',
  standalone: false,
  templateUrl: './lore-content.component.html',
  styleUrl: './lore-content.component.css'
})
export class LoreContentComponent implements OnChanges {
  @Input() adventureId: string | null = null;
  @Input() isActive = false;

  isLoading = false;
  worldSettings: string | null = null;
  entries: LorebookEntryResponseDto[] = [];
  categoryGroups: LorebookCategoryGroup[] = [];
  selectedEntry: LorebookEntryResponseDto | null = null;
  parsedJsonContent: any = null;
  jsonParseError = false;
  showWorldSettings = true;

  private hasLoaded = false;

  constructor(
    private lorebookEntryService: LorebookEntryService,
    private toastService: ToastService
  ) {
  }

  get isWorldSettingsSelected(): boolean {
    return this.selectedEntry === null && this.worldSettings !== null;
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Load entries when becoming active for the first time
    if (changes['isActive'] && this.isActive && this.adventureId && !this.hasLoaded) {
      this.loadEntries();
    }
  }

  toggleCategory(category: string): void {
    const group = this.categoryGroups.find(g => g.category === category);
    if (group) {
      group.isExpanded = !group.isExpanded;
    }
  }

  selectEntry(entry: LorebookEntryResponseDto): void {
    this.selectedEntry = entry;
    this.parseContent(entry);
  }

  getDisplayTitle(entry: LorebookEntryResponseDto): string {
    return entry.title || entry.description || 'Untitled';
  }

  toggleWorldSettings(): void {
    this.showWorldSettings = !this.showWorldSettings;
  }

  selectWorldSettings(): void {
    this.selectedEntry = null;
    this.parsedJsonContent = null;
    this.jsonParseError = false;
  }

  openWorldGraph(): void {
    console.log('openWorldGraph called, adventureId:', this.adventureId);
    if (this.adventureId) {
      const url = `/visualization/${this.adventureId}_world/cognify_graph_visualization.html`;
      console.log('Opening URL:', url);
      window.open(url, '_blank');
    }
  }

  private loadEntries(): void {
    if (!this.adventureId) return;

    this.isLoading = true;

    this.lorebookEntryService.getAdventureLore(this.adventureId).subscribe({
      next: (response) => {
        this.worldSettings = response.worldSettings;
        this.entries = response.entries;
        this.groupEntriesByCategory();
        this.isLoading = false;
        this.hasLoaded = true;
      },
      error: (err) => {
        console.error('Error loading lorebook entries:', err);
        this.toastService.error('Failed to load lore entries.');
        this.isLoading = false;
      }
    });
  }

  private groupEntriesByCategory(): void {
    const groupMap = new Map<string, LorebookEntryResponseDto[]>();

    for (const entry of this.entries) {
      const category = entry.category || 'Uncategorized';
      if (!groupMap.has(category)) {
        groupMap.set(category, []);
      }
      groupMap.get(category)!.push(entry);
    }

    this.categoryGroups = Array.from(groupMap.entries())
      .map(([category, entries]) => ({
        category,
        entries,
        isExpanded: true
      }))
      .sort((a, b) => a.category.localeCompare(b.category));
  }

  private parseContent(entry: LorebookEntryResponseDto): void {
    this.jsonParseError = false;
    this.parsedJsonContent = null;

    if (entry.contentType === 'json') {
      try {
        this.parsedJsonContent = JSON.parse(entry.content);
      } catch (e) {
        console.error('Failed to parse JSON content:', e);
        this.jsonParseError = true;
      }
    }
  }
}
