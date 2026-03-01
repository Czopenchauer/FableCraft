import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges } from '@angular/core';

export type SceneDeleteModalState = 'confirm' | 'force-confirm' | 'retry' | 'deleting' | 'error';

export interface SceneDeleteResult {
  action: 'delete' | 'force-delete' | 'retry' | 'cancel';
}

@Component({
  selector: 'app-scene-delete-modal',
  standalone: false,
  templateUrl: './scene-delete-modal.component.html',
  styleUrl: './scene-delete-modal.component.css'
})
export class SceneDeleteModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() state: SceneDeleteModalState = 'confirm';
  @Input() errorMessage = '';
  @Output() result = new EventEmitter<SceneDeleteResult>();

  get isDeleting(): boolean {
    return this.state === 'deleting';
  }

  get title(): string {
    switch (this.state) {
      case 'confirm':
        return 'Delete Scene';
      case 'force-confirm':
        return 'Delete Committed Scene';
      case 'retry':
        return 'Deletion Incomplete';
      case 'deleting':
        return 'Deleting Scene';
      case 'error':
        return 'Deletion Failed';
      default:
        return 'Delete Scene';
    }
  }

  get message(): string {
    switch (this.state) {
      case 'confirm':
        return 'Are you sure you want to delete the last scene?';
      case 'force-confirm':
        return 'This scene has been committed. Are you sure you want to force delete it?';
      case 'retry':
        return 'The scene was marked for deletion but cleanup of the knowledge graph failed. Would you like to retry?';
      case 'deleting':
        return 'Please wait while the scene is being deleted...';
      case 'error':
        return this.errorMessage || 'An error occurred while deleting the scene.';
      default:
        return '';
    }
  }

  get warningText(): string {
    switch (this.state) {
      case 'confirm':
      case 'force-confirm':
        return 'This action cannot be undone.';
      case 'retry':
        return 'The scene is in a pending deletion state. You can retry now or later.';
      case 'error':
        return 'Please try again or contact support if the issue persists.';
      default:
        return '';
    }
  }

  get iconType(): 'warning' | 'error' | 'loading' {
    switch (this.state) {
      case 'error':
        return 'error';
      case 'deleting':
        return 'loading';
      default:
        return 'warning';
    }
  }

  get primaryButtonText(): string {
    switch (this.state) {
      case 'confirm':
        return 'Delete';
      case 'force-confirm':
        return 'Force Delete';
      case 'retry':
        return 'Retry';
      case 'error':
        return 'Close';
      default:
        return 'Delete';
    }
  }

  get showCancelButton(): boolean {
    return this.state !== 'deleting' && this.state !== 'error';
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  onPrimaryAction(): void {
    if (this.isDeleting) return;

    switch (this.state) {
      case 'confirm':
        this.result.emit({ action: 'delete' });
        break;
      case 'force-confirm':
        this.result.emit({ action: 'force-delete' });
        break;
      case 'retry':
        this.result.emit({ action: 'retry' });
        break;
      case 'error':
        this.result.emit({ action: 'cancel' });
        break;
    }
  }

  onCancel(): void {
    if (!this.isDeleting) {
      this.result.emit({ action: 'cancel' });
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isDeleting) {
      this.result.emit({ action: 'cancel' });
    }
  }
}
