import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';

export type AdventureStateTab = 'lore' | 'characters';

@Component({
  selector: 'app-adventure-state-modal',
  standalone: false,
  templateUrl: './adventure-state-modal.component.html',
  styleUrl: './adventure-state-modal.component.css'
})
export class AdventureStateModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Output() close = new EventEmitter<void>();

  activeTab: AdventureStateTab = 'lore';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && !this.isOpen) {
      // Reset to lore tab when modal closes
      this.activeTab = 'lore';
    }
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
