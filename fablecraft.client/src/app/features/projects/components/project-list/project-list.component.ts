import {Component, OnDestroy, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {interval, Subject} from 'rxjs';
import {startWith, switchMap, takeUntil} from 'rxjs/operators';
import {ProjectResponseDto, IndexingStatusDto, IndexingStatusResponse} from '../../models/project.model';
import {ProjectService} from '../../services/project.service';

interface IndexStateInfo {
  status: IndexingStatusDto;
  pendingChangesCount?: number;
}

@Component({
  selector: 'app-project-list',
  standalone: false,
  templateUrl: './project-list.component.html',
  styleUrl: './project-list.component.css'
})
export class ProjectListComponent implements OnInit, OnDestroy {
  projects: ProjectResponseDto[] = [];
  loading = false;
  error: string | null = null;

  showDeleteModal = false;
  projectToDelete: { id: string; name: string } | null = null;
  isDeleting = false;

  indexStates = new Map<string, IndexStateInfo>();
  private pollingSubjects = new Map<string, Subject<void>>();
  private destroy$ = new Subject<void>();

  constructor(
    private projectService: ProjectService,
    private router: Router
  ) {
  }

  ngOnInit(): void {
    this.loadProjects();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopAllPolling();
  }

  loadProjects(): void {
    this.loading = true;
    this.error = null;

    this.projectService.getAllProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.loading = false;
        this.loadInitialIndexStatuses();
      },
      error: (err) => {
        console.error('Error loading projects:', err);
        this.error = 'Failed to load projects. Please try again later.';
        this.loading = false;
        this.projects = [];
      }
    });
  }

  createNewProject(): void {
    this.router.navigate(['/projects/create']);
  }

  openProject(project: ProjectResponseDto): void {
    this.router.navigate(['/projects', project.id]);
  }

  deleteProject(event: Event, projectId: string, projectName: string): void {
    event.stopPropagation();
    this.projectToDelete = {id: projectId, name: projectName};
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    if (!this.projectToDelete) return;

    this.isDeleting = true;

    this.projectService.deleteProject(this.projectToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteModal = false;
        this.projectToDelete = null;
        this.loadProjects();
      },
      error: (err) => {
        console.error('Error deleting project:', err);
        this.isDeleting = false;
        alert('Failed to delete project. Please try again.');
      }
    });
  }

  cancelDelete(): void {
    if (!this.isDeleting) {
      this.showDeleteModal = false;
      this.projectToDelete = null;
    }
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  getIndexState(projectId: string): IndexStateInfo {
    return this.indexStates.get(projectId) ?? {status: 'NotIndexed'};
  }

  startIndexing(event: Event, projectId: string): void {
    event.stopPropagation();

    this.indexStates.set(projectId, {status: 'Indexing'});

    this.projectService.startIndexing(projectId).subscribe({
      next: () => {
        this.startPolling(projectId);
      },
      error: (err) => {
        console.error('Error starting indexing:', err);
        this.indexStates.set(projectId, {status: 'NotIndexed'});
      }
    });
  }

  getPendingChangesCount(projectId: string): number {
    const state = this.indexStates.get(projectId);
    return state?.pendingChangesCount ?? 0;
  }

  private loadInitialIndexStatuses(): void {
    if (this.projects.length === 0) return;

    this.projects.forEach(project => {
      this.indexStates.set(project.id, {
        status: project.indexingStatus,
        pendingChangesCount: 0
      });
      if (project.indexingStatus === 'Indexing') {
        this.startPolling(project.id);
      }
      if (project.indexingStatus === 'NeedsReindexing') {
        this.loadPendingChanges(project.id);
      }
    });
  }

  private loadPendingChanges(projectId: string): void {
    this.projectService.getIndexingStatus(projectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (status) => {
          const state = this.indexStates.get(projectId);
          if (state) {
            state.pendingChangesCount = status.pendingChanges?.length ?? 0;
          }
        },
        error: () => {}
      });
  }

  private startPolling(projectId: string): void {
    this.stopPolling(projectId);

    const stopPolling$ = new Subject<void>();
    this.pollingSubjects.set(projectId, stopPolling$);

    interval(3000).pipe(
      startWith(0),
      switchMap(() => this.projectService.getIndexingStatus(projectId)),
      takeUntil(stopPolling$),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (status: IndexingStatusResponse) => {
        this.indexStates.set(projectId, {
          status: status.status,
          pendingChangesCount: status.pendingChanges?.length ?? 0
        });
        if (status.status === 'Indexed' || status.status === 'NotIndexed' || status.status === 'NeedsReindexing') {
          this.stopPolling(projectId);
        }
      },
      error: (err) => {
        console.error('Error polling index status:', err);
        this.stopPolling(projectId);
      }
    });
  }

  private stopPolling(projectId: string): void {
    const subject = this.pollingSubjects.get(projectId);
    if (subject) {
      subject.next();
      subject.complete();
      this.pollingSubjects.delete(projectId);
    }
  }

  private stopAllPolling(): void {
    this.pollingSubjects.forEach((subject) => {
      subject.next();
      subject.complete();
    });
    this.pollingSubjects.clear();
  }
}