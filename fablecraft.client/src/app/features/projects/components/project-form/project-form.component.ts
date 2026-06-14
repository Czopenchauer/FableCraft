import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {Subject, takeUntil} from 'rxjs';
import {forkJoin} from 'rxjs';
import {ProjectService} from '../../services/project.service';
import {ProjectDto} from '../../models/project.model';
import {LlmPresetService} from '../../../adventures/services/llm-preset.service';
import {GraphRagSettingsService} from '../../../settings/services/graph-rag-settings.service';
import {LlmPresetResponseDto} from '../../../adventures/models/llm-preset.model';
import {GraphRagSettingsSummaryDto} from '../../../settings/models/graph-rag-settings.model';
import {ToastService} from '../../../../core/services/toast.service';

@Component({
  selector: 'app-project-form',
  standalone: false,
  templateUrl: './project-form.component.html',
  styleUrl: './project-form.component.css'
})
export class ProjectFormComponent implements OnInit, OnDestroy {
  projectForm!: FormGroup;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  llmPresets: LlmPresetResponseDto[] = [];
  graphRagSettingsOptions: GraphRagSettingsSummaryDto[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private projectService: ProjectService,
    private llmPresetService: LlmPresetService,
    private graphRagSettingsService: GraphRagSettingsService,
    private toastService: ToastService,
    private router: Router
  ) {
  }

  ngOnInit(): void {
    this.initializeForm();
    this.loadDropdownData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(): void {
    if (this.projectForm.invalid) {
      this.markFormGroupTouched(this.projectForm);
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.projectForm.value;

    const dto: ProjectDto = {
      name: formValue.name,
      description: formValue.description || null,
      graphRagSettingsId: formValue.graphRagSettingsId || null,
      llmPresetId: formValue.llmPresetId || null
    };

    this.projectService.createProject(dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (project) => {
          this.toastService.success('Project created successfully');
          this.router.navigate(['/projects', project.id]);
        },
        error: (error) => {
          console.error('Error creating project:', error);
          this.errorMessage = this.extractErrorMessage(error);
          this.isSubmitting = false;
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/projects']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.projectForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.projectForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return 'This field is required';
      if (field.errors['maxlength']) return `Maximum length is ${field.errors['maxlength'].requiredLength} characters`;
    }
    return '';
  }

  private initializeForm(): void {
    this.projectForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      graphRagSettingsId: [null],
      llmPresetId: [null]
    });
  }

  private loadDropdownData(): void {
    this.isLoading = true;

    forkJoin({
      presets: this.llmPresetService.getAllPresets(),
      graphRagSettings: this.graphRagSettingsService.getAllSummary()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.llmPresets = result.presets;
          this.graphRagSettingsOptions = result.graphRagSettings;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading dropdown data:', error);
          this.errorMessage = 'Failed to load settings. Please try again.';
          this.isLoading = false;
        }
      });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  private extractErrorMessage(error: any): string {
    if (error.error?.errors) {
      const errorMessages: string[] = [];
      for (const [field, messages] of Object.entries(error.error.errors)) {
        if (Array.isArray(messages)) {
          errorMessages.push(`${field}: ${messages.join(', ')}`);
        }
      }
      if (errorMessages.length > 0) {
        return errorMessages.join('; ');
      }
    }

    if (error.error?.message) {
      return error.error.message;
    }

    if (error.error?.title) {
      return error.error.title;
    }

    return 'Failed to create project. Please try again.';
  }
}