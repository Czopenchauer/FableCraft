import {Component, Input} from '@angular/core';
import {SceneMetadataDto} from '../../models/adventure.model';

type MetadataSection = 'resolution' | 'context' | 'observation' | 'chronicler' | 'guidance';

@Component({
  selector: 'app-scene-metadata-tab',
  standalone: false,
  templateUrl: './scene-metadata-tab.component.html',
  styleUrl: './scene-metadata-tab.component.css'
})
export class SceneMetadataTabComponent {
  @Input() metadata: SceneMetadataDto | null = null;
  @Input() isActive = false;

  activeSection: MetadataSection = 'resolution';

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
    // Check for any of the chronicler state properties (snake_case from backend)
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

  get hasAnyMetadata(): boolean {
    return this.hasResolution || this.hasGatheredContext ||
      this.hasWriterObservation || this.hasChroniclerState || this.hasWriterGuidance;
  }

  get formattedResolutionOutput(): string {
    if (!this.metadata?.resolutionOutput) return '';
    try {
      const parsed = JSON.parse(this.metadata.resolutionOutput);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return this.metadata.resolutionOutput;
    }
  }

  get parsedWriterGuidance(): any {
    if (!this.metadata?.writerGuidance) return null;
    try {
      return JSON.parse(this.metadata.writerGuidance);
    } catch {
      return this.metadata.writerGuidance;
    }
  }

  setActiveSection(section: MetadataSection): void {
    this.activeSection = section;
  }

  isActiveSection(section: MetadataSection): boolean {
    return this.activeSection === section;
  }
}
