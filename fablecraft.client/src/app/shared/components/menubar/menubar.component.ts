import {Component, HostListener, OnDestroy, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import {Theme, ThemeService} from '../../../core/services/theme.service';

interface MenuItem {
  label: string;
  icon: string;
  items?: SubMenuItem[];
  route?: string;
}

interface SubMenuItem {
  label: string;
  icon: string;
  route?: string;
  action?: () => void;
}

@Component({
  selector: 'app-menubar',
  standalone: false,
  templateUrl: './menubar.component.html',
  styleUrl: './menubar.component.css'
})
export class MenubarComponent implements OnInit, OnDestroy {
  showLlmPresetModal = false;
  showGraphRagSettingsModal = false;
  currentTheme: Theme = 'medieval-fantasy';
  menuItems: MenuItem[] = [
    {
      label: 'Adventures',
      icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10',
      items: [
        {
          label: 'All Adventures',
          icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10',
          route: '/adventures'
        },
        {
          label: 'New Adventure',
          icon: 'M12 4v16m8-8H4',
          route: '/adventures/create'
        }
      ]
    },
    {
      label: 'Resources',
      icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253',
      items: [
        {
          label: 'Worldbooks',
          icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253',
          route: '/worldbooks'
        },
        {
          label: 'LLM Presets',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z',
          action: () => this.openLlmPresetModal()
        },
        {
          label: 'Tracker Definitions',
          icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01',
          route: '/adventures/tracker-definitions'
        },
        {
          label: 'GraphRAG Settings',
          icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4',
          action: () => this.openGraphRagSettingsModal()
        }
      ]
    }
  ];
  activeMenu: string | null = null;
  private destroy$ = new Subject<void>();

  constructor(
    public router: Router,
    private themeService: ThemeService
  ) {
  }

  ngOnInit(): void {
    this.themeService.currentTheme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(theme => {
        this.currentTheme = theme;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  getThemeIcon(): string {
    return this.currentTheme === 'medieval-fantasy'
      ? 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z'
      : 'M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z';
  }

  getThemeLabel(): string {
    return this.themeService.getThemeDisplayName(this.currentTheme);
  }

  openLlmPresetModal(): void {
    this.showLlmPresetModal = true;
    this.closeMenus();
  }

  closeLlmPresetModal(): void {
    this.showLlmPresetModal = false;
  }

  openGraphRagSettingsModal(): void {
    this.showGraphRagSettingsModal = true;
    this.closeMenus();
  }

  closeGraphRagSettingsModal(): void {
    this.showGraphRagSettingsModal = false;
  }

  toggleMenu(label: string): void {
    this.activeMenu = this.activeMenu === label ? null : label;
  }

  closeMenus(): void {
    this.activeMenu = null;
  }

  isActive(route: string): boolean {
    return this.router.url === route || this.router.url.startsWith(route + '/');
  }

  navigateAndClose(route: string): void {
    this.router.navigate([route]);
    this.closeMenus();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.menubar')) {
      this.closeMenus();
    }
  }
}
