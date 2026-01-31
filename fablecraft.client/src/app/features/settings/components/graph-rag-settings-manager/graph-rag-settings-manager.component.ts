import {Component, EventEmitter, Input, OnChanges, OnInit, Output} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {GraphRagSettingsDto, GraphRagSettingsResponseDto} from '../../models/graph-rag-settings.model';
import {GraphRagSettingsService} from '../../services/graph-rag-settings.service';

@Component({
  selector: 'app-graph-rag-settings-manager',
  standalone: false,
  templateUrl: './graph-rag-settings-manager.component.html',
  styleUrl: './graph-rag-settings-manager.component.css'
})
export class GraphRagSettingsManagerComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  settings: GraphRagSettingsResponseDto[] = [];
  isLoading = false;
  isSaving = false;
  isDeleting = false;
  errorMessage = '';

  // Form state
  showForm = false;
  editingSettings: GraphRagSettingsResponseDto | null = null;
  settingsForm!: FormGroup;

  // Delete confirmation
  showDeleteConfirmation = false;
  settingsToDelete: GraphRagSettingsResponseDto | null = null;
  deleteError: { worldbooks: string[], adventures: string[] } | null = null;

  constructor(
    private fb: FormBuilder,
    private graphRagSettingsService: GraphRagSettingsService
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

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.graphRagSettingsService.getAll().subscribe({
      next: (data) => {
        this.settings = data;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load GraphRAG settings';
        this.isLoading = false;
        console.error('Error loading GraphRAG settings:', error);
      }
    });
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isSaving && !this.isDeleting) {
      this.closeModal();
    }
  }

  closeModal(): void {
    if (!this.isSaving && !this.isDeleting) {
      this.showForm = false;
      this.editingSettings = null;
      this.showDeleteConfirmation = false;
      this.settingsToDelete = null;
      this.deleteError = null;
      this.close.emit();
    }
  }

  openCreateForm(): void {
    this.editingSettings = null;
    this.settingsForm.reset({
      name: '',
      llmProvider: 'gemini',
      llmModel: 'gemini-2.0-flash',
      llmEndpoint: '',
      llmApiKey: '',
      llmApiVersion: '',
      llmMaxTokens: 4096,
      llmRateLimitEnabled: false,
      llmRateLimitRequests: 0,
      llmRateLimitInterval: 0,
      embeddingProvider: 'gemini',
      embeddingModel: 'text-embedding-004',
      embeddingEndpoint: '',
      embeddingApiKey: '',
      embeddingApiVersion: '',
      embeddingDimensions: 768,
      embeddingMaxTokens: 8191,
      embeddingBatchSize: 100,
      huggingfaceTokenizer: ''
    });
    this.showForm = true;
  }

  openEditForm(settings: GraphRagSettingsResponseDto): void {
    this.editingSettings = settings;
    this.settingsForm.patchValue({
      name: settings.name,
      llmProvider: settings.llmProvider,
      llmModel: settings.llmModel,
      llmEndpoint: settings.llmEndpoint || '',
      llmApiKey: settings.llmApiKey,
      llmApiVersion: settings.llmApiVersion || '',
      llmMaxTokens: settings.llmMaxTokens,
      llmRateLimitEnabled: settings.llmRateLimitEnabled,
      llmRateLimitRequests: settings.llmRateLimitRequests,
      llmRateLimitInterval: settings.llmRateLimitInterval,
      embeddingProvider: settings.embeddingProvider,
      embeddingModel: settings.embeddingModel,
      embeddingEndpoint: settings.embeddingEndpoint || '',
      embeddingApiKey: settings.embeddingApiKey || '',
      embeddingApiVersion: settings.embeddingApiVersion || '',
      embeddingDimensions: settings.embeddingDimensions,
      embeddingMaxTokens: settings.embeddingMaxTokens,
      embeddingBatchSize: settings.embeddingBatchSize,
      huggingfaceTokenizer: settings.huggingfaceTokenizer || ''
    });
    this.showForm = true;
  }

  copySettings(settings: GraphRagSettingsResponseDto): void {
    this.editingSettings = null;
    this.settingsForm.patchValue({
      name: `${settings.name} (Copy)`,
      llmProvider: settings.llmProvider,
      llmModel: settings.llmModel,
      llmEndpoint: settings.llmEndpoint || '',
      llmApiKey: settings.llmApiKey,
      llmApiVersion: settings.llmApiVersion || '',
      llmMaxTokens: settings.llmMaxTokens,
      llmRateLimitEnabled: settings.llmRateLimitEnabled,
      llmRateLimitRequests: settings.llmRateLimitRequests,
      llmRateLimitInterval: settings.llmRateLimitInterval,
      embeddingProvider: settings.embeddingProvider,
      embeddingModel: settings.embeddingModel,
      embeddingEndpoint: settings.embeddingEndpoint || '',
      embeddingApiKey: settings.embeddingApiKey || '',
      embeddingApiVersion: settings.embeddingApiVersion || '',
      embeddingDimensions: settings.embeddingDimensions,
      embeddingMaxTokens: settings.embeddingMaxTokens,
      embeddingBatchSize: settings.embeddingBatchSize,
      huggingfaceTokenizer: settings.huggingfaceTokenizer || ''
    });
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingSettings = null;
    this.settingsForm.reset();
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) {
      Object.keys(this.settingsForm.controls).forEach(key => {
        this.settingsForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.settingsForm.value;
    const settingsDto: GraphRagSettingsDto = {
      name: formValue.name,
      llmProvider: formValue.llmProvider,
      llmModel: formValue.llmModel,
      llmEndpoint: formValue.llmEndpoint || null,
      llmApiKey: formValue.llmApiKey,
      llmApiVersion: formValue.llmApiVersion || null,
      llmMaxTokens: formValue.llmMaxTokens,
      llmRateLimitEnabled: formValue.llmRateLimitEnabled,
      llmRateLimitRequests: formValue.llmRateLimitRequests,
      llmRateLimitInterval: formValue.llmRateLimitInterval,
      embeddingProvider: formValue.embeddingProvider,
      embeddingModel: formValue.embeddingModel,
      embeddingEndpoint: formValue.embeddingEndpoint || null,
      embeddingApiKey: formValue.embeddingApiKey || null,
      embeddingApiVersion: formValue.embeddingApiVersion || null,
      embeddingDimensions: formValue.embeddingDimensions,
      embeddingMaxTokens: formValue.embeddingMaxTokens,
      embeddingBatchSize: formValue.embeddingBatchSize,
      huggingfaceTokenizer: formValue.huggingfaceTokenizer || null
    };

    const saveOperation = this.editingSettings
      ? this.graphRagSettingsService.update(this.editingSettings.id, settingsDto)
      : this.graphRagSettingsService.create(settingsDto);

    saveOperation.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;
        this.editingSettings = null;
        this.loadData();
      },
      error: (error) => {
        this.isSaving = false;
        this.errorMessage = error.error?.message || 'Failed to save settings';
        console.error('Error saving settings:', error);
      }
    });
  }

  confirmDelete(settings: GraphRagSettingsResponseDto): void {
    this.settingsToDelete = settings;
    this.deleteError = null;
    this.showDeleteConfirmation = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirmation = false;
    this.settingsToDelete = null;
    this.deleteError = null;
  }

  deleteSettings(): void {
    if (!this.settingsToDelete) return;

    this.isDeleting = true;
    this.errorMessage = '';

    this.graphRagSettingsService.delete(this.settingsToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteConfirmation = false;
        this.settingsToDelete = null;
        this.deleteError = null;
        this.loadData();
      },
      error: (error) => {
        this.isDeleting = false;
        if (error.error?.worldbooks || error.error?.adventures) {
          this.deleteError = {
            worldbooks: error.error.worldbooks || [],
            adventures: error.error.adventures || []
          };
        } else {
          this.errorMessage = error.error?.message || 'Failed to delete settings';
        }
        console.error('Error deleting settings:', error);
      }
    });
  }

  getFieldError(fieldName: string): string {
    const field = this.settingsForm.get(fieldName);
    if (!field || !field.touched || !field.errors) return '';

    if (field.errors['required']) return 'This field is required';
    if (field.errors['maxlength']) return `Maximum length is ${field.errors['maxlength'].requiredLength}`;
    if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;

    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.settingsForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  private initializeForm(): void {
    this.settingsForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      // LLM Configuration
      llmProvider: ['', [Validators.required, Validators.maxLength(50)]],
      llmModel: ['', [Validators.required, Validators.maxLength(200)]],
      llmEndpoint: ['', [Validators.maxLength(500)]],
      llmApiKey: ['', [Validators.required, Validators.maxLength(500)]],
      llmApiVersion: ['', [Validators.maxLength(50)]],
      llmMaxTokens: [4096, [Validators.required, Validators.min(1)]],
      llmRateLimitEnabled: [false],
      llmRateLimitRequests: [0, [Validators.min(0)]],
      llmRateLimitInterval: [0, [Validators.min(0)]],
      // Embedding Configuration
      embeddingProvider: ['', [Validators.required, Validators.maxLength(50)]],
      embeddingModel: ['', [Validators.required, Validators.maxLength(200)]],
      embeddingEndpoint: ['', [Validators.maxLength(500)]],
      embeddingApiKey: ['', [Validators.maxLength(500)]],
      embeddingApiVersion: ['', [Validators.maxLength(50)]],
      embeddingDimensions: [1536, [Validators.required, Validators.min(1)]],
      embeddingMaxTokens: [8191, [Validators.required, Validators.min(1)]],
      embeddingBatchSize: [100, [Validators.required, Validators.min(1)]],
      huggingfaceTokenizer: ['', [Validators.maxLength(200)]]
    });
  }
}
