import {Component, EventEmitter, Input, Output} from '@angular/core';

@Component({
  selector: 'app-delete-confirmation-modal',
  standalone: false,
  templateUrl: './delete-confirmation-modal.component.html',
  styleUrl: './delete-confirmation-modal.component.css'
})
export class DeleteConfirmationModalComponent {
  @Input() isOpen = false;
  @Input() adventureName = '';
  @Input() isDeleting = false;
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm(): void {
    if (!this.isDeleting) {
      this.confirm.emit();
    }
  }

  onCancel(): void {
    if (!this.isDeleting) {
      this.cancel.emit();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isDeleting) {
      this.cancel.emit();
    }
  }
}
