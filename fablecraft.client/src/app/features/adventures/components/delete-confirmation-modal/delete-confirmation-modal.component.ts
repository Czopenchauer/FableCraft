import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';

@Component({
  selector: 'app-delete-confirmation-modal',
  standalone: false,
  templateUrl: './delete-confirmation-modal.component.html',
  styleUrl: './delete-confirmation-modal.component.css'
})
export class DeleteConfirmationModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() adventureName = '';
  @Input() isDeleting = false;
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

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
