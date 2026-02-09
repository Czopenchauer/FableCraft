import {Component, OnDestroy, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {forkJoin, interval, Subject} from 'rxjs';
import {startWith, switchMap, takeUntil} from 'rxjs/operators';
import {CopyWorldbookDto, IndexingStatus, PendingChangeSummaryDto, WorldbookResponseDto} from '../../models/worldbook.model';
import {WorldbookService} from '../../services/worldbook.service';

interface IndexStateInfo {
  status: IndexingStatus;
  error?: string;
  pendingChanges?: PendingChangeSummaryDto;
}

@Component({
  selector: 'app-worldbook-list',
  standalone: false,
  templateUrl: './worldbook-list.component.html',
  styleUrl: './worldbook-list.component.css'
})
export class WorldbookListComponent implements OnInit, OnDestroy {
  worldbooks: WorldbookResponseDto[] = [];
  loading = false;
  error: string | null = null;

  // Delete modal state
  showDeleteModal = false;
  worldbookToDelete: { id: string; name: string } | null = null;
  isDeleting = false;

  // Indexing state
  indexStates = new Map<string, IndexStateInfo>();
  private pollingSubjects = new Map<string, Subject<void>>();
  private destroy$ = new Subject<void>();

  // Copy modal state
  showCopyModal = false;
  worldbookToCopy: WorldbookResponseDto | null = null;
  copyName = '';
  copyIndexedVolume = true;
  isCopying = false;
  copyError: string | null = null;

  constructor(
    private worldbookService: WorldbookService,
    private router: Router
  ) {
  }

  ngOnInit(): void {
    this.loadWorldbooks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopAllPolling();
  }

  loadWorldbooks(): void {
    this.loading = true;
    this.error = null;

    this.worldbookService.getAllWorldbooks().subscribe({
      next: (worldbooks) => {
        this.worldbooks = worldbooks;
        this.loading = false;
        this.loadInitialIndexStatuses();
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
    this.worldbookToDelete = {id: worldbookId, name: worldbookName};
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

  // Indexing methods
  getIndexState(worldbookId: string): IndexStateInfo {
    return this.indexStates.get(worldbookId) ?? {status: 'NotIndexed'};
  }

  startIndexing(event: Event, worldbookId: string): void {
    event.stopPropagation();

    this.indexStates.set(worldbookId, {status: 'Indexing'});

    this.worldbookService.startIndexing(worldbookId).subscribe({
      next: () => {
        this.startPolling(worldbookId);
      },
      error: (err) => {
        console.error('Error starting indexing:', err);
        this.indexStates.set(worldbookId, {status: 'Failed', error: 'Failed to start indexing'});
      }
    });
  }

  private loadInitialIndexStatuses(): void {
    if (this.worldbooks.length === 0) return;

    // Use worldbook response data for index status and pending changes
    this.worldbooks.forEach(wb => {
      this.indexStates.set(wb.id, {
        status: wb.indexingStatus,
        pendingChanges: wb.pendingChangeSummary || undefined
      });
      // Resume polling if still indexing
      if (wb.indexingStatus === 'Indexing') {
        this.startPolling(wb.id);
      }
    });
  }

  private startPolling(worldbookId: string): void {
    this.stopPolling(worldbookId);

    const stopPolling$ = new Subject<void>();
    this.pollingSubjects.set(worldbookId, stopPolling$);

    interval(3000).pipe(
      startWith(0),
      switchMap(() => this.worldbookService.getIndexStatus(worldbookId)),
      takeUntil(stopPolling$),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (status) => {
        this.indexStates.set(worldbookId, {status: status.status, error: status.error});
        // Stop polling when indexing completes (success or failure)
        if (status.status === 'Indexed' || status.status === 'Failed') {
          this.stopPolling(worldbookId);
        }
      },
      error: (err) => {
        console.error('Error polling index status:', err);
        this.stopPolling(worldbookId);
      }
    });
  }

  private stopPolling(worldbookId: string): void {
    const subject = this.pollingSubjects.get(worldbookId);
    if (subject) {
      subject.next();
      subject.complete();
      this.pollingSubjects.delete(worldbookId);
    }
  }

  private stopAllPolling(): void {
    this.pollingSubjects.forEach((subject) => {
      subject.next();
      subject.complete();
    });
    this.pollingSubjects.clear();
  }

  // Copy worldbook methods
  openCopyModal(event: Event, worldbook: WorldbookResponseDto): void {
    event.stopPropagation();
    this.worldbookToCopy = worldbook;
    this.copyName = `${worldbook.name} (Copy)`;
    this.copyIndexedVolume = worldbook.indexingStatus === 'Indexed';
    this.copyError = null;
    this.showCopyModal = true;
  }

  closeCopyModal(): void {
    if (!this.isCopying) {
      this.showCopyModal = false;
      this.worldbookToCopy = null;
      this.copyName = '';
      this.copyError = null;
    }
  }

  confirmCopy(): void {
    if (!this.worldbookToCopy || !this.copyName.trim()) {
      return;
    }

    this.isCopying = true;
    this.copyError = null;

    const dto: CopyWorldbookDto = {
      name: this.copyName.trim(),
      copyIndexedVolume: this.copyIndexedVolume
    };

    this.worldbookService.copyWorldbook(this.worldbookToCopy.id, dto).subscribe({
      next: (newWorldbook) => {
        this.isCopying = false;
        this.showCopyModal = false;
        this.worldbookToCopy = null;
        this.copyName = '';
        this.loadWorldbooks();
      },
      error: (err) => {
        console.error('Error copying worldbook:', err);
        this.copyError = err.error?.message || 'Failed to copy worldbook. Please try again.';
        this.isCopying = false;
      }
    });
  }

  canCopyWithIndex(worldbook: WorldbookResponseDto): boolean {
    return worldbook.indexingStatus === 'Indexed';
  }

  getPendingChangesDisplay(worldbookId: string): string {
    const state = this.indexStates.get(worldbookId);
    if (!state?.pendingChanges) return '';

    const parts: string[] = [];
    if (state.pendingChanges.addedCount > 0) {
      parts.push(`+${state.pendingChanges.addedCount}`);
    }
    if (state.pendingChanges.modifiedCount > 0) {
      parts.push(`~${state.pendingChanges.modifiedCount}`);
    }
    if (state.pendingChanges.deletedCount > 0) {
      parts.push(`-${state.pendingChanges.deletedCount}`);
    }
    return parts.join(' ');
  }
}
