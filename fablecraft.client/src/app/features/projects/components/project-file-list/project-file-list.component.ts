import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Subject} from 'rxjs';
import {ProjectFileSummaryDto, ProjectFileDto} from '../../models/project.model';

@Component({
  selector: 'app-project-file-list',
  standalone: false,
  templateUrl: './project-file-list.component.html',
  styleUrl: './project-file-list.component.css'
})
export class ProjectFileListComponent implements OnInit, OnDestroy {
  @Input() files: ProjectFileSummaryDto[] = [];
  @Input() selectedFileId: string | null = null;
  @Input() isLoading = false;
  @Output() fileSelected = new EventEmitter<ProjectFileSummaryDto>();
  @Output() fileCreated = new EventEmitter<ProjectFileDto>();
  @Output() fileDeleted = new EventEmitter<string>();

  activeCategory: string | null = null;
  showNewFileForm = false;
  newFileName = '';
  newFileCategory = '';
  newFileContent = '';
  deletingFileId: string | null = null;

  private destroy$ = new Subject<void>();

  ngOnInit(): void {
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get uniqueCategories(): string[] {
    const categories = new Set<string>();
    this.files.forEach(f => {
      if (f.category) categories.add(f.category);
    });
    return Array.from(categories).sort();
  }

  get filteredFiles(): ProjectFileSummaryDto[] {
    if (!this.activeCategory) return this.files;
    return this.files.filter(f => f.category === this.activeCategory);
  }

  get filesByCategoryCount(): { category: string; count: number }[] {
    const counts = new Map<string, number>();
    this.files.forEach(f => {
      const cat = f.category || 'Uncategorized';
      counts.set(cat, (counts.get(cat) || 0) + 1);
    });
    return Array.from(counts.entries()).map(([category, count]) => ({category, count}));
  }

  setCategoryFilter(category: string | null): void {
    this.activeCategory = category;
  }

  selectFile(file: ProjectFileSummaryDto): void {
    this.fileSelected.emit(file);
  }

  toggleNewFileForm(): void {
    this.showNewFileForm = !this.showNewFileForm;
    if (this.showNewFileForm) {
      this.newFileName = '';
      this.newFileCategory = '';
      this.newFileContent = '';
    }
  }

  cancelNewFile(): void {
    this.showNewFileForm = false;
  }

  createFile(): void {
    if (!this.newFileName.trim()) return;

    const dto: ProjectFileDto = {
      name: this.newFileName.trim(),
      content: this.newFileContent,
      category: this.newFileCategory.trim() || null
    };

    this.fileCreated.emit(dto);
    this.showNewFileForm = false;
    this.newFileName = '';
    this.newFileCategory = '';
    this.newFileContent = '';
  }

  onDeleteClick(fileId: string, event: Event): void {
    event.stopPropagation();
    this.deletingFileId = fileId;
  }

  confirmDelete(fileId: string): void {
    this.deletingFileId = null;
    this.fileDeleted.emit(fileId);
  }

  cancelDelete(): void {
    this.deletingFileId = null;
  }

  relativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    const diffHr = Math.floor(diffMs / 3600000);
    const diffDay = Math.floor(diffMs / 86400000);

    if (diffMin < 1) return 'just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    if (diffHr < 24) return `${diffHr}h ago`;
    if (diffDay < 30) return `${diffDay}d ago`;
    return date.toLocaleDateString();
  }
}