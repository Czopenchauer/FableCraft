import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';
import {AdventureSettingsResponseDto, UpdateAdventureSettingsDto} from '../../models/adventure-settings.model';
import {LlmPresetResponseDto} from '../../models/llm-preset.model';
import {AdventureService} from '../../services/adventure.service';
import {LlmPresetService} from '../../services/llm-preset.service';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-adventure-settings-modal',
  standalone: false,
  templateUrl: './adventure-settings-modal.component.html',
  styleUrl: './adventure-settings-modal.component.css'
})
export class AdventureSettingsModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<AdventureSettingsResponseDto>();

  isLoading = false;
  isSaving = false;
  settings: AdventureSettingsResponseDto | null = null;
  availablePresets: LlmPresetResponseDto[] = [];

  // Form state
  promptPath = '';
  agentPresets: { agentName: string; llmPresetId: string | null }[] = [];

  // Editing LLM preset state
  editingPreset: LlmPresetResponseDto | null = null;
  isCreatingPreset = false;

  constructor(
    private adventureService: AdventureService,
    private llmPresetService: LlmPresetService,
    private toastService: ToastService
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
      if (this.isOpen && this.adventureId) {
        this.loadData();
      }
      if (!this.isOpen) {
        this.resetState();
      }
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  onClose(): void {
    if (!this.isSaving) {
      this.close.emit();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isSaving) {
      this.onClose();
    }
  }

  onSave(): void {
    if (!this.adventureId || this.isSaving) return;

    this.isSaving = true;

    const updateDto: UpdateAdventureSettingsDto = {
      promptPath: this.promptPath,
      agentLlmPresets: this.agentPresets.map(p => ({
        agentName: p.agentName,
        llmPresetId: p.llmPresetId
      }))
    };

    this.adventureService.updateAdventureSettings(this.adventureId, updateDto).subscribe({
      next: (updatedSettings) => {
        this.isSaving = false;
        this.toastService.success('Adventure settings saved successfully.');
        this.saved.emit(updatedSettings);
        this.onClose();
      },
      error: (err) => {
        console.error('Error saving adventure settings:', err);
        this.toastService.error('Failed to save adventure settings.');
        this.isSaving = false;
      }
    });
  }

  getPresetForAgent(agentName: string): string | null {
    const preset = this.agentPresets.find(p => p.agentName === agentName);
    return preset?.llmPresetId || null;
  }

  onPromptPathSelected(path: string): void {
    this.promptPath = path;
  }

  applyPresetToAll(presetId: string | null): void {
    if (!presetId) return;

    for (const preset of this.agentPresets) {
      preset.llmPresetId = presetId;
    }
  }

  getPresetName(presetId: string | null): string {
    if (!presetId) return 'Not assigned';
    const preset = this.availablePresets.find(p => p.id === presetId);
    return preset?.name || 'Unknown';
  }

  formatAgentName(agentName: string): string {
    return agentName
      .replace(/Agent$/, '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  // LLM Preset editing
  onEditPreset(presetId: string | null): void {
    if (!presetId) return;
    const preset = this.availablePresets.find(p => p.id === presetId);
    if (preset) {
      this.editingPreset = {...preset};
    }
  }

  onCreatePreset(): void {
    this.isCreatingPreset = true;
    this.editingPreset = {
      id: '',
      name: '',
      provider: '',
      model: '',
      baseUrl: null,
      apiKey: '',
      maxTokens: 4096,
      temperature: 0.7,
      topP: null,
      topK: null,
      frequencyPenalty: null,
      presencePenalty: null,
      createdAt: new Date().toISOString(),
      updatedAt: null
    };
  }

  onCancelPresetEdit(): void {
    this.editingPreset = null;
    this.isCreatingPreset = false;
  }

  onSavePreset(): void {
    if (!this.editingPreset) return;

    const presetDto = {
      name: this.editingPreset.name,
      provider: this.editingPreset.provider,
      model: this.editingPreset.model,
      baseUrl: this.editingPreset.baseUrl,
      apiKey: this.editingPreset.apiKey,
      maxTokens: this.editingPreset.maxTokens,
      temperature: this.editingPreset.temperature,
      topP: this.editingPreset.topP,
      topK: this.editingPreset.topK,
      frequencyPenalty: this.editingPreset.frequencyPenalty,
      presencePenalty: this.editingPreset.presencePenalty
    };

    if (this.isCreatingPreset) {
      this.llmPresetService.createPreset(presetDto).subscribe({
        next: (newPreset) => {
          this.availablePresets.push(newPreset);
          this.toastService.success('LLM preset created successfully.');
          this.editingPreset = null;
          this.isCreatingPreset = false;
        },
        error: (err) => {
          console.error('Error creating LLM preset:', err);
          this.toastService.error('Failed to create LLM preset.');
        }
      });
    } else {
      this.llmPresetService.updatePreset(this.editingPreset.id, presetDto).subscribe({
        next: (updatedPreset) => {
          const index = this.availablePresets.findIndex(p => p.id === updatedPreset.id);
          if (index !== -1) {
            this.availablePresets[index] = updatedPreset;
          }
          this.toastService.success('LLM preset updated successfully.');
          this.editingPreset = null;
        },
        error: (err) => {
          console.error('Error updating LLM preset:', err);
          this.toastService.error('Failed to update LLM preset.');
        }
      });
    }
  }

  private loadData(): void {
    if (!this.adventureId) return;

    this.isLoading = true;

    // Load both settings and available presets
    Promise.all([
      this.adventureService.getAdventureSettings(this.adventureId).toPromise(),
      this.llmPresetService.getAllPresets().toPromise()
    ]).then(([settings, presets]) => {
      this.settings = settings!;
      this.availablePresets = presets || [];
      this.initializeForm();
      this.isLoading = false;
    }).catch(err => {
      console.error('Error loading adventure settings:', err);
      this.toastService.error('Failed to load adventure settings.');
      this.isLoading = false;
      this.onClose();
    });
  }

  private initializeForm(): void {
    if (!this.settings) return;

    this.promptPath = this.settings.promptPath;
    this.agentPresets = this.settings.agentLlmPresets.map(preset => ({
      agentName: preset.agentName,
      llmPresetId: preset.llmPresetId || null
    }));
  }

  private resetState(): void {
    this.settings = null;
    this.promptPath = '';
    this.agentPresets = [];
    this.editingPreset = null;
    this.isCreatingPreset = false;
  }
}
