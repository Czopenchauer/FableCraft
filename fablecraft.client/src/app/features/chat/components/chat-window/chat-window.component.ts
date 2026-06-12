import {AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild} from '@angular/core';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ChatService} from '../../services/chat.service';
import {LlmPresetService} from '../../../adventures/services/llm-preset.service';
import {ToastService} from '../../../../core/services/toast.service';
import {
  ChatSessionWithMessagesDto,
  ChatMessageResponseDto,
  UpdateChatSessionPresetDto
} from '../../models/chat.model';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';

@Component({
  selector: 'app-chat-window',
  standalone: false,
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.css'
})
export class ChatWindowComponent implements OnInit, OnDestroy, AfterViewChecked {
  @Input() session: ChatSessionWithMessagesDto | null = null;
  @Output() presetChanged = new EventEmitter<ChatSessionWithMessagesDto>();
  @Output() messagesUpdated = new EventEmitter<void>();

  messages: ChatMessageResponseDto[] = [];
  isLoading = false;
  inputText = '';
  presets: LlmPresetResponseDto[] = [];
  isLoadingPresets = false;
  isDeletingLatest = false;

  private destroy$ = new Subject<void>();
  private shouldScrollToBottom = false;

  @ViewChild('messageList') messageListContainer!: ElementRef<HTMLElement>;
  @ViewChild('chatInput') chatInputElement!: ElementRef<HTMLTextAreaElement>;

  constructor(
    private chatService: ChatService,
    private llmPresetService: LlmPresetService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.loadPresets();
    if (this.session) {
      this.messages = [...this.session.messages];
      this.shouldScrollToBottom = true;
    }
  }

  ngOnChanges(): void {
    if (this.session) {
      this.messages = [...this.session.messages];
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

  private loadPresets(): void {
    this.isLoadingPresets = true;
    this.llmPresetService.getAllPresets()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (presets) => {
          this.presets = presets;
          this.isLoadingPresets = false;
        },
        error: () => {
          this.isLoadingPresets = false;
          this.toastService.error('Failed to load LLM presets');
        }
      });
  }

  sendMessage(): void {
    if (!this.session || !this.inputText.trim() || this.isLoading) return;

    const content = this.inputText.trim();
    this.inputText = '';
    this.resetInputHeight();

    this.messages.push({
      id: '',
      role: 'user',
      content,
      createdAt: new Date().toISOString()
    });

    this.isLoading = true;
    this.messages.push({
      id: '',
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString()
    });

    this.shouldScrollToBottom = true;

    this.chatService.sendMessage(this.session.id, content)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          const lastMsg = this.messages[this.messages.length - 1];
          if (lastMsg && lastMsg.role === 'assistant') {
            lastMsg.id = response.id;
            lastMsg.content = response.content;
            lastMsg.createdAt = response.createdAt;
          }
          this.isLoading = false;
          this.cdr.detectChanges();
          this.shouldScrollToBottom = true;
          this.messagesUpdated.emit();
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

  onPresetChange(presetId: string): void {
    if (!this.session) return;
    const dto: UpdateChatSessionPresetDto = {llmPresetId: presetId};
    this.chatService.updatePreset(this.session.id, dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updated) => {
          this.toastService.success('LLM preset updated');
          this.presetChanged.emit(updated as ChatSessionWithMessagesDto);
        },
        error: () => this.toastService.error('Failed to update LLM preset')
      });
  }

  deleteLatestMessage(): void {
    if (!this.session || this.isLoading) return;
    this.isDeletingLatest = true;
    this.chatService.deleteLatestMessage(this.session.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isDeletingLatest = false;
          this.chatService.getSession(this.session!.id)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (fullSession) => {
                this.messages = [...fullSession.messages];
                this.shouldScrollToBottom = true;
                this.cdr.detectChanges();
                this.messagesUpdated.emit();
              },
              error: () => this.toastService.error('Failed to refresh session')
            });
        },
        error: () => {
          this.isDeletingLatest = false;
          this.toastService.error('Failed to delete latest message');
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