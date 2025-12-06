import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { WorldbookResponseDto } from '../../models/worldbook.model';
import { WorldbookService } from '../../services/worldbook.service';

@Component({
  selector: 'app-worldbook-list',
  standalone: false,
  templateUrl: './worldbook-list.component.html',
  styleUrl: './worldbook-list.component.css'
})
export class WorldbookListComponent implements OnInit {
  worldbooks: WorldbookResponseDto[] = [];
  loading = false;
  error: string | null = null;

  // Delete modal state
  showDeleteModal = false;
  worldbookToDelete: { id: string; name: string } | null = null;
  isDeleting = false;

  constructor(
    private worldbookService: WorldbookService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadWorldbooks();
  }

  loadWorldbooks(): void {
    this.loading = true;
    this.error = null;

    this.worldbookService.getAllWorldbooks().subscribe({
      next: (worldbooks) => {
        this.worldbooks = worldbooks;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading worldbooks:', err);
        this.error = 'Failed to load worldbooks. Please try again later.';
        this.loading = false;
        this.worldbooks = [];
      }
    });
  }

  createNewWorldbook(): void {
    this.router.navigate(['/worldbooks/create']);
  }

  editWorldbook(worldbook: WorldbookResponseDto): void {
    this.router.navigate(['/worldbooks/edit', worldbook.id]);
  }

  deleteWorldbook(event: Event, worldbookId: string, worldbookName: string): void {
    event.stopPropagation();
    this.worldbookToDelete = { id: worldbookId, name: worldbookName };
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    if (!this.worldbookToDelete) return;

    this.isDeleting = true;

    this.worldbookService.deleteWorldbook(this.worldbookToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteModal = false;
        this.worldbookToDelete = null;
        this.loadWorldbooks();
      },
      error: (err) => {
        console.error('Error deleting worldbook:', err);
        this.isDeleting = false;
        alert('Failed to delete worldbook. Please try again.');
      }
    });
  }

  cancelDelete(): void {
    if (!this.isDeleting) {
      this.showDeleteModal = false;
      this.worldbookToDelete = null;
    }
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
