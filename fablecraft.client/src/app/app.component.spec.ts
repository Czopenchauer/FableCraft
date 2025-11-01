import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { ThemeService } from './core/services/theme.service';
import { of } from 'rxjs';

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let themeService: jasmine.SpyObj<ThemeService>;

  beforeEach(async () => {
    const themeServiceSpy = jasmine.createSpyObj('ThemeService', [
      'getCurrentTheme',
      'setTheme',
      'toggleTheme',
      'getThemeDisplayName'
    ], {
      currentTheme$: of('medieval-fantasy')
    });

    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      providers: [
        { provide: ThemeService, useValue: themeServiceSpy }
      ]
    }).compileComponents();

    themeService = TestBed.inject(ThemeService) as jasmine.SpyObj<ThemeService>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it('should have FableCraft as title', () => {
    expect(component.title).toEqual('FableCraft');
  });

  it('should initialize with theme from service', () => {
    component.ngOnInit();
    expect(component.currentTheme).toBe('medieval-fantasy');
  });

  it('should toggle theme when toggleTheme is called', () => {
    component.toggleTheme();
    expect(themeService.toggleTheme).toHaveBeenCalled();
  });

  it('should get correct theme icon for medieval-fantasy', () => {
    component.currentTheme = 'medieval-fantasy';
    const icon = component.getThemeIcon();
    expect(icon).toContain('M9.663');
  });

  it('should get correct theme icon for modern-dark', () => {
    component.currentTheme = 'modern-dark';
    const icon = component.getThemeIcon();
    expect(icon).toContain('M20.354');
  });

  it('should get theme display name from service', () => {
    themeService.getThemeDisplayName.and.returnValue('Medieval Fantasy');
    const label = component.getThemeLabel();
    expect(themeService.getThemeDisplayName).toHaveBeenCalledWith(component.currentTheme);
    expect(label).toBe('Medieval Fantasy');
  });
});
