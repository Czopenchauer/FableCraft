import {AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild} from '@angular/core';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ProjectService} from '../../services/project.service';
import {ToastService} from '../../../../core/services/toast.service';
import {ProjectChatSessionResponseDto, ProjectChatMessageEntry} from '../../models/project.model';

@Component({
  selector: 'app-project-chat',
  standalone: false,
  templateUrl: './project-chat.component.html',
  styleUrl: './project-chat.component.css'
})
export class ProjectChatComponent implements OnInit, OnDestroy, AfterViewChecked {
  @Input() session: ProjectChatSessionResponseDto | null = null;
  @Input() projectId: string = '';
  @Output() sessionUpdated = new EventEmitter<ProjectChatSessionResponseDto>();

  messages: ProjectChatMessageEntry[] = [];
  isLoading = false;
  inputText = '';

  private destroy$ = new Subject<void>();
  private shouldScrollToBottom = false;

  @ViewChild('messageList') messageListContainer!: ElementRef<HTMLElement>;
  @ViewChild('chatInput') chatInputElement!: ElementRef<HTMLTextAreaElement>;

  constructor(
    private projectService: ProjectService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    if (this.session) {
      this.messages = [...(this.session.messages || [])];
      this.shouldScrollToBottom = true;
    }
  }

  ngOnChanges(): void {
    if (this.session) {
      this.messages = [...(this.session.messages || [])];
      this.shouldScrollToBottom = true;
    } else {
      this.messages = [];
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.shouldScrollToBottom = false;
      this.doScrollToBottom();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  sendMessage(): void {
    if (!this.session || !this.inputText.trim() || this.isLoading) return;

    const content = this.inputText.trim();
    this.inputText = '';
    this.resetInputHeight();

    this.messages.push({
      role: 'user',
      content
    });

    this.isLoading = true;
    this.messages.push({
      role: 'assistant',
      content: ''
    });

    this.shouldScrollToBottom = true;

    this.projectService.sendChatMessage(this.projectId, this.session.id, content)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          const lastMsg = this.messages[this.messages.length - 1];
          if (lastMsg && lastMsg.role === 'assistant') {
            lastMsg.content = response.content;
            lastMsg.role = response.role;
          }
          this.isLoading = false;
          this.cdr.detectChanges();
          this.shouldScrollToBottom = true;

          if (this.session) {
            this.session.messages = [...this.messages];
          }
          this.sessionUpdated.emit(this.session!);
        },
        error: (err) => {
          this.toastService.error('Failed to send message');
          this.isLoading = false;
          if (this.messages.length > 0 && this.messages[this.messages.length - 1].role === 'assistant' && !this.messages[this.messages.length - 1].content) {
            this.messages.pop();
          }
          this.cdr.detectChanges();
        }
      });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  autoResizeInput(): void {
    const el = this.chatInputElement?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    const maxHeight = 160;
    el.style.height = Math.min(el.scrollHeight, maxHeight) + 'px';
    el.style.overflowY = el.scrollHeight > maxHeight ? 'auto' : 'hidden';
  }

  private resetInputHeight(): void {
    const el = this.chatInputElement?.nativeElement;
    if (!el) return;
    el.style.height = '24px';
    el.style.overflowY = 'hidden';
  }

  private doScrollToBottom(): void {
    if (this.messageListContainer?.nativeElement) {
      const el = this.messageListContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }

  isLastMessageLoading(index: number): boolean {
    return this.isLoading && index === this.messages.length - 1;
  }
}