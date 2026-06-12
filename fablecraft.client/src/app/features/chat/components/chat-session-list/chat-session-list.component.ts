import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ChatService} from '../../services/chat.service';
import {AdventureService} from '../../../adventures/services/adventure.service';
import {LlmPresetService} from '../../../adventures/services/llm-preset.service';
import {ToastService} from '../../../../core/services/toast.service';
import {ChatSessionResponseDto, ChatSessionDto} from '../../models/chat.model';
import {AdventureListItemDto} from '../../../adventures/models/adventure.model';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';

@Component({
  selector: 'app-chat-session-list',
  standalone: false,
  templateUrl: './chat-session-list.component.html',
  styleUrl: './chat-session-list.component.css'
})
export class ChatSessionListComponent implements OnInit, OnDestroy {
  @Input() sessions: ChatSessionResponseDto[] = [];
  @Input() activeSessionId: string | null = null;
  @Input() isLoadingSessions = false;
  @Output() sessionSelected = new EventEmitter<ChatSessionResponseDto>();
  @Output() sessionDeleted = new EventEmitter<string>();
  @Output() sessionCreated = new EventEmitter<ChatSessionResponseDto>();

  showNewChatForm = false;
  isCreatingSession = false;

  adventures: AdventureListItemDto[] = [];
  presets: LlmPresetResponseDto[] = [];

  newChatAdventureId = '';
  newChatPresetId = '';
  newChatTitle = '';

  deletingSessionId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private chatService: ChatService,
    private adventureService: AdventureService,
    private llmPresetService: LlmPresetService,
    private toastService: ToastService
  ) {
  }

  ngOnInit(): void {
    this.loadAdventures();
    this.loadPresets();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadAdventures(): void {
    this.adventureService.getAllAdventures()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (adventures) => this.adventures = adventures,
        error: () => this.toastService.error('Failed to load adventures')
      });
  }

  private loadPresets(): void {
    this.llmPresetService.getAllPresets()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (presets) => this.presets = presets,
        error: () => this.toastService.error('Failed to load LLM presets')
      });
  }

  toggleNewChatForm(): void {
    this.showNewChatForm = !this.showNewChatForm;
    if (this.showNewChatForm) {
      this.newChatAdventureId = this.adventures.length > 0 ? this.adventures[0].adventureId : '';
      this.newChatPresetId = this.presets.length > 0 ? this.presets[0].id : '';
      this.newChatTitle = '';
    }
  }

  cancelNewChatForm(): void {
    this.showNewChatForm = false;
    this.newChatTitle = '';
  }

  createSession(): void {
    if (!this.newChatAdventureId || !this.newChatPresetId) {
      this.toastService.warning('Please select an adventure and LLM preset');
      return;
    }

    this.isCreatingSession = true;
    const dto: ChatSessionDto = {
      adventureId: this.newChatAdventureId,
      llmPresetId: this.newChatPresetId,
      title: this.newChatTitle || undefined
    };

    this.chatService.createSession(dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (session) => {
          this.isCreatingSession = false;
          this.showNewChatForm = false;
          this.newChatTitle = '';
          this.sessionCreated.emit(session);
        },
        error: () => {
          this.isCreatingSession = false;
          this.toastService.error('Failed to create chat session');
        }
      });
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

  sortedSessions(): ChatSessionResponseDto[] {
    return [...this.sessions].sort((a, b) =>
      new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    );
  }
}