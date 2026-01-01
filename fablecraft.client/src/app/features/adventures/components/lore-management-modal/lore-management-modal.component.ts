import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {LorebookEntryResponseDto, LorebookCategoryGroup} from '../../models/lorebook-entry.model';
import {LorebookEntryService} from '../../services/lorebook-entry.service';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-lore-management-modal',
  standalone: false,
  templateUrl: './lore-management-modal.component.html',
  styleUrl: './lore-management-modal.component.css'
})
export class LoreManagementModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Output() close = new EventEmitter<void>();

  isLoading = false;
  entries: LorebookEntryResponseDto[] = [];
  categoryGroups: LorebookCategoryGroup[] = [];
  selectedEntry: LorebookEntryResponseDto | null = null;
  parsedJsonContent: any = null;
  jsonParseError = false;

  constructor(
    private lorebookEntryService: LorebookEntryService,
    private toastService: ToastService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.adventureId) {
      this.loadEntries();
    }
    if (changes['isOpen'] && !this.isOpen) {
      this.resetState();
    }
  }

  private loadEntries(): void {
    if (!this.adventureId) return;

    this.isLoading = true;

    this.lorebookEntryService.getEntriesByAdventure(this.adventureId).subscribe({
      next: (entries) => {
        this.entries = entries;
        this.groupEntriesByCategory();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading lorebook entries:', err);
        this.toastService.error('Failed to load lore entries.');
        this.isLoading = false;
        this.onClose();
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

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  private resetState(): void {
    this.entries = [];
    this.categoryGroups = [];
    this.selectedEntry = null;
    this.parsedJsonContent = null;
    this.jsonParseError = false;
  }

  getDisplayTitle(entry: LorebookEntryResponseDto): string {
    return entry.title || entry.description || 'Untitled';
  }
}
