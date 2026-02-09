import {Component, OnInit} from '@angular/core';
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
export class WorldbookFormComponent implements OnInit {
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

  // Worldbook state
  worldbook: WorldbookResponseDto | null = null;
  indexingStatus: IndexingStatus = 'NotIndexed';
  hasPendingChanges = false;
  reverting = false;

  // Lorebook change tracking
  lorebookChangeStatuses: Map<string, LorebookChangeStatus> = new Map();
  deletedLorebookIds: Set<string> = new Set();

  // Pending deletes during current edit session (not yet saved to server)
  pendingDeletes: LorebookResponseDto[] = [];

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

  ngOnInit(): void {
    this.worldbookId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.worldbookId;

    // Load GraphRAG settings options
    this.loadGraphRagSettings();

    if (this.isEditMode && this.worldbookId) {
      this.loadWorldbook(this.worldbookId);
    }
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
   * Add a new lorebook to the FormArray
   */
  addLorebook(): void {
    const newIndex = this.lorebooks.length;
    this.lorebooks.push(this.createLorebookGroup());
    this.expandedPanels.add(newIndex);

    // Focus on title field after a short delay
    setTimeout(() => {
      const titleInput = document.querySelector(
        `#lorebook-${newIndex}-title`
      ) as HTMLInputElement;
      titleInput?.focus();
    }, 100);
  }

  /**
   * Delete a lorebook from the FormArray
   */
  deleteLorebook(index: number): void {
    const lorebookGroup = this.lorebooks.at(index);
    const lorebookId = lorebookGroup.get('id')?.value;
    const isIndexedWorldbook = this.indexingStatus === 'Indexed' || this.indexingStatus === 'NeedsReindexing';

    // For indexed worldbooks with existing lorebooks, show soft-delete warning
    if (isIndexedWorldbook && lorebookId) {
      if (!confirm('This lorebook will be marked for deletion. The deletion will be applied when you re-index the worldbook. Continue?')) {
        return;
      }

      // Track as pending delete so it shows in the "Deleted Lorebooks" section
      const lorebookData: LorebookResponseDto = {
        id: lorebookId,
        worldbookId: this.worldbookId || '',
        title: lorebookGroup.get('title')?.value || '',
        content: lorebookGroup.get('content')?.value || '',
        category: lorebookGroup.get('category')?.value || '',
        contentType: lorebookGroup.get('contentType')?.value || 'txt',
        isDeleted: true,
        changeStatus: 'Deleted'
      };
      this.pendingDeletes.push(lorebookData);
    } else {
      // For new lorebooks or non-indexed worldbooks, just confirm deletion
      if (!confirm('Are you sure you want to delete this lorebook?')) {
        return;
      }
    }

    this.lorebooks.removeAt(index);

    // Adjust expanded panels
    const newExpanded = new Set<number>();
    this.expandedPanels.forEach(i => {
      if (i > index) {
        newExpanded.add(i - 1);
      } else if (i < index) {
        newExpanded.add(i);
      }
    });
    this.expandedPanels = newExpanded;
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

        // Populate lorebooks FormArray (only non-deleted lorebooks for editing)
        const lorebooksArray = this.lorebooks;
        if (worldbook.lorebooks && worldbook.lorebooks.length > 0) {
          worldbook.lorebooks
            .filter(lb => !lb.isDeleted)
            .forEach(lb => {
              console.log('Adding lorebook to FormArray:', lb.title);
              lorebooksArray.push(this.createLorebookGroup(lb));
            });
          console.log('FormArray length after loading:', this.lorebooks.length);
        } else {
          console.log('No lorebooks found in response');
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading worldbook:', err);
        this.error = 'Failed to load worldbook. Please try again.';
        this.loading = false;
      }
    });
  }

  /**
   * Submit form (create or update)
   */
  onSubmit(): void {
    if (this.worldbookForm.invalid) {
      this.worldbookForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = null;

    const formValue = this.worldbookForm.value;

    if (this.isEditMode && this.worldbookId) {
      // Edit mode: Build WorldbookUpdateDto
      const dto: WorldbookUpdateDto = {
        name: formValue.name,
        graphRagSettingsId: formValue.graphRagSettingsId || null,
        lorebooks: formValue.lorebooks.map((lb: any) => ({
          id: lb.id || undefined,  // Include id for existing, omit for new
          title: lb.title,
          content: lb.content,
          category: lb.category,
          contentType: lb.contentType
        } as UpdateLorebookDto))
      };

      this.worldbookService.updateWorldbook(this.worldbookId, dto).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/worldbooks']);
        },
        error: (err) => {
          console.error('Error updating worldbook:', err);
          this.error = err.error?.message || 'Failed to update worldbook. Please try again.';
          this.saving = false;
        }
      });
    } else {
      // Create mode: Build WorldbookDto
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
        next: () => {
          this.saving = false;
          this.router.navigate(['/worldbooks']);
        },
        error: (err) => {
          console.error('Error creating worldbook:', err);
          this.error = err.error?.message || 'Failed to create worldbook. Please try again.';
          this.saving = false;
        }
      });
    }
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
    if (!this.worldbookId || !confirm('Are you sure you want to revert all pending changes? This will restore all lorebooks to their last indexed state.')) {
      return;
    }

    this.reverting = true;
    this.worldbookService.revertAllChanges(this.worldbookId).subscribe({
      next: (worldbook) => {
        this.reverting = false;
        // Reload the worldbook
        this.lorebooks.clear();
        this.expandedPanels.clear();
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

    if (!confirm('Are you sure you want to revert this lorebook to its last indexed state?')) {
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
      },
      error: (err) => {
        console.error('Error reverting lorebook:', err);
        this.error = 'Failed to revert lorebook. Please try again.';
      }
    });
  }

  /**
   * Restore a deleted lorebook
   */
  restoreDeletedLorebook(lorebook: LorebookResponseDto): void {
    if (!this.worldbookId) return;

    this.worldbookService.revertLorebook(this.worldbookId, lorebook.id).subscribe({
      next: (restored) => {
        // Add the restored lorebook to the form
        this.lorebooks.push(this.createLorebookGroup({
          id: restored.id,
          title: restored.title,
          content: restored.content,
          category: restored.category,
          contentType: restored.contentType
        }));
        this.deletedLorebookIds.delete(lorebook.id);
        this.lorebookChangeStatuses.set(lorebook.id, 'None');

        // Update worldbook state
        if (this.worldbook) {
          const idx = this.worldbook.lorebooks.findIndex(lb => lb.id === lorebook.id);
          if (idx !== -1) {
            this.worldbook.lorebooks[idx].isDeleted = false;
            this.worldbook.lorebooks[idx].changeStatus = 'None';
          }
        }
      },
      error: (err) => {
        console.error('Error restoring lorebook:', err);
        this.error = 'Failed to restore lorebook. Please try again.';
      }
    });
  }
}
