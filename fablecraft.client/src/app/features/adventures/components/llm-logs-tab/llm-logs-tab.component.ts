import {Component, Input, OnChanges, SimpleChanges} from '@angular/core';
import {LlmLogResponseDto} from '../../models/llm-log.model';
import {LlmLogService} from '../../services/llm-log.service';

interface LogGroup {
  callerName: string;
  logs: LlmLogResponseDto[];
  isExpanded: boolean;
}

@Component({
  selector: 'app-llm-logs-tab',
  standalone: false,
  templateUrl: './llm-logs-tab.component.html',
  styleUrl: './llm-logs-tab.component.css'
})
export class LlmLogsTabComponent implements OnChanges {
  @Input() sceneId: string | null = null;
  @Input() isActive = false;

  logs: LlmLogResponseDto[] = [];
  logGroups: LogGroup[] = [];
  selectedLog: LlmLogResponseDto | null = null;
  totalCount = 0;
  isLoading = false;
  hasError = false;
  errorMessage = '';

  private readonly pageSize = 100;
  private hasLoaded = false;

  constructor(private llmLogService: LlmLogService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isActive'] && this.isActive && this.sceneId && !this.hasLoaded) {
      this.loadLogs();
    }

    if (changes['sceneId'] && this.sceneId) {
      this.hasLoaded = false;
      this.logs = [];
      this.logGroups = [];
      this.selectedLog = null;

      if (this.isActive) {
        this.loadLogs();
      }
    }
  }

  loadLogs(): void {
    if (!this.sceneId || this.isLoading) return;

    this.isLoading = true;
    this.hasError = false;

    this.llmLogService.getLogsBySceneId(this.sceneId, 0, this.pageSize).subscribe({
      next: (response) => {
        this.logs = response.items;
        this.totalCount = response.totalCount;
        this.buildLogGroups();
        this.isLoading = false;
        this.hasLoaded = true;
      },
      error: (error) => {
        this.hasError = true;
        this.errorMessage = error.message || 'Failed to load LLM logs';
        this.isLoading = false;
      }
    });
  }

  loadMore(): void {
    if (!this.sceneId || this.isLoading || this.logs.length >= this.totalCount) return;

    this.isLoading = true;

    this.llmLogService.getLogsBySceneId(this.sceneId, this.logs.length, this.pageSize).subscribe({
      next: (response) => {
        this.logs = [...this.logs, ...response.items];
        this.buildLogGroups();
        this.isLoading = false;
      },
      error: (error) => {
        this.hasError = true;
        this.errorMessage = error.message || 'Failed to load more logs';
        this.isLoading = false;
      }
    });
  }

  private buildLogGroups(): void {
    const groupMap = new Map<string, LlmLogResponseDto[]>();

    for (const log of this.logs) {
      const callerName = log.callerName || 'Unknown';
      if (!groupMap.has(callerName)) {
        groupMap.set(callerName, []);
      }
      groupMap.get(callerName)!.push(log);
    }

    // Preserve expansion state
    const previousExpanded = new Set(
      this.logGroups.filter(g => g.isExpanded).map(g => g.callerName)
    );

    this.logGroups = Array.from(groupMap.entries()).map(([callerName, logs]) => ({
      callerName,
      logs,
      isExpanded: previousExpanded.has(callerName) || this.logGroups.length === 0
    }));

    // Expand first group by default if nothing was expanded
    if (this.logGroups.length > 0 && !this.logGroups.some(g => g.isExpanded)) {
      this.logGroups[0].isExpanded = true;
    }
  }

  toggleGroup(callerName: string): void {
    const group = this.logGroups.find(g => g.callerName === callerName);
    if (group) {
      group.isExpanded = !group.isExpanded;
    }
  }

  selectLog(log: LlmLogResponseDto): void {
    this.selectedLog = log;
  }

  get hasMoreLogs(): boolean {
    return this.logs.length < this.totalCount;
  }

  formatDuration(durationMs: number): string {
    if (durationMs < 1000) {
      return `${durationMs}ms`;
    }
    return `${(durationMs / 1000).toFixed(2)}s`;
  }

  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleString();
  }

  formatShortTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleTimeString();
  }

  formatTokens(tokens: number | null): string {
    if (tokens === null || tokens === undefined) return '-';
    return tokens.toLocaleString();
  }
}
