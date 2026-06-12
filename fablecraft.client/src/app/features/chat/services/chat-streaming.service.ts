import {Injectable, NgZone} from '@angular/core';
import {ChangeDetectorRef} from '@angular/core';
import {Observable, Subscriber} from 'rxjs';
import {ChatSseChunk} from '../models/chat.model';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatStreamingService {
  private readonly apiUrl = `${environment.apiUrl}/api/chat`;

  constructor(private ngZone: NgZone) {
  }

  streamMessage(sessionId: string, content: string, cdr: ChangeDetectorRef): Observable<ChatSseChunk> {
    return new Observable<ChatSseChunk>((subscriber: Subscriber<ChatSseChunk>) => {
      const abortController = new AbortController();
      const url = `${this.apiUrl}/sessions/${sessionId}/messages`;

      this.ngZone.runOutsideAngular(async () => {
        try {
          const response = await fetch(url, {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({content}),
            signal: abortController.signal
          });

          if (!response.ok) {
            const errorText = await response.text();
            this.ngZone.run(() => {
              subscriber.next({type: 'error', error: errorText || `HTTP ${response.status}`});
              subscriber.complete();
            });
            return;
          }

          const reader = response.body?.getReader();
          if (!reader) {
            this.ngZone.run(() => {
              subscriber.next({type: 'error', error: 'No response body'});
              subscriber.complete();
            });
            return;
          }

          const decoder = new TextDecoder();
          let buffer = '';

          while (true) {
            const {done, value} = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, {stream: true});
            const lines = buffer.split('\n');
            buffer = lines.pop() || '';

            for (const line of lines) {
              const trimmed = line.trim();
              if (!trimmed.startsWith('data:')) continue;
              const dataStr = trimmed.slice(5).trim();
              if (!dataStr) continue;

              try {
                const chunk: ChatSseChunk = JSON.parse(dataStr);
                this.ngZone.run(() => {
                  subscriber.next(chunk);
                  cdr.detectChanges();
                });
                if (chunk.type === 'done' || chunk.type === 'error') {
                  subscriber.complete();
                  return;
                }
              } catch {
                // skip unparseable lines
              }
            }
          }

          this.ngZone.run(() => {
            subscriber.next({type: 'done'});
            subscriber.complete();
          });
        } catch (err: any) {
          if (err.name === 'AbortError') {
            this.ngZone.run(() => subscriber.complete());
          } else {
            this.ngZone.run(() => {
              subscriber.next({type: 'error', error: err.message || 'Streaming failed'});
              subscriber.complete();
            });
          }
        }
      });

      return () => {
        abortController.abort();
      };
    });
  }
}