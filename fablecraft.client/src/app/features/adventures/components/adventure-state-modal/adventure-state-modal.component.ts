import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';
import {SceneMetadataDto} from '../../models/adventure.model';

export type AdventureStateTab = 'lore' | 'characters' | 'metadata' | 'llm-logs';

@Component({
  selector: 'app-adventure-state-modal',
  standalone: false,
  templateUrl: './adventure-state-modal.component.html',
  styleUrl: './adventure-state-modal.component.css'
})
export class AdventureStateModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Input() sceneId: string | null = null;
  @Input() sceneMetadata: SceneMetadataDto | null = null;
  @Output() close = new EventEmitter<void>();

  activeTab: AdventureStateTab = 'lore';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        document.body.style.overflow = 'hidden';
      } else {
        document.body.style.overflow = '';
        this.activeTab = 'lore';
      }
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  setActiveTab(tab: AdventureStateTab): void {
    this.activeTab = tab;
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
