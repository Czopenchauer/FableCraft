import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TrackerDefinitionService } from '../../services/tracker-definition.service';
import {
  TrackerDefinitionDto,
  TrackerDefinitionResponseDto,
  TrackerStructure,
  FieldDefinition,
  FieldType
} from '../../models/tracker-definition.model';
import { JsonRendererComponent } from '../json-renderer/json-renderer.component';
import { FieldEditorComponent } from '../field-editor/field-editor.component';

@Component({
  selector: 'app-tracker-definition-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, JsonRendererComponent, FieldEditorComponent],
  templateUrl: './tracker-definition-builder.component.html',
  styleUrls: ['./tracker-definition-builder.component.css']
})
export class TrackerDefinitionBuilderComponent implements OnInit {
  definitionId: string | null = null;
  definitionName = '';
  structure: TrackerStructure | null = null;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  error: string | null = null;

  // Live preview
  visualizationData: any = null;
  isLoadingVisualization = false;

  // Framework field names that are locked
  private readonly frameworkFields = {
    story: ['Time', 'Weather', 'Location'],
    mainCharacter: ['Name'],
    characters: ['Name'],
    charactersPresent: 'CharactersPresent'
  };

  FieldType = FieldType;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private trackerDefinitionService: TrackerDefinitionService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.definitionId = params.get('id');
      this.isEditMode = !!this.definitionId;

      if (this.isEditMode) {
        this.loadDefinition(this.definitionId!);
      } else {
        this.loadDefaultStructure();
      }
    });
  }

  loadDefaultStructure(): void {
    this.isLoading = true;
    this.trackerDefinitionService.getDefaultStructure().subscribe({
      next: (structure) => {
        this.structure = structure;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load default structure';
        console.error('Error loading default structure:', err);
        this.isLoading = false;
      }
    });
  }

  loadDefinition(id: string): void {
    this.isLoading = true;
    this.trackerDefinitionService.getDefinitionById(id).subscribe({
      next: (definition) => {
        this.definitionName = definition.name;
        this.structure = definition.structure;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load tracker definition';
        console.error('Error loading tracker definition:', err);
        this.isLoading = false;
      }
    });
  }

  onRefreshPreview(): void {
    if (!this.structure) return;

    this.isLoadingVisualization = true;
    this.trackerDefinitionService.visualizeTracker(this.structure).subscribe({
      next: (data) => {
        this.visualizationData = data;
        this.isLoadingVisualization = false;
      },
      error: (err) => {
        console.error('Error visualizing tracker:', err);
        this.isLoadingVisualization = false;
      }
    });
  }

  isFrameworkField(section: string, fieldName: string): boolean {
    switch (section) {
      case 'story':
        return this.frameworkFields.story.includes(fieldName);
      case 'mainCharacter':
        return this.frameworkFields.mainCharacter.includes(fieldName);
      case 'characters':
        return this.frameworkFields.characters.includes(fieldName);
      case 'charactersPresent':
        return fieldName === this.frameworkFields.charactersPresent;
      default:
        return false;
    }
  }

  addCustomField(section: keyof TrackerStructure): void {
    if (!this.structure) return;

    const newField: FieldDefinition = {
      name: 'NewField',
      type: FieldType.String,
      prompt: 'Enter prompt for this field',
      defaultValue: '',
      exampleValues: []
    };

    const sectionData = this.structure[section];
    if (Array.isArray(sectionData)) {
      sectionData.push(newField);
    }
  }

  removeField(section: keyof TrackerStructure, index: number): void {
    if (!this.structure) return;

    const sectionData = this.structure[section];
    if (Array.isArray(sectionData)) {
      const field = sectionData[index];

      // Prevent deletion of framework fields
      if (this.isFrameworkField(section, field.name)) {
        alert('Cannot delete framework fields');
        return;
      }

      sectionData.splice(index, 1);
    }
  }

  addExampleValue(field: FieldDefinition): void {
    if (!field.exampleValues) {
      field.exampleValues = [];
    }
    field.exampleValues.push('');
  }

  removeExampleValue(field: FieldDefinition, index: number): void {
    if (field.exampleValues) {
      field.exampleValues.splice(index, 1);
    }
  }

  addNestedField(field: FieldDefinition): void {
    if (!field.nestedFields) {
      field.nestedFields = [];
    }

    const newNestedField: FieldDefinition = {
      name: 'NestedField',
      type: FieldType.String,
      prompt: 'Enter prompt for nested field',
      defaultValue: '',
      exampleValues: []
    };

    field.nestedFields.push(newNestedField);
  }

  removeNestedField(field: FieldDefinition, index: number): void {
    if (field.nestedFields) {
      field.nestedFields.splice(index, 1);
    }
  }

  enableCharacterDevelopment(): void {
    if (this.structure && !this.structure.characterDevelopment) {
      this.structure.characterDevelopment = [];
    }
  }

  disableCharacterDevelopment(): void {
    if (this.structure) {
      this.structure.characterDevelopment = undefined;
    }
  }

  enableMainCharacterDevelopment(): void {
    if (this.structure && !this.structure.mainCharacterDevelopment) {
      this.structure.mainCharacterDevelopment = [];
    }
  }

  disableMainCharacterDevelopment(): void {
    if (this.structure) {
      this.structure.mainCharacterDevelopment = undefined;
    }
  }

  onSave(): void {
    if (!this.structure || !this.definitionName.trim()) {
      alert('Please provide a name for the tracker definition');
      return;
    }

    const dto: TrackerDefinitionDto = {
      name: this.definitionName,
      structure: this.structure
    };

    this.isSaving = true;
    this.error = null;

    if (this.isEditMode && this.definitionId) {
      // Update existing
      this.trackerDefinitionService.updateDefinition(this.definitionId, dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/adventures/tracker-definitions']);
        },
        error: (err) => {
          this.isSaving = false;
          this.handleSaveError(err);
        }
      });
    } else {
      // Create new
      this.trackerDefinitionService.createDefinition(dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/adventures/tracker-definitions']);
        },
        error: (err) => {
          this.isSaving = false;
          this.handleSaveError(err);
        }
      });
    }
  }

  private handleSaveError(err: any): void {
    if (err.status === 409) {
      this.error = 'A tracker definition with this name already exists';
    } else if (err.status === 400) {
      this.error = 'Validation failed. Please check that all required framework fields are present';
    } else {
      this.error = 'Failed to save tracker definition';
    }
    console.error('Error saving tracker definition:', err);
  }

  onCancel(): void {
    this.router.navigate(['/adventures/tracker-definitions']);
  }

  onImportFromJson(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    const reader = new FileReader();

    reader.onload = (e: ProgressEvent<FileReader>) => {
      try {
        const content = e.target?.result as string;
        const jsonData = JSON.parse(content);

        // Normalize the JSON structure (convert PascalCase to camelCase if needed)
        const normalizedData = this.normalizeTrackerStructure(jsonData);

        // Validate that the JSON has the expected structure
        if (!this.isValidTrackerStructure(normalizedData)) {
          alert('Invalid tracker structure. Please ensure the JSON file contains a valid tracker definition.');
          return;
        }

        // Update the structure with imported data
        this.structure = normalizedData;
        this.error = null;

        // Clear the file input so the same file can be selected again
        input.value = '';
      } catch (err) {
        console.error('Error parsing JSON file:', err);
        alert('Failed to parse JSON file. Please ensure it contains valid JSON.');
      }
    };

    reader.onerror = () => {
      alert('Failed to read file.');
    };

    reader.readAsText(file);
  }

  private normalizeTrackerStructure(data: any): TrackerStructure {
    // Helper function to convert PascalCase to camelCase
    const toCamelCase = (str: string): string => {
      return str.charAt(0).toLowerCase() + str.slice(1);
    };

    // Helper function to normalize a field definition
    const normalizeField = (field: any): FieldDefinition => {
      const normalized: any = {};

      // Map common property names (both PascalCase and camelCase)
      normalized.name = field.Name || field.name || '';
      normalized.type = field.Type || field.type || FieldType.String;
      normalized.prompt = field.Prompt || field.prompt || '';

      if (field.DefaultValue !== undefined || field.defaultValue !== undefined) {
        normalized.defaultValue = field.DefaultValue ?? field.defaultValue;
      }

      if (field.ExampleValues || field.exampleValues) {
        normalized.exampleValues = field.ExampleValues || field.exampleValues;
      }

      if (field.NestedFields || field.nestedFields) {
        const nested = field.NestedFields || field.nestedFields;
        normalized.nestedFields = Array.isArray(nested)
          ? nested.map((n: any) => normalizeField(n))
          : [];
      }

      return normalized as FieldDefinition;
    };

    // Normalize the structure
    const normalized: any = {};

    // Story section
    const storyData = data.Story || data.story;
    normalized.story = Array.isArray(storyData)
      ? storyData.map((f: any) => normalizeField(f))
      : [];

    // CharactersPresent section (single field, not array)
    const charsPresentData = data.CharactersPresent || data.charactersPresent;
    normalized.charactersPresent = charsPresentData
      ? normalizeField(charsPresentData)
      : { name: 'CharactersPresent', type: FieldType.Array, prompt: '' };

    // MainCharacter section
    const mainCharData = data.MainCharacter || data.mainCharacter;
    normalized.mainCharacter = Array.isArray(mainCharData)
      ? mainCharData.map((f: any) => normalizeField(f))
      : [];

    // Characters section
    const charsData = data.Characters || data.characters;
    normalized.characters = Array.isArray(charsData)
      ? charsData.map((f: any) => normalizeField(f))
      : [];

    // Optional sections
    const charDevData = data.CharacterDevelopment || data.characterDevelopment;
    if (charDevData && Array.isArray(charDevData)) {
      normalized.characterDevelopment = charDevData.map((f: any) => normalizeField(f));
    }

    const mainCharDevData = data.MainCharacterDevelopment || data.mainCharacterDevelopment;
    if (mainCharDevData && Array.isArray(mainCharDevData)) {
      normalized.mainCharacterDevelopment = mainCharDevData.map((f: any) => normalizeField(f));
    }

    return normalized as TrackerStructure;
  }

  private isValidTrackerStructure(data: any): boolean {
    // Basic validation to ensure required framework fields exist
    if (!data || typeof data !== 'object') {
      return false;
    }

    // Check for required sections
    if (!Array.isArray(data.story) || !Array.isArray(data.mainCharacter) ||
        !Array.isArray(data.characters) || !data.charactersPresent) {
      return false;
    }

    // Check that framework fields exist in each section
    const storyFieldNames = data.story.map((f: any) => f.name);
    const hasStoryFramework = this.frameworkFields.story.every(name =>
      storyFieldNames.includes(name)
    );

    const mainCharFieldNames = data.mainCharacter.map((f: any) => f.name);
    const hasMainCharFramework = this.frameworkFields.mainCharacter.every(name =>
      mainCharFieldNames.includes(name)
    );

    const charFieldNames = data.characters.map((f: any) => f.name);
    const hasCharFramework = this.frameworkFields.characters.every(name =>
      charFieldNames.includes(name)
    );

    const hasCharactersPresent = data.charactersPresent.name === this.frameworkFields.charactersPresent;

    return hasStoryFramework && hasMainCharFramework && hasCharFramework && hasCharactersPresent;
  }

  onDelete(): void {
    if (!this.definitionId) return;

    if (!confirm(`Are you sure you want to delete this tracker definition?`)) {
      return;
    }

    this.trackerDefinitionService.deleteDefinition(this.definitionId).subscribe({
      next: () => {
        this.router.navigate(['/adventures/tracker-definitions']);
      },
      error: (err) => {
        if (err.status === 409) {
          alert('Cannot delete this tracker definition because it is currently in use by one or more adventures.');
        } else {
          alert('Failed to delete tracker definition');
        }
        console.error('Error deleting tracker definition:', err);
      }
    });
  }

  trackByIndex(index: number): number {
    return index;
  }
}
