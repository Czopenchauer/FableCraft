import {Component, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {AdventureListItemDto} from '../../models/adventure.model';
import {AdventureService} from '../../services/adventure.service';

@Component({
  selector: 'app-adventure-list',
  standalone: false,
  templateUrl: './adventure-list.component.html',
  styleUrl: './adventure-list.component.css'
})
export class AdventureListComponent implements OnInit {
  adventures: AdventureListItemDto[] = [];
  loading = false;
  error: string | null = null;

  // Delete modal state
  showDeleteModal = false;
  adventureToDelete: { id: string; name: string } | null = null;
  isDeleting = false;

  // Settings modal state
  showSettingsModal = false;
  settingsAdventureId: string | null = null;

  // Lore modal state
  showLoreModal = false;
  loreAdventureId: string | null = null;

  constructor(
    private adventureService: AdventureService,
    private router: Router
  ) {
  }

  ngOnInit(): void {
    this.loadAdventures();
  }

  loadAdventures(): void {
    this.loading = true;
    this.error = null;

    this.adventureService.getAllAdventures().subscribe({
      next: (adventures) => {
        this.adventures = adventures;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading adventures:', err);
        this.error = 'Failed to load adventures. The backend endpoint may not be implemented yet.';
        this.loading = false;
        // For now, show empty state
        this.adventures = [];
      }
    });
  }

  createNewAdventure(): void {
    this.router.navigate(['/adventures/create']);
  }

  openAdventure(adventure: AdventureListItemDto): void {
    // Navigate to status screen which will:
    // - Auto-redirect to game panel if all statuses are completed
    // - Show retry option if any status failed
    // - Display progress if still processing
    this.router.navigate(['/adventures/status', adventure.adventureId]);
  }

  deleteAdventure(event: Event, adventureId: string, adventureName: string): void {
    event.stopPropagation();
    this.adventureToDelete = {id: adventureId, name: adventureName};
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    if (!this.adventureToDelete) return;

    this.isDeleting = true;

    this.adventureService.deleteAdventure(this.adventureToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteModal = false;
        this.adventureToDelete = null;
        this.loadAdventures();
      },
      error: (err) => {
        console.error('Error deleting adventure:', err);
        this.isDeleting = false;
        // Keep modal open to show error state
        alert('Failed to delete adventure. Please try again.');
      }
    });
  }

  cancelDelete(): void {
    if (!this.isDeleting) {
      this.showDeleteModal = false;
      this.adventureToDelete = null;
    }
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  openSettings(event: Event, adventureId: string): void {
    event.stopPropagation();
    this.settingsAdventureId = adventureId;
    this.showSettingsModal = true;
  }

  closeSettingsModal(): void {
    this.showSettingsModal = false;
    this.settingsAdventureId = null;
  }

  onSettingsSaved(): void {
    this.closeSettingsModal();
  }

  openLore(event: Event, adventureId: string): void {
    event.stopPropagation();
    this.loreAdventureId = adventureId;
    this.showLoreModal = true;
  }

  closeLoreModal(): void {
    this.showLoreModal = false;
    this.loreAdventureId = null;
  }
}
