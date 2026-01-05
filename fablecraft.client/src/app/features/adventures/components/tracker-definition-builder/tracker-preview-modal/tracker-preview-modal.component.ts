import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JsonRendererComponent } from '../../json-renderer/json-renderer.component';

@Component({
  selector: 'app-tracker-preview-modal',
  standalone: true,
  imports: [CommonModule, JsonRendererComponent],
  templateUrl: './tracker-preview-modal.component.html',
  styleUrls: ['./tracker-preview-modal.component.css']
})
export class TrackerPreviewModalComponent {
  @Input() isOpen = false;
  @Input() visualizationData: any = null;
  @Input() isLoading = false;
  @Output() close = new EventEmitter<void>();
  @Output() refresh = new EventEmitter<void>();

  onClose(): void {
    if (!this.isLoading) {
      this.close.emit();
    }
  }

  onRefresh(): void {
    this.refresh.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isLoading) {
      this.close.emit();
    }
  }
}
