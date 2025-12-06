import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { WorldbookService } from '../../services/worldbook.service';
import { WorldbookDto, WorldbookResponseDto } from '../../models/worldbook.model';

@Component({
  selector: 'app-worldbook-form',
  standalone: false,
  templateUrl: './worldbook-form.component.html',
  styleUrl: './worldbook-form.component.css'
})
export class WorldbookFormComponent implements OnInit {
  worldbookForm: FormGroup;
  isEditMode = false;
  worldbookId: string | null = null;
  loading = false;
  saving = false;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private worldbookService: WorldbookService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.worldbookForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]]
    });
  }

  ngOnInit(): void {
    this.worldbookId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.worldbookId;

    if (this.isEditMode && this.worldbookId) {
      this.loadWorldbook(this.worldbookId);
    }
  }

  loadWorldbook(id: string): void {
    this.loading = true;
    this.error = null;

    this.worldbookService.getWorldbookById(id).subscribe({
      next: (worldbook: WorldbookResponseDto) => {
        this.worldbookForm.patchValue({
          name: worldbook.name
        });
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading worldbook:', err);
        this.error = 'Failed to load worldbook. Please try again.';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.worldbookForm.invalid) {
      this.worldbookForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = null;

    const dto: WorldbookDto = {
      name: this.worldbookForm.value.name
    };

    const operation = this.isEditMode && this.worldbookId
      ? this.worldbookService.updateWorldbook(this.worldbookId, dto)
      : this.worldbookService.createWorldbook(dto);

    operation.subscribe({
      next: () => {
        this.saving = false;
        this.router.navigate(['/worldbooks']);
      },
      error: (err) => {
        console.error('Error saving worldbook:', err);
        this.error = err.error?.message || 'Failed to save worldbook. Please try again.';
        this.saving = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/worldbooks']);
  }

  get nameControl() {
    return this.worldbookForm.get('name');
  }
}
