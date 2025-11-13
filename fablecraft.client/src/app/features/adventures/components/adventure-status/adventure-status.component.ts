import {Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {interval, Subject} from 'rxjs';
import {startWith, switchMap, takeUntil} from 'rxjs/operators';
import {AdventureService} from '../../services/adventure.service';
import {AdventureCreationStatus, ComponentStatus} from '../../models/adventure.model';

@Component({
  selector: 'app-adventure-status',
  standalone: false,
  templateUrl: './adventure-status.component.html',
  styleUrl: './adventure-status.component.css'
})
export class AdventureStatusComponent implements OnInit, OnDestroy {
  status: AdventureCreationStatus | null = null;
  isLoading = true;
  hasError = false;
  errorMessage = '';
  private destroy$ = new Subject<void>();
  private adventureId: string | null = null;
  // Poll every 2 seconds
  private readonly POLL_INTERVAL = 2000;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adventureService: AdventureService
  ) {
  }

  ngOnInit(): void {
    this.adventureId = this.route.snapshot.paramMap.get('id');

    if (!this.adventureId) {
      this.errorMessage = 'No adventure ID provided';
      this.hasError = true;
      this.isLoading = false;
      return;
    }

    this.startPolling();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getStatusEntries(): Array<{ key: string; value: ComponentStatus }> {
    if (!this.status) return [];
    return Object.entries(this.status.componentStatuses).map(([key, value]) => ({
      key,
      value: value as ComponentStatus
    }));
  }

  getStatusIcon(status: ComponentStatus): string {
    switch (status) {
      case 'Completed':
        return '✓';
      case 'Failed':
        return '✗';
      case 'Pending':
        return '⋯';
      case 'InProgress':
        return '⟳';
      default:
        return '?';
    }
  }

  getStatusClass(status: ComponentStatus): string {
    switch (status) {
      case 'Completed':
        return 'status-completed';
      case 'Failed':
        return 'status-failed';
      case 'Pending':
        return 'status-pending';
      case 'InProgress':
        return 'status-in-progress';
      default:
        return '';
    }
  }

  hasAnyFailedStatus(): boolean {
    if (!this.status) return false;
    return Object.values(this.status.componentStatuses).some(s => s === 'Failed');
  }

  retry(): void {
    if (!this.adventureId) return;

    this.isLoading = true;
    this.hasError = false;

    this.adventureService.retryCreateAdventure(this.adventureId)
      .subscribe({
        next: (status) => {
          this.status = status;
          this.isLoading = false;

          // Create a new destroy$ subject for the new polling session
          this.destroy$ = new Subject<void>();

          // Restart polling
          this.startPolling();
        },
        error: (error) => {
          console.error('Error retrying adventure creation:', error);
          this.hasError = true;
          this.errorMessage = 'Failed to retry adventure creation. Please try again.';
          this.isLoading = false;
        }
      });
  }

  goToAdventureList(): void {
    this.router.navigate(['/adventures']);
  }

  private startPolling(): void {
    interval(this.POLL_INTERVAL)
      .pipe(
        startWith(0), // Start immediately
        switchMap(() => this.adventureService.getAdventureStatus(this.adventureId!)),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (status) => {
          this.status = status;
          this.isLoading = false;
          this.checkStatusAndNavigate();
        },
        error: (error) => {
          console.error('Error fetching adventure status:', error);
          this.hasError = true;
          this.errorMessage = 'Failed to fetch adventure status. Please try again.';
          this.isLoading = false;
        }
      });
  }

  private checkStatusAndNavigate(): void {
    if (!this.status) return;

    const statuses = Object.values(this.status.componentStatuses);
    const allCompleted = statuses.every(s => s === 'Completed');
    const anyFailed = statuses.some(s => s === 'Failed');

    if (allCompleted) {
      // Stop polling and navigate to game panel
      this.destroy$.next();
      this.router.navigate(['/adventures/play', this.adventureId]);
    } else if (anyFailed) {
      // Stop polling to wait for user action
      this.destroy$.next();
    }
  }
}
