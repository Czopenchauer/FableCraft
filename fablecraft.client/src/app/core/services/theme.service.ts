import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';

export type Theme = 'medieval-fantasy' | 'modern-dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  public currentTheme$: Observable<Theme>;
  private readonly THEME_STORAGE_KEY = 'fablecraft-theme';
  private currentThemeSubject: BehaviorSubject<Theme>;

  constructor() {
    // Load theme from localStorage or default to medieval-fantasy
    const savedTheme = this.getStoredTheme();
    this.currentThemeSubject = new BehaviorSubject<Theme>(savedTheme);
    this.currentTheme$ = this.currentThemeSubject.asObservable();

    // Apply the initial theme
    this.applyTheme(savedTheme);
  }

  /**
   * Get the currently active theme
   */
  getCurrentTheme(): Theme {
    return this.currentThemeSubject.value;
  }

  /**
   * Set and apply a new theme
   */
  setTheme(theme: Theme): void {
    this.currentThemeSubject.next(theme);
    this.applyTheme(theme);
    this.storeTheme(theme);
  }

  /**
   * Toggle between available themes
   */
  toggleTheme(): void {
    const currentTheme = this.getCurrentTheme();
    const newTheme: Theme = currentTheme === 'medieval-fantasy' ? 'modern-dark' : 'medieval-fantasy';
    this.setTheme(newTheme);
  }

  /**
   * Get theme display name
   */
  getThemeDisplayName(theme: Theme): string {
    const displayNames: Record<Theme, string> = {
      'medieval-fantasy': 'Medieval Fantasy',
      'modern-dark': 'Modern Dark'
    };
    return displayNames[theme];
  }

  /**
   * Get all available themes
   */
  getAvailableThemes(): Theme[] {
    return ['medieval-fantasy', 'modern-dark'];
  }

  /**
   * Apply theme to the document
   */
  private applyTheme(theme: Theme): void {
    const html = document.documentElement;

    // Remove all theme classes
    html.removeAttribute('data-theme');

    // Add new theme
    html.setAttribute('data-theme', theme);
  }

  /**
   * Store theme preference in localStorage
   */
  private storeTheme(theme: Theme): void {
    try {
      localStorage.setItem(this.THEME_STORAGE_KEY, theme);
    } catch (error) {
      console.warn('Failed to store theme preference:', error);
    }
  }

  /**
   * Retrieve theme preference from localStorage
   */
  private getStoredTheme(): Theme {
    try {
      const stored = localStorage.getItem(this.THEME_STORAGE_KEY);
      if (stored === 'medieval-fantasy' || stored === 'modern-dark') {
        return stored;
      }
    } catch (error) {
      console.warn('Failed to retrieve theme preference:', error);
    }
    return 'medieval-fantasy'; // Default theme
  }
}
