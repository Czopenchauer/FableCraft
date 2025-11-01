import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Adventure, AdventureStatus } from '../../models/adventure.model';
import { AdventureService } from '../../services/adventure.service';

@Component({
  selector: 'app-adventure-list',
  standalone: false,
  templateUrl: './adventure-list.component.html',
  styleUrl: './adventure-list.component.css'
})
export class AdventureListComponent implements OnInit {
  adventures: Adventure[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private adventureService: AdventureService,
    private router: Router
  ) {}

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

  openAdventure(adventure: Adventure): void {
    this.router.navigate(['/adventures', adventure.id]);
  }

  deleteAdventure(event: Event, adventureId: string): void {
    event.stopPropagation();

    if (confirm('Are you sure you want to delete this adventure?')) {
      this.adventureService.deleteAdventure(adventureId).subscribe({
        next: () => {
          this.loadAdventures();
        },
        error: (err) => {
          console.error('Error deleting adventure:', err);
          alert('Failed to delete adventure');
        }
      });
    }
  }

  getStatusClass(status: AdventureStatus): string {
    switch (status) {
      case AdventureStatus.Ready:
        return 'status-ready';
      case AdventureStatus.Creating:
      case AdventureStatus.Processing:
        return 'status-processing';
      case AdventureStatus.Failed:
        return 'status-failed';
      default:
        return 'status-default';
    }
  }

  getStatusIcon(status: AdventureStatus): string {
    switch (status) {
      case AdventureStatus.Ready:
        return 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z';
      case AdventureStatus.Creating:
      case AdventureStatus.Processing:
        return 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15';
      case AdventureStatus.Failed:
        return 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
      default:
        return 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
    }
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
