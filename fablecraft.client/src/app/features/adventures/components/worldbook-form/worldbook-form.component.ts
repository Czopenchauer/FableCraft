import {Component, HostListener, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {FormArray, FormBuilder, FormGroup, Validators} from '@angular/forms';
import {WorldbookService} from '../../services/worldbook.service';
import {
  CreateLorebookDto,
  IndexingStatus,
  LorebookChangeStatus,
  LorebookResponseDto,
  UpdateLorebookDto,
  WorldbookDto,
  WorldbookResponseDto,
  WorldbookUpdateDto
} from '../../models/worldbook.model';
import {GraphRagSettingsService} from '../../../settings/services/graph-rag-settings.service';
import {GraphRagSettingsSummaryDto} from '../../../settings/models/graph-rag-settings.model';

@Component({
  selector: 'app-worldbook-form',
  standalone: false,
  templateUrl: './worldbook-form.component.html',
  styleUrl: './worldbook-form.component.css'
})
export class WorldbookFormComponent implements OnInit, OnDestroy {
  worldbookForm: FormGroup;
  isEditMode = false;
  worldbookId: string | null = null;
  loading = false;
  saving = false;
  error: string | null = null;

  // Lorebook panel management
  expandedPanels: Set<number> = new Set<number>();

  // JSON import
  importError: string | null = null;

  // GraphRAG Settings options
  graphRagSettingsOptions: GraphRagSettingsSummaryDto[] = [];
  graphRagDropdownOpen = false;

  // Worldbook state
  worldbook: WorldbookResponseDto | null = null;
  indexingStatus: IndexingStatus = 'NotIndexed';
  hasPendingChanges = false;
  reverting = false;

  // Polling for indexing status
  private pollingInterval: ReturnType<typeof setInterval> | null = null;
  private readonly POLLING_INTERVAL_MS = 3000;

  // Lorebook change tracking
  lorebookChangeStatuses: Map<string, LorebookChangeStatus> = new Map();
  deletedLorebookIds: Set<string> = new Set();

  // Pending deletes during current edit session (not yet saved to server)
  pendingDeletes: LorebookResponseDto[] = [];

  // Filtering and sorting
  searchTerm = '';
  sortOption: 'title-asc' | 'title-desc' | 'category-asc' | 'category-desc' | 'status' = 'title-asc';
  statusFilter: 'all' | 'Added' | 'Modified' | 'Deleted' | 'None' = 'all';
  sortDropdownOpen = false;

  // Filtered view
  filteredIndices: number[] = [];

  // Success message
  successMessage: string | null = null;

  // Selected lorebook for detail view
  selectedLorebookIndex: number | null = null;

  // Content type dropdown
  contentTypeDropdownOpen = false;

  constructor(
    private fb: FormBuilder,
    private worldbookService: WorldbookService,
    private graphRagSettingsService: GraphRagSettingsService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.worldbookForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      graphRagSettingsId: [null],
      lorebooks: this.fb.array([])
    });
  }

  get lorebooks(): FormArray {
    return this.worldbookForm.get('lorebooks') as FormArray;
  }

  get nameControl() {
    return this.worldbookForm.get('name');
  }

  // Count getters for filter badges
  get addedCount(): number {
    return this.lorebooks.controls.filter((_, i) => this.getLorebookChangeStatus(i) === 'Added').length;
  }

  get modifiedCount(): number {
    return this.lorebooks.controls.filter((_, i) => this.getLorebookChangeStatus(i) === 'Modified').length;
  }

  get deletedCount(): number {
    return this.lorebooks.controls.filter((_, i) => this.isLorebookDeleted(i)).length;
  }

  get unchangedCount(): number {
    return this.lorebooks.controls.filter((_, i) =>
      this.getLorebookChangeStatus(i) === 'None' && !this.isLorebookDeleted(i)
    ).length;
  }

  // Check if editing is allowed (not Indexing or Failed)
  get canEdit(): boolean {
    return this.indexingStatus !== 'Indexing' && this.indexingStatus !== 'Failed';
  }

  // Check if reindex is available
  get canReindex(): boolean {
    return this.isEditMode &&
           this.indexingStatus !== 'Indexing' &&
           (this.indexingStatus === 'NotIndexed' ||
            this.indexingStatus === 'NeedsReindexing' ||
            this.indexingStatus === 'Failed');
  }

  ngOnInit(): void {
    this.worldbookId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.worldbookId;

    // Load GraphRAG settings options
    this.loadGraphRagSettings();

    if (this.isEditMode && this.worldbookId) {
      this.loadWorldbook(this.worldbookId);
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  /**
   * Close dropdowns when clicking outside
   */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.custom-dropdown')) {
      this.sortDropdownOpen = false;
      this.graphRagDropdownOpen = false;
      this.contentTypeDropdownOpen = false;
    }
  }

  /**
   * Toggle content type dropdown
   */
  toggleContentTypeDropdown(): void {
    this.contentTypeDropdownOpen = !this.contentTypeDropdownOpen;
    this.sortDropdownOpen = false;
    this.graphRagDropdownOpen = false;
  }

  /**
   * Get selected content type value
   */
  getSelectedContentType(): string {
    if (this.selectedLorebookIndex === null) return 'txt';
    return this.lorebooks.at(this.selectedLorebookIndex).get('contentType')?.value || 'txt';
  }

  /**
   * Get content type label
   */
  getContentTypeLabel(): string {
    const value = this.getSelectedContentType();
    return value === 'json' ? 'JSON' : 'Text';
  }

  /**
   * Select content type
   */
  selectContentType(value: string): void {
    if (this.selectedLorebookIndex !== null) {
      this.lorebooks.at(this.selectedLorebookIndex).patchValue({ contentType: value });
    }
    this.contentTypeDropdownOpen = false;
  }

  /**
   * Load GraphRAG settings options for dropdown
   */
  loadGraphRagSettings(): void {
    this.graphRagSettingsService.getAllSummary().subscribe({
      next: (settings) => {
        this.graphRagSettingsOptions = settings;
      },
      error: (err) => {
        console.error('Error loading GraphRAG settings:', err);
      }
    });
  }

  /**
   * Toggle GraphRAG settings dropdown
   */
  toggleGraphRagDropdown(): void {
    this.graphRagDropdownOpen = !this.graphRagDropdownOpen;
    // Close sort dropdown if open
    this.sortDropdownOpen = false;
  }

  /**
   * Select GraphRAG settings
   */
  selectGraphRagSettings(id: string | null): void {
    this.worldbookForm.patchValue({ graphRagSettingsId: id });
    this.graphRagDropdownOpen = false;
  }

  /**
   * Get selected GraphRAG settings name
   */
  getSelectedGraphRagName(): string {
    const selectedId = this.worldbookForm.get('graphRagSettingsId')?.value;
    if (!selectedId) return '-- None --';
    const found = this.graphRagSettingsOptions.find(s => s.id === selectedId);
    return found?.name || '-- None --';
  }

  /**
   * Create a FormGroup for a lorebook
   */
  createLorebookGroup(lorebook?: any): FormGroup {
    return this.fb.group({
      id: [lorebook?.id || null],
      title: [lorebook?.title || '', [Validators.required, Validators.maxLength(200)]],
      content: [lorebook?.content || '', [Validators.required, Validators.minLength(10)]],
      category: [lorebook?.category || '', [Validators.required, Validators.maxLength(100)]],
      contentType: [lorebook?.contentType || 'txt', Validators.required]
    });
  }

  /**
   * Generate a unique lorebook title
   */
  private generateUniqueTitle(): string {
    const baseName = 'New Lorebook';
    const existingTitles = new Set<string>();

    // Collect all existing titles
    for (let i = 0; i < this.lorebooks.length; i++) {
      const title = this.lorebooks.at(i).get('title')?.value;
      if (title) {
        existingTitles.add(title.toLowerCase());
      }
    }

    // Find a unique name
    if (!existingTitles.has(baseName.toLowerCase())) {
      return baseName;
    }

    let counter = 1;
    while (existingTitles.has(`${baseName} ${counter}`.toLowerCase())) {
      counter++;
    }
    return `${baseName} ${counter}`;
  }

  /**
   * Check if all lorebook titles are unique
   */
  hasDuplicateTitles(): boolean {
    const titles = new Map<string, number>();
    for (let i = 0; i < this.lorebooks.length; i++) {
      if (this.isLorebookDeleted(i)) continue;
      const title = (this.lorebooks.at(i).get('title')?.value || '').toLowerCase().trim();
      if (title) {
        titles.set(title, (titles.get(title) || 0) + 1);
      }
    }
    return Array.from(titles.values()).some(count => count > 1);
  }

  /**
   * Check if a specific lorebook has a duplicate title
   */
  isDuplicateTitle(index: number): boolean {
    if (this.isLorebookDeleted(index)) return false;
    const title = (this.lorebooks.at(index).get('title')?.value || '').toLowerCase().trim();
    if (!title) return false;

    for (let i = 0; i < this.lorebooks.length; i++) {
      if (i === index || this.isLorebookDeleted(i)) continue;
      const otherTitle = (this.lorebooks.at(i).get('title')?.value || '').toLowerCase().trim();
      if (title === otherTitle) return true;
    }
    return false;
  }

  /**
   * Check if a lorebook is indexed (cannot be modified directly)
   */
  isLorebookIndexed(index: number): boolean {
    // Only applies when worldbook is indexed
    if (this.indexingStatus !== 'Indexed' && this.indexingStatus !== 'NeedsReindexing') {
      return false;
    }

    const lorebookId = this.lorebooks.at(index)?.get('id')?.value;
    if (!lorebookId) {
      return false; // New lorebook - can be edited
    }

    const changeStatus = this.getLorebookChangeStatus(index);
    // Indexed lorebooks with no changes cannot be modified
    return changeStatus === 'None' && !this.isLorebookDeleted(index);
  }

  /**
   * Add a new lorebook to the FormArray
   */
  addLorebook(): void {
    if (!this.canEdit) return;

    const newIndex = this.lorebooks.length;
    this.lorebooks.push(this.createLorebookGroup({
      title: this.generateUniqueTitle(),
      content: 'Enter content here...',
      category: 'General',
      contentType: 'txt'
    }));
    this.applyFilters();
    this.selectLorebook(newIndex);

    // Focus on title field after a short delay
    setTimeout(() => {
      const titleInput = document.querySelector(
        `#lorebook-${newIndex}-title`
      ) as HTMLInputElement;
      titleInput?.focus();
      titleInput?.select();
    }, 100);
  }

  /**
   * Select a lorebook for detail view
   */
  selectLorebook(index: number | null): void {
    // Force view refresh by briefly clearing selection
    if (this.selectedLorebookIndex !== null && index !== null && this.selectedLorebookIndex !== index) {
      this.selectedLorebookIndex = null;
      setTimeout(() => {
        this.selectedLorebookIndex = index;
      }, 0);
    } else {
      this.selectedLorebookIndex = index;
    }
  }

  /**
   * Check if a lorebook is selected
   */
  isLorebookSelected(index: number): boolean {
    return this.selectedLorebookIndex === index;
  }

  /**
   * Copy a lorebook to create a new editable entry
   */
  copyLorebook(index: number): void {
    if (!this.canEdit) return;

    const sourceLorebook = this.lorebooks.at(index);
    const sourceTitle = sourceLorebook.get('title')?.value || 'Lorebook';
    const sourceContent = sourceLorebook.get('content')?.value || '';
    const sourceCategory = sourceLorebook.get('category')?.value || 'General';
    const sourceContentType = sourceLorebook.get('contentType')?.value || 'txt';

    // Generate unique title for the copy
    const copyTitle = this.generateUniqueCopyTitle(sourceTitle);

    const newIndex = this.lorebooks.length;
    this.lorebooks.push(this.createLorebookGroup({
      title: copyTitle,
      content: sourceContent,
      category: sourceCategory,
      contentType: sourceContentType
    }));

    this.applyFilters();
    this.selectLorebook(newIndex);

    // Focus on title field
    setTimeout(() => {
      const titleInput = document.querySelector(
        `#lorebook-${newIndex}-title`
      ) as HTMLInputElement;
      titleInput?.focus();
      titleInput?.select();
    }, 100);
  }

  /**
   * Generate a unique title for a copied lorebook
   */
  private generateUniqueCopyTitle(originalTitle: string): string {
    const baseName = `Copy of ${originalTitle}`;
    const existingTitles = new Set<string>();

    for (let i = 0; i < this.lorebooks.length; i++) {
      const title = this.lorebooks.at(i).get('title')?.value;
      if (title) {
        existingTitles.add(title.toLowerCase());
      }
    }

    if (!existingTitles.has(baseName.toLowerCase())) {
      return baseName;
    }

    let counter = 2;
    while (existingTitles.has(`${baseName} ${counter}`.toLowerCase())) {
      counter++;
    }
    return `${baseName} ${counter}`;
  }

  /**
   * Delete a lorebook from the FormArray
   */
  deleteLorebook(index: number): void {
    if (!this.canEdit) return;

    const lorebookGroup = this.lorebooks.at(index);
    const lorebookId = lorebookGroup.get('id')?.value;
    const isIndexedWorldbook = this.indexingStatus === 'Indexed' || this.indexingStatus === 'NeedsReindexing';

    // For indexed worldbooks with existing lorebooks, soft-delete
    if (isIndexedWorldbook && lorebookId) {
      // Mark as deleted instead of removing from FormArray
      this.deletedLorebookIds.add(lorebookId);
      this.lorebookChangeStatuses.set(lorebookId, 'Deleted');

      // Clear selection if this was selected
      if (this.selectedLorebookIndex === index) {
        this.selectedLorebookIndex = null;
      }

      this.applyFilters();
      return;
    }

    // For new lorebooks or non-indexed worldbooks, remove from FormArray
    this.lorebooks.removeAt(index);

    // Adjust selection
    if (this.selectedLorebookIndex !== null) {
      if (this.selectedLorebookIndex === index) {
        this.selectedLorebookIndex = null;
      } else if (this.selectedLorebookIndex > index) {
        this.selectedLorebookIndex--;
      }
    }

    this.applyFilters();
  }

  /**
   * Toggle panel expansion
   */
  togglePanel(index: number): void {
    if (this.expandedPanels.has(index)) {
      this.expandedPanels.delete(index);
    } else {
      this.expandedPanels.add(index);
    }
  }

  /**
   * Check if panel is expanded
   */
  isPanelExpanded(index: number): boolean {
    return this.expandedPanels.has(index);
  }

  /**
   * Get lorebook title or placeholder
   */
  getLorebookTitle(index: number): string {
    const title = this.lorebooks.at(index).get('title')?.value;
    return title || 'New Lorebook';
  }

  /**
   * Get lorebook category
   */
  getLorebookCategory(index: number): string {
    return this.lorebooks.at(index).get('category')?.value || '';
  }

  /**
   * Check if lorebook has errors
   */
  hasLorebookErrors(index: number): boolean {
    const group = this.lorebooks.at(index) as FormGroup;
    return group.invalid && group.touched;
  }

  /**
   * Handle keyboard events for accessibility
   */
  onKeyDown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.togglePanel(index);
    }
  }

  /**
   * TrackBy function for ngFor performance
   */
  trackByIndex(index: number): number {
    return index;
  }

  /**
   * TrackBy function for filtered indices
   */
  trackByFilteredIndex(index: number, item: number): number {
    return item;
  }

  /**
   * Check if lorebook at index is deleted
   */
  isLorebookDeleted(index: number): boolean {
    const lorebookId = this.lorebooks.at(index)?.get('id')?.value;
    if (!lorebookId) return false;
    return this.deletedLorebookIds.has(lorebookId);
  }

  /**
   * Set status filter and apply filters
   */
  setStatusFilter(filter: 'all' | 'Added' | 'Modified' | 'Deleted' | 'None'): void {
    this.statusFilter = filter;
    this.applyFilters();
  }

  /**
   * Toggle sort dropdown
   */
  toggleSortDropdown(): void {
    this.sortDropdownOpen = !this.sortDropdownOpen;
    // Close GraphRAG dropdown if open
    this.graphRagDropdownOpen = false;
  }

  /**
   * Select sort option
   */
  selectSortOption(option: 'title-asc' | 'title-desc' | 'category-asc' | 'category-desc' | 'status'): void {
    this.sortOption = option;
    this.sortDropdownOpen = false;
    this.applyFilters();
  }

  /**
   * Get current sort label
   */
  getSortLabel(): string {
    switch (this.sortOption) {
      case 'title-asc': return 'A-Z';
      case 'title-desc': return 'Z-A';
      case 'category-asc': return 'Category';
      case 'category-desc': return 'Category';
      case 'status': return 'Status';
      default: return 'Sort';
    }
  }

  /**
   * Apply filters and sorting to lorebooks
   */
  applyFilters(): void {
    const searchLower = this.searchTerm.toLowerCase().trim();

    // Get all indices
    let indices = this.lorebooks.controls.map((_, i) => i);

    // Filter by search term
    if (searchLower) {
      indices = indices.filter(i => {
        const ctrl = this.lorebooks.at(i);
        const title = (ctrl.get('title')?.value || '').toLowerCase();
        const category = (ctrl.get('category')?.value || '').toLowerCase();
        const content = (ctrl.get('content')?.value || '').toLowerCase();
        return title.includes(searchLower) || category.includes(searchLower) || content.includes(searchLower);
      });
    }

    // Filter by status
    if (this.statusFilter !== 'all') {
      indices = indices.filter(i => {
        const isDeleted = this.isLorebookDeleted(i);
        const status = this.getLorebookChangeStatus(i);

        if (this.statusFilter === 'Deleted') {
          return isDeleted;
        } else if (this.statusFilter === 'None') {
          return status === 'None' && !isDeleted;
        } else {
          return status === this.statusFilter && !isDeleted;
        }
      });
    }

    // Sort
    indices.sort((a, b) => {
      const ctrlA = this.lorebooks.at(a);
      const ctrlB = this.lorebooks.at(b);

      switch (this.sortOption) {
        case 'title-asc':
          return (ctrlA.get('title')?.value || '').localeCompare(ctrlB.get('title')?.value || '');
        case 'title-desc':
          return (ctrlB.get('title')?.value || '').localeCompare(ctrlA.get('title')?.value || '');
        case 'category-asc':
          return (ctrlA.get('category')?.value || '').localeCompare(ctrlB.get('category')?.value || '');
        case 'category-desc':
          return (ctrlB.get('category')?.value || '').localeCompare(ctrlA.get('category')?.value || '');
        case 'status': {
          const statusOrder = {'Deleted': 0, 'Modified': 1, 'Added': 2, 'None': 3};
          const statusA = this.isLorebookDeleted(a) ? 'Deleted' : this.getLorebookChangeStatus(a);
          const statusB = this.isLorebookDeleted(b) ? 'Deleted' : this.getLorebookChangeStatus(b);
          return statusOrder[statusA] - statusOrder[statusB];
        }
        default:
          return 0;
      }
    });

    this.filteredIndices = indices;
  }

  /**
   * Handle JSON file import
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    if (!file.name.endsWith('.json')) {
      this.importError = 'Please select a JSON file';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: ProgressEvent<FileReader>) => {
      try {
        const content = e.target?.result as string;
        const data = JSON.parse(content);
        this.importLorebooks(data);
      } catch (error) {
        console.error('Error parsing JSON:', error);
        this.importError = 'Invalid JSON file. Please check the file format.';
      }
    };

    reader.onerror = () => {
      this.importError = 'Failed to read file';
    };

    reader.readAsText(file);

    // Reset the input so the same file can be selected again
    input.value = '';
  }

  /**
   * Import lorebooks from parsed JSON
   */
  importLorebooks(data: any): void {
    this.importError = null;

    // Validate that data is an array
    if (!Array.isArray(data)) {
      this.importError = 'JSON must be an array of lorebook objects';
      return;
    }

    // Validate each item has required fields
    const invalidItems: number[] = [];
    data.forEach((item, index) => {
      if (!item.title || !item.content || !item.category) {
        invalidItems.push(index + 1);
      }
    });

    if (invalidItems.length > 0) {
      this.importError = `Invalid lorebook data at positions: ${invalidItems.join(', ')}. Each item must have title, content, and category.`;
      return;
    }

    // Clear existing lorebooks if any
    if (this.lorebooks.length > 0) {
      const confirmClear = confirm('This will replace existing lorebooks. Continue?');
      if (!confirmClear) {
        return;
      }
      this.lorebooks.clear();
      this.expandedPanels.clear();
    }

    // Import lorebooks
    data.forEach((item: any, index: number) => {
      const lorebookGroup = this.createLorebookGroup({
        title: item.title,
        content: item.content,
        category: item.category,
        contentType: item.contentType || 'txt'
      });
      this.lorebooks.push(lorebookGroup);

      // Expand the first few panels so users can review
      if (index < 3) {
        this.expandedPanels.add(index);
      }
    });

    this.applyFilters();
    console.log(`Imported ${data.length} lorebooks from JSON`);
  }

  /**
   * Trigger file input click
   */
  triggerFileInput(): void {
    const fileInput = document.getElementById('json-file-input') as HTMLInputElement;
    fileInput?.click();
  }

  /**
   * Load worldbook data in edit mode
   */
  loadWorldbook(id: string): void {
    this.loading = true;
    this.error = null;

    this.worldbookService.getWorldbookById(id).subscribe({
      next: (worldbook: WorldbookResponseDto) => {
        console.log('Loaded worldbook:', worldbook);
        console.log('Lorebooks count:', worldbook.lorebooks?.length || 0);

        this.worldbook = worldbook;
        this.indexingStatus = worldbook.indexingStatus;
        this.hasPendingChanges = worldbook.hasPendingChanges;

        // Track change statuses and deleted lorebooks
        this.lorebookChangeStatuses.clear();
        this.deletedLorebookIds.clear();
        worldbook.lorebooks.forEach(lb => {
          this.lorebookChangeStatuses.set(lb.id, lb.changeStatus);
          if (lb.isDeleted) {
            this.deletedLorebookIds.add(lb.id);
          }
        });

        // Patch worldbook name and settings
        this.worldbookForm.patchValue({
          name: worldbook.name,
          graphRagSettingsId: worldbook.graphRagSettingsId || null
        });

        // Clear existing lorebooks before populating
        this.lorebooks.clear();
        this.expandedPanels.clear();
        this.selectedLorebookIndex = null;

        // Populate lorebooks FormArray (include ALL lorebooks, including deleted)
        if (worldbook.lorebooks && worldbook.lorebooks.length > 0) {
          worldbook.lorebooks.forEach(lb => {
            console.log('Adding lorebook to FormArray:', lb.title, lb.isDeleted ? '(deleted)' : '');
            this.lorebooks.push(this.createLorebookGroup(lb));
          });
          console.log('FormArray length after loading:', this.lorebooks.length);
        } else {
          console.log('No lorebooks found in response');
        }

        this.applyFilters();
        this.loading = false;

        // If currently indexing, start polling
        if (this.indexingStatus === 'Indexing') {
          this.startPolling();
        }
      },
      error: (err) => {
        console.error('Error loading worldbook:', err);
        this.error = 'Failed to load worldbook. Please try again.';
        this.loading = false;
      }
    });
  }

  /**
   * Submit form (create mode only - edit mode uses auto-save)
   */
  onSubmit(): void {
    if (this.worldbookForm.invalid) {
      this.worldbookForm.markAllAsTouched();
      return;
    }

    // In edit mode, just trigger a manual save if needed
    if (this.isEditMode && this.worldbookId) {
      this.performSave();
      return;
    }

    // Create mode: Build WorldbookDto
    this.saving = true;
    this.error = null;
    const formValue = this.worldbookForm.value;

    const dto: WorldbookDto = {
      name: formValue.name,
      graphRagSettingsId: formValue.graphRagSettingsId || null,
      lorebooks: formValue.lorebooks.map((lb: any) => ({
        title: lb.title,
        content: lb.content,
        category: lb.category,
        contentType: lb.contentType
      } as CreateLorebookDto))
    };

    this.worldbookService.createWorldbook(dto).subscribe({
      next: (worldbook) => {
        this.saving = false;
        // Navigate to edit mode for the new worldbook
        this.router.navigate(['/worldbooks/edit', worldbook.id]);
      },
      error: (err) => {
        console.error('Error creating worldbook:', err);
        this.error = err.error?.message || 'Failed to create worldbook. Please try again.';
        this.saving = false;
      }
    });
  }

  /**
   * Perform the save operation
   */
  performSave(): void {
    if (!this.worldbookId || this.saving) return;

    this.saving = true;
    const formValue = this.worldbookForm.value;

    // Filter out deleted lorebooks from the update
    const nonDeletedLorebooks = formValue.lorebooks.filter((_: any, i: number) => {
      const lorebookId = this.lorebooks.at(i).get('id')?.value;
      return !lorebookId || !this.deletedLorebookIds.has(lorebookId);
    });

    const dto: WorldbookUpdateDto = {
      name: formValue.name,
      graphRagSettingsId: formValue.graphRagSettingsId || null,
      lorebooks: nonDeletedLorebooks.map((lb: any) => ({
        id: lb.id || undefined,
        title: lb.title,
        content: lb.content,
        category: lb.category,
        contentType: lb.contentType
      } as UpdateLorebookDto))
    };

    this.worldbookService.updateWorldbook(this.worldbookId, dto).subscribe({
      next: (worldbook) => {
        this.saving = false;
        this.refreshAfterSave(worldbook);
      },
      error: (err) => {
        console.error('Error saving worldbook:', err);
        this.error = err.error?.message || 'Failed to save. Please try again.';
        this.saving = false;
      }
    });
  }

  /**
   * Start indexing/reindexing the worldbook
   */
  startReindex(): void {
    if (!this.worldbookId || !this.canReindex) return;

    this.worldbookService.startIndexing(this.worldbookId).subscribe({
      next: () => {
        this.indexingStatus = 'Indexing';
        this.showSuccessMessage('Indexing started');
        // Start polling for completion
        this.startPolling();
      },
      error: (err) => {
        console.error('Error starting indexing:', err);
        this.error = err.error?.message || 'Failed to start indexing.';
      }
    });
  }

  /**
   * Start polling for indexing status
   */
  private startPolling(): void {
    this.stopPolling(); // Clear any existing polling

    this.pollingInterval = setInterval(() => {
      this.pollIndexingStatus();
    }, this.POLLING_INTERVAL_MS);
  }

  /**
   * Stop polling for indexing status
   */
  private stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }

  /**
   * Poll for indexing status
   */
  private pollIndexingStatus(): void {
    if (!this.worldbookId) {
      this.stopPolling();
      return;
    }

    this.worldbookService.getWorldbookById(this.worldbookId).subscribe({
      next: (worldbook) => {
        this.indexingStatus = worldbook.indexingStatus;

        // Stop polling if indexing is complete or failed
        if (worldbook.indexingStatus === 'Indexed' || worldbook.indexingStatus === 'Failed') {
          this.stopPolling();

          if (worldbook.indexingStatus === 'Indexed') {
            this.showSuccessMessage('Indexing completed successfully');
          } else {
            this.error = 'Indexing failed. Please try again.';
          }

          // Reload the full worldbook data
          this.loadWorldbook(this.worldbookId!);
        }
      },
      error: (err) => {
        console.error('Error polling indexing status:', err);
        this.stopPolling();
      }
    });
  }

  /**
   * Show success message with auto-dismiss
   */
  private showSuccessMessage(message: string): void {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = null;
    }, 3000);
  }

  /**
   * Open graph visualization in a new tab
   */
  viewGraph(): void {
    if (!this.worldbookId) return;

    this.worldbookService.getVisualizationUrl(this.worldbookId).subscribe({
      next: (response) => {
        window.open(response.visualizationUrl, '_blank');
      },
      error: (err) => {
        console.error('Error getting visualization URL:', err);
        this.error = err.error?.message || 'Failed to get visualization URL. Make sure the worldbook is indexed.';
      }
    });
  }

  /**
   * Check if graph can be viewed (indexed or needs reindexing)
   */
  get canViewGraph(): boolean {
    return this.indexingStatus === 'Indexed' || this.indexingStatus === 'NeedsReindexing';
  }

  /**
   * Refresh component state after save
   */
  private refreshAfterSave(worldbook: WorldbookResponseDto): void {
    this.worldbook = worldbook;
    this.indexingStatus = worldbook.indexingStatus;
    this.hasPendingChanges = worldbook.hasPendingChanges;

    // Clear and rebuild tracking maps
    this.lorebookChangeStatuses.clear();
    this.deletedLorebookIds.clear();
    this.pendingDeletes = [];
    this.selectedLorebookIndex = null;

    worldbook.lorebooks.forEach(lb => {
      this.lorebookChangeStatuses.set(lb.id, lb.changeStatus);
      if (lb.isDeleted) {
        this.deletedLorebookIds.add(lb.id);
      }
    });

    // Clear and rebuild FormArray
    this.lorebooks.clear();
    this.expandedPanels.clear();

    worldbook.lorebooks.forEach(lb => {
      this.lorebooks.push(this.createLorebookGroup(lb));
    });

    this.applyFilters();
  }

  cancel(): void {
    this.router.navigate(['/worldbooks']);
  }

  /**
   * Get the change status for a lorebook by its ID
   */
  getLorebookChangeStatus(index: number): LorebookChangeStatus {
    const lorebookId = this.lorebooks.at(index).get('id')?.value;
    if (!lorebookId) {
      return 'Added'; // New lorebook without ID
    }
    return this.lorebookChangeStatuses.get(lorebookId) || 'None';
  }

  /**
   * Check if lorebook has a specific change status
   */
  isLorebookAdded(index: number): boolean {
    return this.getLorebookChangeStatus(index) === 'Added';
  }

  isLorebookModified(index: number): boolean {
    return this.getLorebookChangeStatus(index) === 'Modified';
  }

  /**
   * Get deleted lorebooks for display
   */
  getDeletedLorebooks(): LorebookResponseDto[] {
    return this.worldbook?.lorebooks.filter(lb => lb.isDeleted) || [];
  }

  /**
   * Revert all pending changes
   */
  revertAllChanges(): void {
    if (!this.worldbookId) {
      return;
    }

    this.reverting = true;
    this.selectedLorebookIndex = null;
    this.worldbookService.revertAllChanges(this.worldbookId).subscribe({
      next: (worldbook) => {
        this.reverting = false;
        // Reload the worldbook
        this.lorebooks.clear();
        this.expandedPanels.clear();
        this.deletedLorebookIds.clear();
        this.lorebookChangeStatuses.clear();
        this.loadWorldbook(this.worldbookId!);
      },
      error: (err) => {
        console.error('Error reverting changes:', err);
        this.error = 'Failed to revert changes. Please try again.';
        this.reverting = false;
      }
    });
  }

  /**
   * Revert a single lorebook
   */
  revertLorebook(index: number): void {
    const lorebookId = this.lorebooks.at(index).get('id')?.value;
    if (!this.worldbookId || !lorebookId) {
      return;
    }

    this.worldbookService.revertLorebook(this.worldbookId, lorebookId).subscribe({
      next: (lorebook) => {
        // Update the form with reverted data
        const group = this.lorebooks.at(index);
        group.patchValue({
          title: lorebook.title,
          content: lorebook.content,
          category: lorebook.category,
          contentType: lorebook.contentType
        });
        this.lorebookChangeStatuses.set(lorebookId, 'None');

        // Check if there are still pending changes
        this.hasPendingChanges = Array.from(this.lorebookChangeStatuses.values()).some(s => s !== 'None');
        if (!this.hasPendingChanges && this.indexingStatus === 'NeedsReindexing') {
          this.indexingStatus = 'Indexed';
        }

        this.applyFilters();
      },
      error: (err) => {
        console.error('Error reverting lorebook:', err);
        this.error = 'Failed to revert lorebook. Please try again.';
      }
    });
  }

  /**
   * Restore a deleted lorebook by index
   */
  restoreLorebookByIndex(index: number): void {
    const lorebookId = this.lorebooks.at(index)?.get('id')?.value;
    if (!this.worldbookId || !lorebookId) return;

    this.worldbookService.revertLorebook(this.worldbookId, lorebookId).subscribe({
      next: (restored) => {
        // Update the form with restored data
        const group = this.lorebooks.at(index);
        group.patchValue({
          title: restored.title,
          content: restored.content,
          category: restored.category,
          contentType: restored.contentType
        });

        this.deletedLorebookIds.delete(lorebookId);
        this.lorebookChangeStatuses.set(lorebookId, 'None');

        // Update worldbook state
        if (this.worldbook) {
          const idx = this.worldbook.lorebooks.findIndex(lb => lb.id === lorebookId);
          if (idx !== -1) {
            this.worldbook.lorebooks[idx].isDeleted = false;
            this.worldbook.lorebooks[idx].changeStatus = 'None';
          }
        }

        this.applyFilters();
      },
      error: (err) => {
        console.error('Error restoring lorebook:', err);
        this.error = 'Failed to restore lorebook. Please try again.';
      }
    });
  }

  /**
   * Restore a deleted lorebook (legacy method for compatibility)
   */
  restoreDeletedLorebook(lorebook: LorebookResponseDto): void {
    // Find the index of the lorebook in the FormArray
    const index = this.lorebooks.controls.findIndex(ctrl => ctrl.get('id')?.value === lorebook.id);
    if (index !== -1) {
      this.restoreLorebookByIndex(index);
    }
  }
}
