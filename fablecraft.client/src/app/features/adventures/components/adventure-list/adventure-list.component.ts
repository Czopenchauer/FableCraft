import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AdventureListItemDto } from '../../models/adventure.model';
import { AdventureService } from '../../services/adventure.service';

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

  openAdventure(adventure: AdventureListItemDto): void {
    this.router.navigate(['/adventures', adventure.adventureId]);
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

  goBack(): void {
    this.router.navigate(['/']);
  }
}
