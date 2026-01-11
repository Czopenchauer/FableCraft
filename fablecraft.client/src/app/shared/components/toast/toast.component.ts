import {Component, OnDestroy, OnInit} from '@angular/core';
import {Toast, ToastService} from '../../../core/services/toast.service';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';

interface ToastWithTimer extends Toast {
  timeoutId?: any;
}

@Component({
  selector: 'app-toast',
  standalone: false,
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: ToastWithTimer[] = [];
  private destroy$ = new Subject<void>();

  constructor(private toastService: ToastService) {
  }

  ngOnInit(): void {
    this.toastService.getToasts()
      .pipe(takeUntil(this.destroy$))
      .subscribe(toast => {
        this.addToast(toast);
      });
  }

  ngOnDestroy(): void {
    this.toasts.forEach(toast => {
      if (toast.timeoutId) {
        clearTimeout(toast.timeoutId);
      }
    });
    this.destroy$.next();
    this.destroy$.complete();
  }

  removeToast(id: string): void {
    const index = this.toasts.findIndex(t => t.id === id);
    if (index !== -1) {
      if (this.toasts[index].timeoutId) {
        clearTimeout(this.toasts[index].timeoutId);
      }
      this.toasts.splice(index, 1);
    }
  }

  getIconPath(type: Toast['type']): string {
    switch (type) {
      case 'success':
        return 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z';
      case 'error':
        return 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z';
      case 'warning':
        return 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z';
      case 'info':
      default:
        return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
    }
  }

  private addToast(toast: Toast): void {
    const toastWithTimer: ToastWithTimer = {...toast};

    this.toasts.push(toastWithTimer);

    if (toast.duration && toast.duration > 0) {
      toastWithTimer.timeoutId = setTimeout(() => {
        this.removeToast(toast.id);
      }, toast.duration);
    }
  }
}
