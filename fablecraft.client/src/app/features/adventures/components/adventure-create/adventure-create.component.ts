import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {AdventureService} from '../../services/adventure.service';
import {AdventureAgentLlmPresetDto, AdventureDto} from '../../models/adventure.model';
import {Subject, takeUntil, forkJoin} from 'rxjs';
import {LlmPresetService} from '../../services/llm-preset.service';
import {WorldbookService} from '../../services/worldbook.service';
import {TrackerDefinitionService} from '../../services/tracker-definition.service';
import {LlmPresetResponseDto} from '../../models/llm-preset.model';
import {WorldbookResponseDto} from '../../models/worldbook.model';
import {TrackerDefinitionResponseDto} from '../../models/tracker-definition.model';
import {AGENT_NAMES} from '../../models/adventure-settings.model';

@Component({
  selector: 'app-adventure-create',
  standalone: false,
  templateUrl: './adventure-create.component.html',
  styleUrl: './adventure-create.component.css'
})
export class AdventureCreateComponent implements OnInit, OnDestroy {
  adventureForm!: FormGroup;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  // Dropdown data
  llmPresets: LlmPresetResponseDto[] = [];
  worldbooks: WorldbookResponseDto[] = [];
  trackerDefinitions: TrackerDefinitionResponseDto[] = [];

  // Agent names for configuration
  agentNames = AGENT_NAMES;

  // Default prompt path from server
  defaultPromptPath = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private adventureService: AdventureService,
    private llmPresetService: LlmPresetService,
    private worldbookService: WorldbookService,
    private trackerDefinitionService: TrackerDefinitionService,
    private router: Router
  ) {
  }

  get characterGroup(): FormGroup {
    return this.adventureForm.get('character') as FormGroup;
  }

  ngOnInit(): void {
    this.initializeForm();
    this.loadDropdownData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(): void {
    if (this.adventureForm.invalid) {
      this.markFormGroupTouched(this.adventureForm);
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.adventureForm.value;

    // Build agent LLM presets array from form
    const agentLlmPresets: AdventureAgentLlmPresetDto[] = this.agentNames.map(agentName => ({
      agentName: agentName,
      llmPresetId: formValue.agentPresets[agentName]
    }));

    const adventureDto: AdventureDto = {
      name: formValue.name,
      firstSceneDescription: formValue.firstSceneDescription,
      referenceTime: formValue.referenceTime,
      authorNotes: formValue.authorNotes,
      character: {
        name: formValue.character.name,
        description: formValue.character.description
      },
      worldbookId: formValue.worldbookId || null,
      trackerDefinitionId: formValue.trackerDefinitionId,
      promptPath: formValue.promptPath,
      agentLlmPresets: agentLlmPresets
    };

    this.adventureService.createAdventure(adventureDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (status) => {
          this.router.navigate(['/adventures/status', status.adventureId]);
        },
        error: (error) => {
          console.error('Error creating adventure:', error);
          this.errorMessage = error.error?.message || 'Failed to create adventure. Please try again.';
          this.isSubmitting = false;
        }
      });
  }

  isFieldInvalid(fieldName: string, parentGroup?: FormGroup): boolean {
    const group = parentGroup || this.adventureForm;
    const field = group.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string, parentGroup?: FormGroup): string {
    const group = parentGroup || this.adventureForm;
    const field = group.get(fieldName);

    if (field?.errors) {
      if (field.errors['required']) return 'This field is required';
      if (field.errors['minlength']) {
        return `Minimum length is ${field.errors['minlength'].requiredLength} characters`;
      }
    }
    return '';
  }

  private initializeForm(): void {
    // Build agent presets form group dynamically
    const agentPresetsGroup: { [key: string]: any } = {};
    for (const agentName of this.agentNames) {
      agentPresetsGroup[agentName] = ['', Validators.required];
    }

    this.adventureForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      firstSceneDescription: ['', Validators.required],
      referenceTime: ['', Validators.required],
      authorNotes: ['', Validators.required],
      character: this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        description: ['', Validators.required]
      }),
      worldbookId: [null],
      trackerDefinitionId: ['', Validators.required],
      promptPath: ['', Validators.required],
      agentPresets: this.fb.group(agentPresetsGroup)
    });
  }

  get agentPresetsGroup(): FormGroup {
    return this.adventureForm.get('agentPresets') as FormGroup;
  }

  applyPresetToAll(presetId: string): void {
    if (!presetId) return;

    const patchValue: { [key: string]: string } = {};
    for (const agentName of this.agentNames) {
      patchValue[agentName] = presetId;
    }
    this.agentPresetsGroup.patchValue(patchValue);
  }

  formatAgentName(agentName: string): string {
    return agentName
      .replace(/Agent$/, '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  private loadDropdownData(): void {
    this.isLoading = true;

    forkJoin({
      presets: this.llmPresetService.getAllPresets(),
      worldbooks: this.worldbookService.getAllWorldbooks(),
      trackerDefinitions: this.trackerDefinitionService.getAllDefinitions(),
      defaults: this.adventureService.getDefaults()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.llmPresets = result.presets;
          this.worldbooks = result.worldbooks;
          this.trackerDefinitions = result.trackerDefinitions;
          this.defaultPromptPath = result.defaults.defaultPromptPath;

          // Set default prompt path in form
          this.adventureForm.patchValue({
            promptPath: this.defaultPromptPath
          });

          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading dropdown data:', error);
          this.errorMessage = 'Failed to load settings. Please try again.';
          this.isLoading = false;
        }
      });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
}
