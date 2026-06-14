import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Subject} from 'rxjs';
import {ProjectFileResponseDto, ProjectFileUpdateDto} from '../../models/project.model';

@Component({
  selector: 'app-project-file-editor',
  standalone: false,
  templateUrl: './project-file-editor.component.html',
  styleUrl: './project-file-editor.component.css'
})
export class ProjectFileEditorComponent implements OnInit, OnDestroy {
  @Input() file: ProjectFileResponseDto | null = null;
  @Input() categories: string[] = [];
  @Output() fileSaved = new EventEmitter<{ fileId: string; dto: ProjectFileUpdateDto }>();
  @Output() fileDeleted = new EventEmitter<string>();
  @Output() fileClosed = new EventEmitter<void>();

  editContent = '';
  editCategory = '';
  isDirty = false;
  showDeleteConfirm = false;

  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    if (this.file) {
      this.editContent = this.file.content;
      this.editCategory = this.file.category || '';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnChanges(): void {
    if (this.file) {
      this.editContent = this.file.content;
      this.editCategory = this.file.category || '';
      this.isDirty = false;
      this.showDeleteConfirm = false;
    }
  }

  onContentChange(): void {
    this.checkDirty();
  }

  onCategoryChange(): void {
    this.checkDirty();
  }

  save(): void {
    if (!this.file || !this.isDirty) return;

    const dto: ProjectFileUpdateDto = {
      content: this.editContent,
      category: this.editCategory.trim() || null
    };

    this.fileSaved.emit({fileId: this.file.id, dto});
    this.isDirty = false;
  }

  deleteFile(): void {
    if (!this.file) return;
    this.showDeleteConfirm = true;
  }

  confirmDelete(): void {
    if (!this.file) return;
    this.fileDeleted.emit(this.file.id);
    this.showDeleteConfirm = false;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
  }

  close(): void {
    if (this.isDirty) {
      if (!confirm('You have unsaved changes. Are you sure you want to close?')) {
        return;
      }
    }
    this.fileClosed.emit();
  }

  private checkDirty(): void {
    if (!this.file) return;
    this.isDirty = this.editContent !== this.file.content ||
      this.editCategory !== (this.file.category || '');
  }
}