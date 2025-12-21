import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {AdventureService} from '../../../features/adventures/services/adventure.service';
import {DirectoryEntryDto, DirectoryListingDto} from '../../../features/adventures/models/adventure.model';

@Component({
  selector: 'app-directory-browser',
  standalone: false,
  templateUrl: './directory-browser.component.html',
  styleUrl: './directory-browser.component.css'
})
export class DirectoryBrowserComponent implements OnChanges {
  @Input() currentPath = '';
  @Input() basePath = '';
  @Input() disabled = false;
  @Output() pathSelected = new EventEmitter<string>();

  isOpen = false;
  isLoading = false;
  listing: DirectoryListingDto | null = null;
  errorMessage = '';

  constructor(private adventureService: AdventureService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentPath'] && this.isOpen && this.currentPath) {
      this.loadDirectories(this.currentPath);
    }
  }

  toggleDropdown(): void {
    if (this.disabled) return;

    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      const startPath = this.currentPath || this.basePath;
      if (startPath) {
        this.loadDirectories(startPath);
      }
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  loadDirectories(path: string): void {
    if (!path) {
      this.listing = null;
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.adventureService.getPromptDirectories(path).subscribe({
      next: (result) => {
        this.listing = result;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading directories:', err);
        this.errorMessage = 'Failed to load directories';
        this.isLoading = false;
      }
    });
  }

  navigateTo(directory: DirectoryEntryDto): void {
    this.loadDirectories(directory.fullPath);
  }

  navigateUp(): void {
    if (this.listing?.parentPath) {
      this.loadDirectories(this.listing.parentPath);
    }
  }

  selectCurrentPath(): void {
    if (this.listing?.currentPath) {
      this.pathSelected.emit(this.listing.currentPath);
      this.closeDropdown();
    }
  }

  selectDirectory(directory: DirectoryEntryDto): void {
    this.pathSelected.emit(directory.fullPath);
    this.closeDropdown();
  }

  getDisplayPath(): string {
    if (!this.currentPath) return 'Select directory...';
    // Show only the last part of the path for display
    const parts = this.currentPath.replace(/\\/g, '/').split('/');
    return parts[parts.length - 1] || this.currentPath;
  }

  getPathParts(): { name: string; path: string }[] {
    if (!this.listing?.currentPath) return [];
    const fullPath = this.listing.currentPath.replace(/\\/g, '/');
    const parts = fullPath.split('/').filter(p => p);
    const result: { name: string; path: string }[] = [];

    let accumulated = '';
    for (const part of parts) {
      accumulated += (accumulated ? '/' : '') + part;
      result.push({ name: part, path: accumulated });
    }

    return result;
  }

  navigateToPath(path: string): void {
    this.loadDirectories(path);
  }
}
