import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {interval, Subject} from 'rxjs';
import {startWith, switchMap, takeUntil} from 'rxjs/operators';
import {ProjectService} from '../../services/project.service';
import {ToastService} from '../../../../core/services/toast.service';
import {LlmPresetService} from '../../../adventures/services/llm-preset.service';
import {GraphRagSettingsService} from '../../../settings/services/graph-rag-settings.service';
import {
  ProjectResponseDto,
  ProjectUpdateDto,
  ProjectFileSummaryDto,
  ProjectFileResponseDto,
  ProjectFileDto,
  ProjectFileUpdateDto,
  ProjectChatSessionResponseDto,
  ProjectChatSessionDto,
  IndexingStatusDto,
  IndexingStatusResponse
} from '../../models/project.model';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';
import {GraphRagSettingsSummaryDto} from '../../../settings/models/graph-rag-settings.model';

type DetailView = 'none' | 'file' | 'chat';

@Component({
  selector: 'app-project-detail',
  standalone: false,
  templateUrl: './project-detail.component.html',
  styleUrl: './project-detail.component.css'
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  project: ProjectResponseDto | null = null;
  loading = true;
  error: string | null = null;

  selectedFileId: string | null = null;
  selectedFile: ProjectFileResponseDto | null = null;
  selectedSessionId: string | null = null;
  selectedSession: ProjectChatSessionResponseDto | null = null;
  activeView: DetailView = 'none';

  chatSessions: ProjectChatSessionResponseDto[] = [];
  isLoadingSessions = false;

  llmPresets: LlmPresetResponseDto[] = [];
  graphRagSettingsOptions: GraphRagSettingsSummaryDto[] = [];

  showDeleteModal = false;
  isDeleting = false;

  indexStatus: IndexingStatusDto = 'NotIndexed';
  isIndexing = false;
  pendingChangesCount = 0;
  private pollingSubject: Subject<void> | null = null;
  private destroy$ = new Subject<void>();

  showMobileSidebar = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private projectService: ProjectService,
    private llmPresetService: LlmPresetService,
    private graphRagSettingsService: GraphRagSettingsService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    const projectId = this.route.snapshot.paramMap.get('id');
    if (!projectId) {
      this.router.navigate(['/projects']);
      return;
    }

    this.loadProject(projectId);
    this.loadDropdownData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopPolling();
  }

  private loadProject(projectId: string): void {
    this.loading = true;
    this.error = null;

    this.projectService.getProject(projectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (project) => {
          this.project = project;
          this.indexStatus = project.indexingStatus;
          this.loading = false;

          if (project.indexingStatus === 'Indexing') {
            this.startPolling(project.id);
          }
          if (project.indexingStatus === 'NeedsReindexing') {
            this.loadPendingChanges(project.id);
          }

          this.loadChatSessions(project.id);
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error loading project:', err);
          this.error = 'Failed to load project.';
          this.loading = false;
        }
      });
  }

  private loadChatSessions(projectId: string): void {
    this.isLoadingSessions = true;
    this.projectService.getChatSessions(projectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (sessions) => {
          this.chatSessions = sessions;
          this.isLoadingSessions = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoadingSessions = false;
          this.toastService.error('Failed to load chat sessions');
        }
      });
  }

  private loadDropdownData(): void {
    this.llmPresetService.getAllPresets()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (presets) => this.llmPresets = presets,
        error: () => this.toastService.error('Failed to load LLM presets')
      });

    this.graphRagSettingsService.getAllSummary()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (settings) => this.graphRagSettingsOptions = settings,
        error: () => this.toastService.error('Failed to load GraphRAG settings')
      });
  }

  onFileSelected(file: ProjectFileSummaryDto): void {
    if (!this.project) return;

    this.selectedFileId = file.id;
    this.selectedSessionId = null;
    this.selectedSession = null;
    this.activeView = 'file';
    this.showMobileSidebar = false;

    this.projectService.getFile(this.project.id, file.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (fullFile) => {
          this.selectedFile = fullFile;
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to load file')
      });
  }

  onFileCreated(dto: ProjectFileDto): void {
    if (!this.project) return;

    this.projectService.createFile(this.project.id, dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newFile) => {
          this.toastService.success('File created');
          this.selectedFileId = newFile.id;
          this.selectedFile = newFile;
          this.activeView = 'file';
          this.reloadProjectFiles();
        },
        error: () => this.toastService.error('Failed to create file')
      });
  }

  onFileSaved(event: { fileId: string; dto: ProjectFileUpdateDto }): void {
    if (!this.project) return;

    this.projectService.updateFile(this.project.id, event.fileId, event.dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedFile) => {
          this.toastService.success('File saved');
          this.selectedFile = updatedFile;
          this.reloadProjectFiles();
        },
        error: () => this.toastService.error('Failed to save file')
      });
  }

  onFileDeleted(fileId: string): void {
    if (!this.project) return;

    this.projectService.deleteFile(this.project.id, fileId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('File deleted');
          if (this.selectedFileId === fileId) {
            this.selectedFileId = null;
            this.selectedFile = null;
            this.activeView = 'none';
          }
          this.reloadProjectFiles();
        },
        error: () => this.toastService.error('Failed to delete file')
      });
  }

  onFileClosed(): void {
    this.selectedFileId = null;
    this.selectedFile = null;
    this.activeView = 'none';
  }

  onSessionSelected(session: ProjectChatSessionResponseDto): void {
    if (!this.project) return;

    this.selectedSessionId = session.id;
    this.selectedFileId = null;
    this.selectedFile = null;
    this.activeView = 'chat';
    this.showMobileSidebar = false;

    this.projectService.getChatSession(this.project.id, session.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (fullSession) => {
          this.selectedSession = fullSession;
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to load chat session')
      });
  }

  onSessionCreated(dto: ProjectChatSessionDto): void {
    if (!this.project) return;

    this.projectService.createChatSession(this.project.id, dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (session) => {
          this.toastService.success('Chat session created');
          this.selectedSessionId = session.id;
          this.selectedSession = session;
          this.activeView = 'chat';
          this.showMobileSidebar = false;
          this.loadChatSessions(this.project!.id);
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to create chat session')
      });
  }

  onSessionDeleted(sessionId: string): void {
    if (!this.project) return;

    this.projectService.deleteChatSession(this.project.id, sessionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Session deleted');
          this.chatSessions = this.chatSessions.filter(s => s.id !== sessionId);
          if (this.selectedSessionId === sessionId) {
            this.selectedSessionId = null;
            this.selectedSession = null;
            this.activeView = 'none';
          }
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to delete session')
      });
  }

  onSessionUpdated(session: ProjectChatSessionResponseDto): void {
    this.selectedSession = session;
    const idx = this.chatSessions.findIndex(s => s.id === session.id);
    if (idx !== -1) {
      this.chatSessions[idx] = session;
    }
    this.cdr.detectChanges();
  }

  private reloadProjectFiles(): void {
    if (!this.project) return;

    this.projectService.getProject(this.project.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (project) => {
          this.project = project;
          this.cdr.detectChanges();
        },
        error: () => {}
      });
  }

  startIndexing(): void {
    if (!this.project) return;

    this.indexStatus = 'Indexing';
    this.isIndexing = true;

    this.projectService.startIndexing(this.project.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.startPolling(this.project!.id);
        },
        error: () => {
          this.toastService.error('Failed to start indexing');
          this.indexStatus = 'NotIndexed';
          this.isIndexing = false;
        }
      });
  }

  onDeleteProject(): void {
    this.showDeleteModal = true;
  }

  confirmDeleteProject(): void {
    if (!this.project) return;

    this.isDeleting = true;
    this.projectService.deleteProject(this.project.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Project deleted');
          this.router.navigate(['/projects']);
        },
        error: () => {
          this.isDeleting = false;
          this.toastService.error('Failed to delete project');
        }
      });
  }

  cancelDeleteProject(): void {
    if (!this.isDeleting) {
      this.showDeleteModal = false;
    }
  }

  onUpdateProject(dto: ProjectUpdateDto): void {
    if (!this.project) return;

    this.projectService.updateProject(this.project.id, dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updated) => {
          this.project = updated;
          this.toastService.success('Project updated');
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to update project')
      });
  }

  toggleMobileSidebar(): void {
    this.showMobileSidebar = !this.showMobileSidebar;
  }

  closeMobileSidebar(): void {
    this.showMobileSidebar = false;
  }

  goBack(): void {
    this.router.navigate(['/projects']);
  }

  getUniqueCategories(): string[] {
    if (!this.project) return [];
    const categories = new Set<string>();
    this.project.files.forEach(f => {
      if (f.category) categories.add(f.category);
    });
    return Array.from(categories);
  }

  private loadPendingChanges(projectId: string): void {
    this.projectService.getIndexingStatus(projectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (status) => {
          this.pendingChangesCount = status.pendingChanges?.length ?? 0;
        },
        error: () => {}
      });
  }

  private startPolling(projectId: string): void {
    this.stopPolling();

    this.pollingSubject = new Subject<void>();

    interval(3000).pipe(
      startWith(0),
      switchMap(() => this.projectService.getIndexingStatus(projectId)),
      takeUntil(this.pollingSubject),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (status: IndexingStatusResponse) => {
        this.indexStatus = status.status;
        this.pendingChangesCount = status.pendingChanges?.length ?? 0;
        this.isIndexing = status.status === 'Indexing';
        if (status.status !== 'Indexing') {
          this.stopPolling();
          if (this.project) {
            this.project.indexingStatus = status.status;
          }
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.stopPolling();
      }
    });
  }

  private stopPolling(): void {
    if (this.pollingSubject) {
      this.pollingSubject.next();
      this.pollingSubject.complete();
      this.pollingSubject = null;
    }
  }
}