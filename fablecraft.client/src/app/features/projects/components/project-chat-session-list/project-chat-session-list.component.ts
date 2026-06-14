import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ProjectChatSessionResponseDto, ProjectChatSessionDto} from '../../models/project.model';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';

@Component({
  selector: 'app-project-chat-session-list',
  standalone: false,
  templateUrl: './project-chat-session-list.component.html',
  styleUrl: './project-chat-session-list.component.css'
})
export class ProjectChatSessionListComponent implements OnInit, OnDestroy {
  @Input() sessions: ProjectChatSessionResponseDto[] = [];
  @Input() activeSessionId: string | null = null;
  @Input() isLoading = false;
  @Input() presets: LlmPresetResponseDto[] = [];
  @Output() sessionSelected = new EventEmitter<ProjectChatSessionResponseDto>();
  @Output() sessionCreated = new EventEmitter<ProjectChatSessionDto>();
  @Output() sessionDeleted = new EventEmitter<string>();

  showNewChatForm = false;
  isCreatingSession = false;
  newChatPresetId = '';
  newChatTitle = '';
  deletingSessionId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor() {
  }

  ngOnInit(): void {
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleNewChatForm(): void {
    this.showNewChatForm = !this.showNewChatForm;
    if (this.showNewChatForm) {
      this.newChatPresetId = this.presets.length > 0 ? this.presets[0].id : '';
      this.newChatTitle = '';
    }
  }

  cancelNewChatForm(): void {
    this.showNewChatForm = false;
    this.newChatTitle = '';
  }

  createSession(): void {
    if (!this.newChatPresetId || !this.newChatTitle.trim()) return;

    const dto: ProjectChatSessionDto = {
      llmPresetId: this.newChatPresetId,
      title: this.newChatTitle.trim()
    };

    this.sessionCreated.emit(dto);
    this.showNewChatForm = false;
    this.newChatTitle = '';
  }

  onDeleteClick(sessionId: string, event: Event): void {
    event.stopPropagation();
    this.deletingSessionId = sessionId;
  }

  confirmDelete(sessionId: string): void {
    this.deletingSessionId = null;
    this.sessionDeleted.emit(sessionId);
  }

  cancelDelete(): void {
    this.deletingSessionId = null;
  }

  sortedSessions(): ProjectChatSessionResponseDto[] {
    return [...this.sessions].sort((a, b) => {
      const aDate = a.updatedAt || a.createdAt;
      const bDate = b.updatedAt || b.createdAt;
      return new Date(bDate).getTime() - new Date(aDate).getTime();
    });
  }
}