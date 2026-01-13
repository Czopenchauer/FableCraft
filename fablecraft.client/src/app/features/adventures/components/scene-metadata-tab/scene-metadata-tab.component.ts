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
    return !!(this.metadata?.chroniclerState?.worldMomentum ||
      this.metadata?.chroniclerState?.additionalData);
  }

  get hasWriterGuidance(): boolean {
    return !!this.metadata?.writerGuidance;
  }

  get hasAnyMetadata(): boolean {
    return this.hasResolution || this.hasGatheredContext ||
      this.hasWriterObservation || this.hasChroniclerState || this.hasWriterGuidance;
  }

  get parsedResolutionOutput(): any {
    if (!this.metadata?.resolutionOutput) return null;
    try {
      return JSON.parse(this.metadata.resolutionOutput);
    } catch {
      return this.metadata.resolutionOutput;
    }
  }

  get isResolutionString(): boolean {
    return typeof this.parsedResolutionOutput === 'string';
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
