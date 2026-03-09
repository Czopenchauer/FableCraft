import {Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild} from '@angular/core';
import {
  CreateLorebookEntryDto,
  LorebookCategoryGroup,
  LorebookEntryResponseDto,
  UpdateLorebookEntryDto
} from '../../models/lorebook-entry.model';
import {LorebookEntryService} from '../../services/lorebook-entry.service';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-lore-management-modal',
  standalone: false,
  templateUrl: './lore-management-modal.component.html',
  styleUrl: './lore-management-modal.component.css'
})
export class LoreManagementModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Output() close = new EventEmitter<void>();
  @ViewChild('loreFileInput') loreFileInput!: ElementRef<HTMLInputElement>;

  isLoading = false;
  isSaving = false;
  worldSettings: string | null = null;
  entries: LorebookEntryResponseDto[] = [];
  categoryGroups: LorebookCategoryGroup[] = [];
  selectedEntry: LorebookEntryResponseDto | null = null;
  parsedJsonContent: any = null;
  jsonParseError = false;
  showWorldSettings = true;

  // Edit mode state
  isEditMode = false;
  isAddingEntry = false;
  editingEntryId: string | null = null;
  deleteConfirmEntryId: string | null = null;
  importMessage = '';

  // Form fields for add/edit
  formTitle = '';
  formContent = '';
  formCategory = 'Lore';
  formDescription = '';
  formPriority = 0;
  formContentType: 'json' | 'txt' = 'txt';

  // Available categories
  availableCategories = ['Lore', 'Character', 'Location', 'Item', 'Event', 'Faction', 'Custom'];

  constructor(
    private lorebookEntryService: LorebookEntryService,
    private toastService: ToastService
  ) {
  }

  get isWorldSettingsSelected(): boolean {
    return this.selectedEntry === null && this.worldSettings !== null;
  }

  get isEditingEntry(): boolean {
    return this.editingEntryId !== null;
  }

  get isFormValid(): boolean {
    return this.formTitle.trim() !== '' && this.formContent.trim() !== '';
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
      if (this.isOpen && this.adventureId) {
        this.loadEntries();
      }
      if (!this.isOpen) {
        this.resetState();
      }
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  toggleCategory(category: string): void {
    const group = this.categoryGroups.find(g => g.category === category);
    if (group) {
      group.isExpanded = !group.isExpanded;
    }
  }

  selectEntry(entry: LorebookEntryResponseDto): void {
    if (this.deleteConfirmEntryId) return; // Don't change selection during delete confirmation
    this.selectedEntry = entry;
    this.parseContent(entry);
    this.cancelEditing();
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
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
    this.cancelEditing();
  }

  // Edit mode methods
  toggleEditMode(): void {
    this.isEditMode = !this.isEditMode;
    if (!this.isEditMode) {
      this.cancelEditing();
      this.cancelAdding();
    }
  }

  // Add entry methods
  startAddingEntry(): void {
    this.isAddingEntry = true;
    this.editingEntryId = null;
    this.selectedEntry = null;
    this.resetForm();
  }

  cancelAdding(): void {
    this.isAddingEntry = false;
    this.resetForm();
  }

  saveNewEntry(): void {
    if (!this.adventureId || !this.isFormValid) return;

    this.isSaving = true;
    const dto: CreateLorebookEntryDto = {
      title: this.formTitle.trim(),
      content: this.formContent.trim(),
      category: this.formCategory,
      description: this.formDescription.trim() || undefined,
      priority: this.formPriority,
      contentType: this.formContentType
    };

    this.lorebookEntryService.createEntry(this.adventureId, dto).subscribe({
      next: (newEntry) => {
        this.entries.push(newEntry);
        this.groupEntriesByCategory();
        this.selectedEntry = newEntry;
        this.parseContent(newEntry);
        this.isAddingEntry = false;
        this.resetForm();
        this.isSaving = false;
        this.toastService.success('Lore entry created successfully');
      },
      error: (err) => {
        console.error('Error creating entry:', err);
        this.toastService.error('Failed to create lore entry');
        this.isSaving = false;
      }
    });
  }

  // Edit entry methods
  startEditingEntry(entry: LorebookEntryResponseDto, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.editingEntryId = entry.id;
    this.isAddingEntry = false;
    this.selectedEntry = entry;
    this.populateFormFromEntry(entry);
  }

  cancelEditing(): void {
    this.editingEntryId = null;
    this.resetForm();
  }

  saveEditedEntry(): void {
    if (!this.editingEntryId || !this.isFormValid) return;

    this.isSaving = true;
    const dto: UpdateLorebookEntryDto = {
      title: this.formTitle.trim(),
      content: this.formContent.trim(),
      category: this.formCategory,
      description: this.formDescription.trim() || undefined,
      priority: this.formPriority,
      contentType: this.formContentType
    };

    this.lorebookEntryService.updateEntry(this.editingEntryId, dto).subscribe({
      next: (updatedEntry) => {
        const index = this.entries.findIndex(e => e.id === this.editingEntryId);
        if (index >= 0) {
          this.entries[index] = updatedEntry;
        }
        this.groupEntriesByCategory();
        this.selectedEntry = updatedEntry;
        this.parseContent(updatedEntry);
        this.editingEntryId = null;
        this.resetForm();
        this.isSaving = false;
        this.toastService.success('Lore entry updated successfully');
      },
      error: (err) => {
        console.error('Error updating entry:', err);
        this.toastService.error('Failed to update lore entry');
        this.isSaving = false;
      }
    });
  }

  // Delete entry methods
  confirmDelete(entry: LorebookEntryResponseDto, event: Event): void {
    event.stopPropagation();
    this.deleteConfirmEntryId = entry.id;
  }

  cancelDelete(): void {
    this.deleteConfirmEntryId = null;
  }

  deleteEntry(): void {
    if (!this.deleteConfirmEntryId) return;

    this.isSaving = true;
    const entryId = this.deleteConfirmEntryId;

    this.lorebookEntryService.deleteEntry(entryId).subscribe({
      next: () => {
        this.entries = this.entries.filter(e => e.id !== entryId);
        this.groupEntriesByCategory();
        if (this.selectedEntry?.id === entryId) {
          this.selectedEntry = null;
        }
        this.deleteConfirmEntryId = null;
        this.isSaving = false;
        this.toastService.success('Lore entry deleted successfully');
      },
      error: (err) => {
        console.error('Error deleting entry:', err);
        this.toastService.error('Failed to delete lore entry');
        this.deleteConfirmEntryId = null;
        this.isSaving = false;
      }
    });
  }

  // JSON Import methods
  triggerLoreImport(): void {
    if (this.loreFileInput?.nativeElement) {
      this.loreFileInput.nativeElement.click();
    }
  }

  onLoreFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0 || !this.adventureId) {
      return;
    }

    const file = input.files[0];
    const reader = new FileReader();

    reader.onload = () => {
      try {
        const content = reader.result as string;
        const entries = JSON.parse(content);

        if (!Array.isArray(entries)) {
          this.importMessage = 'Invalid format: Expected an array of lore entries';
          this.clearImportMessage();
          return;
        }

        const validEntries: CreateLorebookEntryDto[] = [];
        for (const entry of entries) {
          if (this.isValidLoreEntry(entry)) {
            validEntries.push({
              title: entry.title,
              content: entry.content,
              category: entry.category || 'Lore',
              description: entry.description,
              priority: entry.priority || 0,
              contentType: entry.contentType || 'txt'
            });
          }
        }

        if (validEntries.length === 0) {
          this.importMessage = 'No valid entries found in the file';
          this.clearImportMessage();
          return;
        }

        this.isSaving = true;
        this.lorebookEntryService.bulkCreateEntries(this.adventureId!, validEntries).subscribe({
          next: (newEntries) => {
            this.entries.push(...newEntries);
            this.groupEntriesByCategory();
            this.importMessage = `Successfully imported ${newEntries.length} lore entries`;
            this.clearImportMessage();
            this.isSaving = false;
            this.toastService.success(`Imported ${newEntries.length} lore entries`);
          },
          error: (err) => {
            console.error('Error importing entries:', err);
            this.importMessage = 'Failed to import entries';
            this.clearImportMessage();
            this.isSaving = false;
            this.toastService.error('Failed to import lore entries');
          }
        });
      } catch (e) {
        this.importMessage = 'Failed to parse JSON file. Please ensure it is valid JSON.';
        this.clearImportMessage();
      }
    };

    reader.onerror = () => {
      this.importMessage = 'Failed to read file';
      this.clearImportMessage();
    };

    reader.readAsText(file);
    // Reset input so same file can be re-selected
    input.value = '';
  }

  private isValidLoreEntry(entry: any): boolean {
    return entry &&
      typeof entry.title === 'string' && entry.title.trim() !== '' &&
      typeof entry.content === 'string' && entry.content.trim() !== '';
  }

  private clearImportMessage(): void {
    setTimeout(() => this.importMessage = '', 5000);
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

  private resetForm(): void {
    this.formTitle = '';
    this.formContent = '';
    this.formCategory = 'Lore';
    this.formDescription = '';
    this.formPriority = 0;
    this.formContentType = 'txt';
  }

  private populateFormFromEntry(entry: LorebookEntryResponseDto): void {
    this.formTitle = entry.title || '';
    this.formContent = entry.content;
    this.formCategory = entry.category;
    this.formDescription = entry.description || '';
    this.formPriority = entry.priority;
    this.formContentType = entry.contentType;
  }

  private resetState(): void {
    this.worldSettings = null;
    this.entries = [];
    this.categoryGroups = [];
    this.selectedEntry = null;
    this.parsedJsonContent = null;
    this.jsonParseError = false;
    this.showWorldSettings = true;
    this.isEditMode = false;
    this.isAddingEntry = false;
    this.editingEntryId = null;
    this.deleteConfirmEntryId = null;
    this.importMessage = '';
    this.resetForm();
  }
}
