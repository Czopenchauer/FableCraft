import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { WorldbookService } from '../../services/worldbook.service';
import { WorldbookDto, WorldbookUpdateDto, WorldbookResponseDto, UpdateLorebookDto, CreateLorebookDto, ContentType } from '../../models/worldbook.model';

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

  constructor(
    private fb: FormBuilder,
    private worldbookService: WorldbookService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.worldbookForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      lorebooks: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.worldbookId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.worldbookId;

    if (this.isEditMode && this.worldbookId) {
      this.loadWorldbook(this.worldbookId);
    }
  }

  get lorebooks(): FormArray {
    return this.worldbookForm.get('lorebooks') as FormArray;
  }

  get nameControl() {
    return this.worldbookForm.get('name');
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
    // Show confirmation dialog
    if (!confirm('Are you sure you want to delete this lorebook?')) {
      return;
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

        // Patch worldbook name
        this.worldbookForm.patchValue({
          name: worldbook.name
        });

        // Populate lorebooks FormArray
        const lorebooksArray = this.lorebooks;
        if (worldbook.lorebooks && worldbook.lorebooks.length > 0) {
          worldbook.lorebooks.forEach(lb => {
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
}
