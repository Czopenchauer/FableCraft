import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {LlmPresetDto, LlmPresetResponseDto} from '../../models/llm-preset.model';
import {LlmPresetService} from '../../services/llm-preset.service';
import {AdventureService} from '../../services/adventure.service';
import {AdventureListItemDto} from '../../models/adventure.model';
import {catchError, forkJoin, of} from 'rxjs';

interface PresetWithUsage extends LlmPresetResponseDto {
  adventuresUsing: AdventureListItemDto[];
}

@Component({
  selector: 'app-llm-preset-manager',
  standalone: false,
  templateUrl: './llm-preset-manager.component.html',
  styleUrl: './llm-preset-manager.component.css'
})
export class LlmPresetManagerComponent implements OnInit {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  presets: PresetWithUsage[] = [];
  adventures: AdventureListItemDto[] = [];
  isLoading = false;
  isSaving = false;
  isDeleting = false;
  errorMessage = '';

  // Form state
  showForm = false;
  editingPreset: LlmPresetResponseDto | null = null;
  presetForm!: FormGroup;

  // Delete confirmation
  showDeleteConfirmation = false;
  presetToDelete: PresetWithUsage | null = null;

  constructor(
    private fb: FormBuilder,
    private llmPresetService: LlmPresetService,
    private adventureService: AdventureService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    if (this.isOpen) {
      this.loadData();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.loadData();
    }
  }

  private initializeForm(): void {
    this.presetForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      provider: ['', [Validators.required, Validators.maxLength(50)]],
      model: ['', [Validators.required, Validators.maxLength(200)]],
      baseUrl: ['', [Validators.maxLength(500)]],
      apiKey: ['', [Validators.required, Validators.maxLength(500)]],
      maxTokens: [2000, [Validators.required, Validators.min(1)]],
      temperature: [1, [Validators.min(0), Validators.max(2)]],
      topP: [1.0, [Validators.min(0), Validators.max(1)]],
      topK: [50, [Validators.min(1), Validators.max(100)]],
      frequencyPenalty: [0, [Validators.min(-2), Validators.max(2)]],
      presencePenalty: [0, [Validators.min(-2), Validators.max(2)]]
    });
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      presets: this.llmPresetService.getAllPresets(),
      adventures: this.adventureService.getAllAdventures()
    }).subscribe({
      next: (data) => {
        this.adventures = data.adventures;
        this.presets = data.presets.map(preset => ({
          ...preset,
          adventuresUsing: this.getAdventuresUsingPreset(preset.id, data.adventures)
        }));
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load presets';
        this.isLoading = false;
        console.error('Error loading presets:', error);
      }
    });
  }

  private getAdventuresUsingPreset(presetId: string, adventures: AdventureListItemDto[]): AdventureListItemDto[] {
    // Note: We need to check if adventures have fastPresetId or complexPresetId
    // This will require updating the AdventureListItemDto model to include these fields
    return [];
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isSaving && !this.isDeleting) {
      this.closeModal();
    }
  }

  closeModal(): void {
    if (!this.isSaving && !this.isDeleting) {
      this.showForm = false;
      this.editingPreset = null;
      this.showDeleteConfirmation = false;
      this.presetToDelete = null;
      this.close.emit();
    }
  }

  openCreateForm(): void {
    this.editingPreset = null;
    this.presetForm.reset({
      name: '',
      provider: '',
      model: '',
      baseUrl: '',
      apiKey: '',
      maxTokens: 2000,
      temperature: 1,
      topP: 1.0,
      topK: 50,
      frequencyPenalty: 0,
      presencePenalty: 0
    });
    this.showForm = true;
  }

  openEditForm(preset: LlmPresetResponseDto): void {
    this.editingPreset = preset;
    this.presetForm.patchValue({
      name: preset.name,
      provider: preset.provider,
      model: preset.model,
      baseUrl: preset.baseUrl || '',
      apiKey: preset.apiKey,
      maxTokens: preset.maxTokens,
      temperature: preset.temperature ?? 1,
      topP: preset.topP ?? 1.0,
      topK: preset.topK ?? 50,
      frequencyPenalty: preset.frequencyPenalty ?? 0,
      presencePenalty: preset.presencePenalty ?? 0
    });
    this.showForm = true;
  }

  copyPreset(preset: LlmPresetResponseDto): void {
    this.editingPreset = null; // This is a new preset, not editing
    this.presetForm.patchValue({
      name: `${preset.name} (Copy)`,
      provider: preset.provider,
      model: preset.model,
      baseUrl: preset.baseUrl || '',
      apiKey: preset.apiKey,
      maxTokens: preset.maxTokens,
      temperature: preset.temperature ?? 1,
      topP: preset.topP ?? 1.0,
      topK: preset.topK ?? 50,
      frequencyPenalty: preset.frequencyPenalty ?? 0,
      presencePenalty: preset.presencePenalty ?? 0
    });
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingPreset = null;
    this.presetForm.reset();
  }

  savePreset(): void {
    if (this.presetForm.invalid) {
      Object.keys(this.presetForm.controls).forEach(key => {
        this.presetForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.presetForm.value;
    const presetDto: LlmPresetDto = {
      name: formValue.name,
      provider: formValue.provider,
      model: formValue.model,
      baseUrl: formValue.baseUrl || null,
      apiKey: formValue.apiKey,
      maxTokens: formValue.maxTokens,
      temperature: formValue.temperature,
      topP: formValue.topP,
      topK: formValue.topK,
      frequencyPenalty: formValue.frequencyPenalty,
      presencePenalty: formValue.presencePenalty
    };

    const saveOperation = this.editingPreset
      ? this.llmPresetService.updatePreset(this.editingPreset.id, presetDto)
      : this.llmPresetService.createPreset(presetDto);

    saveOperation.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;
        this.editingPreset = null;
        this.loadData();
      },
      error: (error) => {
        this.isSaving = false;
        this.errorMessage = error.error?.message || 'Failed to save preset';
        console.error('Error saving preset:', error);
      }
    });
  }

  confirmDelete(preset: PresetWithUsage): void {
    this.presetToDelete = preset;
    this.showDeleteConfirmation = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirmation = false;
    this.presetToDelete = null;
  }

  deletePreset(): void {
    if (!this.presetToDelete) return;

    this.isDeleting = true;
    this.errorMessage = '';

    this.llmPresetService.deletePreset(this.presetToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteConfirmation = false;
        this.presetToDelete = null;
        this.loadData();
      },
      error: (error) => {
        this.isDeleting = false;
        this.errorMessage = error.error?.message || 'Failed to delete preset';
        console.error('Error deleting preset:', error);
      }
    });
  }

  getFieldError(fieldName: string): string {
    const field = this.presetForm.get(fieldName);
    if (!field || !field.touched || !field.errors) return '';

    if (field.errors['required']) return 'This field is required';
    if (field.errors['maxlength']) return `Maximum length is ${field.errors['maxlength'].requiredLength}`;
    if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;
    if (field.errors['max']) return `Maximum value is ${field.errors['max'].max}`;

    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.presetForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }
}
