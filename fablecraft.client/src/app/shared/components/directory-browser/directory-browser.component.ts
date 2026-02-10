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

  constructor(private adventureService: AdventureService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentPath'] && this.isOpen && this.currentPath) {
      this.loadDirectories(this.currentPath);
    }
  }

  toggleDropdown(): void {
    if (this.disabled) return;

    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      // If no current path, load without path to get the root
      const startPath = this.currentPath || this.basePath || '';
      this.loadDirectories(startPath);
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  loadDirectories(path: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Pass empty string or path - backend will default to root if empty
    this.adventureService.getPromptDirectories(path || undefined).subscribe({
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

  /**
   * Gets the display path relative to the root path.
   * If at root, shows "Prompts Root". Otherwise shows the relative path.
   */
  getDisplayPath(): string {
    if (!this.currentPath) return 'Select directory...';

    const rootPath = this.listing?.rootPath || '';
    const relativePath = this.getRelativePath(this.currentPath, rootPath);

    return relativePath || 'Prompts Root';
  }

  /**
   * Gets path parts for breadcrumb, starting after the root path.
   * Returns parts that are relative to the prompts root.
   */
  getPathParts(): { name: string; path: string }[] {
    if (!this.listing?.currentPath || !this.listing?.rootPath) return [];

    const rootPath = this.listing.rootPath.replace(/\\/g, '/').replace(/\/$/, '');
    const fullPath = this.listing.currentPath.replace(/\\/g, '/');

    // Get the relative path after the root
    const relativePath = this.getRelativePath(fullPath, rootPath);
    if (!relativePath) {
      // At root level - return empty array (no breadcrumb parts to show)
      return [];
    }

    const parts = relativePath.split('/').filter(p => p);
    const result: { name: string; path: string }[] = [];

    // Build accumulated paths starting from root
    let accumulated = rootPath;
    for (const part of parts) {
      accumulated += '/' + part;
      result.push({name: part, path: accumulated});
    }

    return result;
  }

  /**
   * Gets the relative path by removing the root prefix.
   */
  private getRelativePath(fullPath: string, rootPath: string): string {
    const normalizedFull = fullPath.replace(/\\/g, '/').replace(/\/$/, '');
    const normalizedRoot = rootPath.replace(/\\/g, '/').replace(/\/$/, '');

    if (normalizedFull === normalizedRoot) {
      return '';
    }

    if (normalizedFull.startsWith(normalizedRoot + '/')) {
      return normalizedFull.substring(normalizedRoot.length + 1);
    }

    // Fallback: return the last segment
    const parts = normalizedFull.split('/');
    return parts[parts.length - 1] || '';
  }

  navigateToPath(path: string): void {
    this.loadDirectories(path);
  }

  /**
   * Checks if currently at the root level (cannot navigate up further).
   */
  isAtRoot(): boolean {
    return !this.listing?.parentPath;
  }
}
