import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ChatService} from '../../services/chat.service';
import {ToastService} from '../../../../core/services/toast.service';
import {ChatSessionResponseDto, ChatSessionWithMessagesDto} from '../../models/chat.model';

@Component({
  selector: 'app-chat',
  standalone: false,
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit, OnDestroy {
  sessions: ChatSessionResponseDto[] = [];
  activeSessionId: string | null = null;
  activeSession: ChatSessionWithMessagesDto | null = null;
  isLoadingSessions = false;

  showMobileSidebar = false;

  private destroy$ = new Subject<void>();

  constructor(
    private chatService: ChatService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.loadSessions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadSessions(): void {
    this.isLoadingSessions = true;
    this.chatService.getSessions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (sessions) => {
          this.sessions = sessions;
          this.isLoadingSessions = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoadingSessions = false;
          this.toastService.error('Failed to load chat sessions');
          this.cdr.detectChanges();
        }
      });
  }

  onSessionSelected(session: ChatSessionResponseDto): void {
    this.activeSessionId = session.id;
    this.chatService.getSession(session.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (fullSession) => {
          this.activeSession = fullSession;
          this.showMobileSidebar = false;
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to load session')
      });
  }

  onSessionCreated(session: ChatSessionResponseDto): void {
    this.sessions.unshift(session);
    this.activeSessionId = session.id;
    this.chatService.getSession(session.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (fullSession) => {
          this.activeSession = fullSession;
          this.showMobileSidebar = false;
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to load new session')
      });
  }

  onSessionDeleted(sessionId: string): void {
    this.chatService.deleteSession(sessionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.sessions = this.sessions.filter(s => s.id !== sessionId);
          if (this.activeSessionId === sessionId) {
            this.activeSessionId = null;
            this.activeSession = null;
          }
          this.toastService.success('Session deleted');
          this.cdr.detectChanges();
        },
        error: () => this.toastService.error('Failed to delete session')
      });
  }

  onPresetChanged(session: ChatSessionWithMessagesDto): void {
    this.activeSession = session;
    const idx = this.sessions.findIndex(s => s.id === session.id);
    if (idx !== -1) {
      this.sessions[idx] = session;
    }
  }

  onMessagesUpdated(): void {
    if (this.activeSessionId) {
      this.chatService.getSession(this.activeSessionId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (fullSession) => {
            this.activeSession = fullSession;
            const idx = this.sessions.findIndex(s => s.id === fullSession.id);
            if (idx !== -1) {
              this.sessions[idx] = fullSession;
            }
            this.cdr.detectChanges();
          },
          error: () => {}
        });
    }
  }

  toggleMobileSidebar(): void {
    this.showMobileSidebar = !this.showMobileSidebar;
  }
}