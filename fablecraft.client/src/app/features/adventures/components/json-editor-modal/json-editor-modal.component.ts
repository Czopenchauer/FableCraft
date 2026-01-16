import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';

@Component({
  selector: 'app-json-editor-modal',
  standalone: false,
  templateUrl: './json-editor-modal.component.html',
  styleUrl: './json-editor-modal.component.css'
})
export class JsonEditorModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() title = 'Edit JSON';
  @Input() data: any = null;
  @Output() save = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  jsonText = '';
  hasError = false;
  errorMessage = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
      if (this.isOpen && this.data !== null) {
        this.jsonText = JSON.stringify(this.data, null, 2);
        this.hasError = false;
        this.errorMessage = '';
      }
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  onJsonChange(): void {
    try {
      JSON.parse(this.jsonText);
      this.hasError = false;
      this.errorMessage = '';
    } catch (e: any) {
      this.hasError = true;
      this.errorMessage = e.message || 'Invalid JSON';
    }
  }

  onSave(): void {
    if (this.hasError) return;

    try {
      const parsedData = JSON.parse(this.jsonText);
      this.save.emit(parsedData);
    } catch (e: any) {
      this.hasError = true;
      this.errorMessage = e.message || 'Invalid JSON';
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}
