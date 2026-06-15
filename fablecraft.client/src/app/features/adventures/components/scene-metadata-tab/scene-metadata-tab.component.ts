import {Component, EventEmitter, Input, Output} from '@angular/core';
import {SceneMetadataDto} from '../../models/adventure.model';
import {AdventureService} from '../../services/adventure.service';

type MetadataSection =
  | 'resolution' | 'context' | 'observation' | 'chronicler' | 'guidance'
  | 'catalystAssessment' | 'catalystGoals' | 'catalystEvent' | 'mcSummary';

@Component({
  selector: 'app-scene-metadata-tab',
  standalone: false,
  templateUrl: './scene-metadata-tab.component.html',
  styleUrl: './scene-metadata-tab.component.css'
})
export class SceneMetadataTabComponent {
  @Input() metadata: SceneMetadataDto | null = null;
  @Input() isActive = false;
  @Input() adventureId: string | null = null;
  @Input() sceneId: string | null = null;
  @Output() metadataUpdated = new EventEmitter<SceneMetadataDto>();

  activeSection: MetadataSection = 'resolution';
  editText = '';
  originalText = '';
  isDirty = false;
  isSaving = false;
  jsonError: string | null = null;

  private readonly jsonSections: Set<MetadataSection> = new Set(['context', 'observation', 'chronicler']);

  get hasResolution(): boolean {
    return !!this.metadata?.resolutionOutput;
  }

  get hasGatheredContext(): boolean {
    return !!(this.metadata?.gatheredContext?.worldContext?.length ||
      this.metadata?.gatheredContext?.narrativeContext?.length);
  }

  get hasWriterObservation(): boolean {
    return !!this.metadata?.writerObservation &&
      Object.keys(this.metadata.writerObservation).length > 0;
  }

  get hasChroniclerState(): boolean {
    const state = this.metadata?.chroniclerState;
    if (!state) return false;
    return !!(state.world_momentum?.length ||
      state.dramatic_questions?.length ||
      state.promises?.length ||
      state.active_threads?.length ||
      state.stakes?.length ||
      state.windows?.length);
  }

  get hasWriterGuidance(): boolean {
    return !!this.metadata?.writerGuidance;
  }

  get hasCatalystAssessment(): boolean {
    return !!this.metadata?.catalystStoryAssessment;
  }

  get hasCatalystGoals(): boolean {
    return !!this.metadata?.catalystGoals;
  }

  get hasCatalystEvent(): boolean {
    return !!this.metadata?.catalystRandomEvent;
  }

  get hasMcSummary(): boolean {
    return !!this.metadata?.mcStorySummary;
  }

  get hasAnyMetadata(): boolean {
    return this.hasResolution || this.hasGatheredContext ||
      this.hasWriterObservation || this.hasChroniclerState || this.hasWriterGuidance ||
      this.hasCatalystAssessment || this.hasCatalystGoals ||
      this.hasCatalystEvent || this.hasMcSummary;
  }

  get sectionDescriptions(): Record<MetadataSection, string> {
    return {
      resolution: 'Action resolution from the ResolutionAgent determining the outcome of player actions.',
      context: 'Context retrieved from the knowledge graph to inform scene generation.',
      observation: 'Extra context and observations from the writer to be used in future scene generation.',
      chronicler: 'Story state tracking narrative elements: dramatic questions, promises, threads, stakes, and world momentum.',
      guidance: 'Guidance from the Chronicler to the Writer for the next scene generation.',
      catalystAssessment: 'Narrative Catalyst story assessment for the current scene.',
      catalystGoals: 'Narrative Catalyst goals for the story.',
      catalystEvent: 'Narrative Catalyst random event for the current scene.',
      mcSummary: 'Rolling story summary for the Main Character up to this scene.'
    };
  }

  get sectionTitles(): Record<MetadataSection, string> {
    return {
      resolution: 'Resolution Output',
      context: 'Gathered Context (RAG)',
      observation: 'Writer Observation',
      chronicler: 'Chronicler State',
      guidance: 'Writer Guidance',
      catalystAssessment: 'Catalyst Story Assessment',
      catalystGoals: 'Catalyst Goals',
      catalystEvent: 'Catalyst Random Event',
      mcSummary: 'MC Story Summary'
    };
  }

  isJsonSection(section: MetadataSection): boolean {
    return this.jsonSections.has(section);
  }

  getSerializedValue(section: MetadataSection): string {
    if (!this.metadata) return '';
    switch (section) {
      case 'resolution':
        return this.metadata.resolutionOutput ?? '';
      case 'context':
        return this.metadata.gatheredContext ? JSON.stringify(this.metadata.gatheredContext, null, 2) : '';
      case 'observation':
        return this.metadata.writerObservation ? JSON.stringify(this.metadata.writerObservation, null, 2) : '';
      case 'chronicler':
        return this.metadata.chroniclerState ? JSON.stringify(this.metadata.chroniclerState, null, 2) : '';
      case 'guidance':
        return this.metadata.writerGuidance ?? '';
      case 'catalystAssessment':
        return this.metadata.catalystStoryAssessment ?? '';
      case 'catalystGoals':
        return this.metadata.catalystGoals ?? '';
      case 'catalystEvent':
        return this.metadata.catalystRandomEvent ?? '';
      case 'mcSummary':
        return this.metadata.mcStorySummary ?? '';
    }
  }

  setActiveSection(section: MetadataSection): void {
    this.activeSection = section;
    this.editText = this.getSerializedValue(section);
    this.originalText = this.editText;
    this.isDirty = false;
    this.jsonError = null;
  }

  onEditTextChange(): void {
    this.isDirty = this.editText !== this.originalText;
    if (this.isJsonSection(this.activeSection)) {
      try {
        JSON.parse(this.editText);
        this.jsonError = null;
      } catch (e: any) {
        this.jsonError = e.message;
      }
    } else {
      this.jsonError = null;
    }
  }

  onSave(): void {
    if (!this.adventureId || !this.sceneId || !this.metadata) return;
    if (this.isJsonSection(this.activeSection) && this.jsonError) return;

    const updatedMetadata = this.buildUpdatedMetadata();
    this.isSaving = true;

    this.adventureService.updateSceneMetadata(this.adventureId, this.sceneId, updatedMetadata)
      .subscribe({
        next: (scene) => {
          if (scene.metadata) {
            this.metadataUpdated.emit(scene.metadata);
            this.originalText = this.editText;
            this.isDirty = false;
          }
          this.isSaving = false;
        },
        error: () => {
          this.isSaving = false;
        }
      });
  }

  onCancel(): void {
    this.editText = this.originalText;
    this.isDirty = false;
    this.jsonError = null;
  }

  private buildUpdatedMetadata(): SceneMetadataDto {
    const metadata = this.metadata!;
    const text = this.editText;
    const section = this.activeSection;

    const result: SceneMetadataDto = {
      resolutionOutput: metadata.resolutionOutput,
      gatheredContext: metadata.gatheredContext,
      writerObservation: metadata.writerObservation,
      chroniclerState: metadata.chroniclerState,
      writerGuidance: metadata.writerGuidance,
      catalystStoryAssessment: metadata.catalystStoryAssessment,
      catalystGoals: metadata.catalystGoals,
      catalystRandomEvent: metadata.catalystRandomEvent,
      mcStorySummary: metadata.mcStorySummary
    };

    switch (section) {
      case 'resolution':
        result.resolutionOutput = text || null;
        break;
      case 'context':
        try { result.gatheredContext = JSON.parse(text); } catch { result.gatheredContext = null; }
        break;
      case 'observation':
        try { result.writerObservation = JSON.parse(text); } catch { result.writerObservation = null; }
        break;
      case 'chronicler':
        try { result.chroniclerState = JSON.parse(text); } catch { result.chroniclerState = null; }
        break;
      case 'guidance':
        result.writerGuidance = text || null;
        break;
      case 'catalystAssessment':
        result.catalystStoryAssessment = text || null;
        break;
      case 'catalystGoals':
        result.catalystGoals = text || null;
        break;
      case 'catalystEvent':
        result.catalystRandomEvent = text || null;
        break;
      case 'mcSummary':
        result.mcStorySummary = text || null;
        break;
    }

    return result;
  }

  canSave(): boolean {
    return this.isDirty && !this.isSaving && !this.jsonError;
  }

  constructor(private adventureService: AdventureService) {}
}