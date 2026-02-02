import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormArray, FormBuilder, FormGroup, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {AdventureService} from '../../services/adventure.service';
import {AdventureAgentLlmPresetDto, AdventureDto, CustomCharacterDto, CustomRelationshipDto, ExtraLoreEntryDto} from '../../models/adventure.model';
import {forkJoin, Subject, takeUntil} from 'rxjs';
import {LlmPresetService} from '../../services/llm-preset.service';
import {WorldbookService} from '../../services/worldbook.service';
import {TrackerDefinitionService} from '../../services/tracker-definition.service';
import {LlmPresetResponseDto} from '../../models/llm-preset.model';
import {WorldbookResponseDto} from '../../models/worldbook.model';
import {TrackerDefinitionResponseDto} from '../../models/tracker-definition.model';
import {GraphRagSettingsService} from '../../../settings/services/graph-rag-settings.service';
import {GraphRagSettingsSummaryDto} from '../../../settings/models/graph-rag-settings.model';

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
  graphRagSettingsOptions: GraphRagSettingsSummaryDto[] = [];

  // Agent names from backend
  agentNames: string[] = [];

  // Default prompt path from server
  defaultPromptPath = '';

  // Current selected prompt path
  currentPromptPath = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private adventureService: AdventureService,
    private llmPresetService: LlmPresetService,
    private worldbookService: WorldbookService,
    private trackerDefinitionService: TrackerDefinitionService,
    private graphRagSettingsService: GraphRagSettingsService,
    private router: Router
  ) {
  }

  get mainCharacterGroup(): FormGroup {
    return this.adventureForm.get('mainCharacter') as FormGroup;
  }

  get agentPresetsGroup(): FormGroup {
    return this.adventureForm.get('agentPresets') as FormGroup;
  }

  get extraLoreArray(): FormArray {
    return this.adventureForm.get('extraLoreEntries') as FormArray;
  }

  get customCharactersArray(): FormArray {
    return this.adventureForm.get('customCharacters') as FormArray;
  }

  ngOnInit(): void {
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

    // Build extra lore entries array from form
    const extraLoreEntries: ExtraLoreEntryDto[] = formValue.extraLoreEntries || [];

    // Build custom characters array from form
    const customCharacters: CustomCharacterDto[] = (formValue.customCharacters || []).map((char: any) => ({
      name: char.name,
      description: char.description,
      importance: char.importance,
      characterStats: this.parseJsonSafe(char.characterStatsJson, {name: char.name, motivations: null, routine: null}),
      characterTracker: this.parseJsonSafe(char.characterTrackerJson, {name: char.name, location: ''}),
      initialRelationships: (char.relationships || []).map((rel: any) => ({
        targetCharacterName: rel.targetCharacterName,
        dynamic: rel.dynamic,
        data: this.parseJsonSafe(rel.dataJson, {})
      }))
    }));

    // Parse initial MC tracker if provided
    const initialTracker = formValue.mainCharacter.initialTrackerJson
      ? this.parseJsonSafe(formValue.mainCharacter.initialTrackerJson, null)
      : null;

    const adventureDto: AdventureDto = {
      name: formValue.name,
      firstSceneDescription: formValue.firstSceneDescription,
      referenceTime: formValue.referenceTime,
      mainCharacter: {
        name: formValue.mainCharacter.name,
        description: formValue.mainCharacter.description,
        initialTracker: initialTracker
      },
      worldbookId: formValue.worldbookId || null,
      graphRagSettingsId: formValue.graphRagSettingsId || null,
      trackerDefinitionId: formValue.trackerDefinitionId,
      promptPath: formValue.promptPath,
      agentLlmPresets: agentLlmPresets,
      extraLoreEntries: extraLoreEntries,
      customCharacters: customCharacters.length > 0 ? customCharacters : undefined
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

  onPromptPathSelected(path: string): void {
    this.currentPromptPath = path;
    this.adventureForm.patchValue({
      promptPath: path
    });
  }

  addExtraLoreEntry(): void {
    const loreGroup = this.fb.group({
      title: ['', Validators.required],
      content: ['', Validators.required],
      category: ['Lore', Validators.required]
    });
    this.extraLoreArray.push(loreGroup);
  }

  removeExtraLoreEntry(index: number): void {
    this.extraLoreArray.removeAt(index);
  }

  // Custom Characters management
  addCustomCharacter(): void {
    const characterGroup = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      importance: ['significant', Validators.required],
      characterStatsJson: ['{\n  "name": "",\n  "motivations": null,\n  "routine": null\n}'],
      characterTrackerJson: ['{\n  "name": "",\n  "location": ""\n}'],
      relationships: this.fb.array([])
    });
    this.customCharactersArray.push(characterGroup);
  }

  removeCustomCharacter(index: number): void {
    this.customCharactersArray.removeAt(index);
  }

  getCharacterRelationships(characterIndex: number): FormArray {
    return this.customCharactersArray.at(characterIndex).get('relationships') as FormArray;
  }

  addRelationship(characterIndex: number): void {
    const relationshipGroup = this.fb.group({
      targetCharacterName: ['', Validators.required],
      dynamic: ['', Validators.required],
      dataJson: ['{}']
    });
    this.getCharacterRelationships(characterIndex).push(relationshipGroup);
  }

  removeRelationship(characterIndex: number, relationshipIndex: number): void {
    this.getCharacterRelationships(characterIndex).removeAt(relationshipIndex);
  }

  private parseJsonSafe(jsonString: string, defaultValue: any): any {
    if (!jsonString || jsonString.trim() === '') {
      return defaultValue;
    }
    try {
      return JSON.parse(jsonString);
    } catch (e) {
      console.warn('Failed to parse JSON:', e);
      return defaultValue;
    }
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
      mainCharacter: this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        description: ['', Validators.required],
        initialTrackerJson: [''] // Optional JSON for initial MC tracker
      }),
      worldbookId: [null],
      graphRagSettingsId: [null],
      trackerDefinitionId: ['', Validators.required],
      promptPath: ['', Validators.required],
      agentPresets: this.fb.group(agentPresetsGroup),
      extraLoreEntries: this.fb.array([]),
      customCharacters: this.fb.array([])
    });

    // Watch worldbook changes to auto-populate GraphRAG settings
    this.adventureForm.get('worldbookId')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(worldbookId => {
        this.onWorldbookChanged(worldbookId);
      });
  }

  /**
   * When worldbook selection changes, auto-populate GraphRAG settings from worldbook if available
   */
  private onWorldbookChanged(worldbookId: string | null): void {
    if (!worldbookId) {
      return;
    }

    const selectedWorldbook = this.worldbooks.find(wb => wb.id === worldbookId);
    if (selectedWorldbook?.graphRagSettingsId) {
      this.adventureForm.patchValue({
        graphRagSettingsId: selectedWorldbook.graphRagSettingsId
      });
    }
  }

  private loadDropdownData(): void {
    this.isLoading = true;

    forkJoin({
      presets: this.llmPresetService.getAllPresets(),
      worldbooks: this.worldbookService.getAllWorldbooks(),
      trackerDefinitions: this.trackerDefinitionService.getAllDefinitions(),
      defaults: this.adventureService.getDefaults(),
      graphRagSettings: this.graphRagSettingsService.getAllSummary()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.llmPresets = result.presets;
          this.worldbooks = result.worldbooks;
          this.trackerDefinitions = result.trackerDefinitions;
          this.graphRagSettingsOptions = result.graphRagSettings;
          this.defaultPromptPath = result.defaults.defaultPromptPath;
          this.currentPromptPath = result.defaults.defaultPromptPath;
          this.agentNames = result.defaults.availableAgents;

          // Initialize form after we know the agents
          this.initializeForm();

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
