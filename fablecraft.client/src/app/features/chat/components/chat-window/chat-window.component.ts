import {ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild} from '@angular/core';
import {Subject, Subscription} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {ChatService} from '../../services/chat.service';
import {ChatStreamingService} from '../../services/chat-streaming.service';
import {LlmPresetService} from '../../../adventures/services/llm-preset.service';
import {ToastService} from '../../../../core/services/toast.service';
import {
  ChatSessionWithMessagesDto,
  ChatMessageResponseDto,
  ChatSseChunk,
  UpdateChatSessionPresetDto
} from '../../models/chat.model';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';

@Component({
  selector: 'app-chat-window',
  standalone: false,
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.css'
})
export class ChatWindowComponent implements OnInit, OnDestroy {
  @Input() session: ChatSessionWithMessagesDto | null = null;
  @Output() presetChanged = new EventEmitter<ChatSessionWithMessagesDto>();
  @Output() messagesUpdated = new EventEmitter<void>();

  messages: ChatMessageResponseDto[] = [];
  streamingContent = '';
  isStreaming = false;
  inputText = '';
  presets: LlmPresetResponseDto[] = [];
  isLoadingPresets = false;
  isDeletingLatest = false;

  private streamSubscription: Subscription | null = null;
  private destroy$ = new Subject<void>();

  @ViewChild('messageList') messageListContainer: any;
  @ViewChild('chatInput') chatInput: any;

  constructor(
    private chatService: ChatService,
    private chatStreamingService: ChatStreamingService,
    private llmPresetService: LlmPresetService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.loadPresets();
    if (this.session) {
      this.messages = [...this.session.messages];
    }
  }

  ngOnChanges(): void {
    if (this.session) {
      this.messages = [...this.session.messages];
    } else {
      this.messages = [];
      this.streamingContent = '';
      this.isStreaming = false;
    }
  }

  ngOnDestroy(): void {
    this.cancelStream();
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
    if (!this.session || !this.inputText.trim() || this.isStreaming) return;

    const content = this.inputText.trim();
    this.inputText = '';

    this.messages.push({
      id: '',
      role: 'user',
      content,
      createdAt: new Date().toISOString()
    });

    this.isStreaming = true;
    this.streamingContent = '';
    this.messages.push({
      id: '',
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString()
    });

    this.scrollToBottom();

    this.streamSubscription = this.chatStreamingService
      .streamMessage(this.session.id, content, this.cdr)
      .subscribe({
        next: (chunk: ChatSseChunk) => {
          if (chunk.type === 'token' && chunk.content) {
            this.streamingContent += chunk.content;
            const lastMsg = this.messages[this.messages.length - 1];
            if (lastMsg && lastMsg.role === 'assistant') {
              lastMsg.content = this.streamingContent;
            }
            this.cdr.detectChanges();
            this.scrollToBottom();
          } else if (chunk.type === 'done') {
            if (chunk.message) {
              const lastMsg = this.messages[this.messages.length - 1];
              if (lastMsg && lastMsg.role === 'assistant') {
                lastMsg.id = chunk.message.id;
                lastMsg.content = chunk.message.content;
                lastMsg.createdAt = chunk.message.createdAt;
              }
            }
            this.isStreaming = false;
            this.streamingContent = '';
            this.cdr.detectChanges();
            this.messagesUpdated.emit();
          } else if (chunk.type === 'error') {
            this.toastService.error(chunk.error || 'Streaming error');
            this.isStreaming = false;
            this.streamingContent = '';
            if (this.messages.length > 0 && this.messages[this.messages.length - 1].role === 'assistant' && !this.messages[this.messages.length - 1].content) {
              this.messages.pop();
            }
            this.cdr.detectChanges();
          }
        },
        error: (err) => {
          this.toastService.error('Connection error');
          this.isStreaming = false;
          this.streamingContent = '';
          this.cdr.detectChanges();
        }
      });
  }

  cancelStream(): void {
    if (this.streamSubscription) {
      this.streamSubscription.unsubscribe();
      this.streamSubscription = null;
    }
    this.isStreaming = false;
    this.streamingContent = '';
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
    if (!this.session || this.isStreaming) return;
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

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messageListContainer?.nativeElement) {
        const el = this.messageListContainer.nativeElement;
        el.scrollTop = el.scrollHeight;
      }
    }, 0);
  }

  isLastMessageStreaming(index: number): boolean {
    return this.isStreaming && index === this.messages.length - 1;
  }
}