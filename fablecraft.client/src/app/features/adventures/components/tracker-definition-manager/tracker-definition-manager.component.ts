import {Component, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterModule} from '@angular/router';
import {TrackerDefinitionService} from '../../services/tracker-definition.service';
import {TrackerDefinitionResponseDto} from '../../models/tracker-definition.model';

@Component({
  selector: 'app-tracker-definition-manager',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './tracker-definition-manager.component.html',
  styleUrls: ['./tracker-definition-manager.component.css']
})
export class TrackerDefinitionManagerComponent implements OnInit {
  definitions: TrackerDefinitionResponseDto[] = [];
  isLoading = false;
  error: string | null = null;

  constructor(private trackerDefinitionService: TrackerDefinitionService) {
  }

  ngOnInit(): void {
    this.loadDefinitions();
  }

  loadDefinitions(): void {
    this.isLoading = true;
    this.error = null;

    this.trackerDefinitionService.getAllDefinitions().subscribe({
      next: (definitions) => {
        this.definitions = definitions;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load tracker definitions';
        console.error('Error loading tracker definitions:', err);
        this.isLoading = false;
      }
    });
  }

  onDelete(definition: TrackerDefinitionResponseDto): void {
    if (!confirm(`Are you sure you want to delete the tracker definition "${definition.name}"?`)) {
      return;
    }

    this.trackerDefinitionService.deleteDefinition(definition.id).subscribe({
      next: () => {
        this.loadDefinitions();
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

  onDuplicate(definition: TrackerDefinitionResponseDto): void {
    const duplicateName = `${definition.name} (Copy)`;
    const duplicateDto = {
      name: duplicateName,
      structure: definition.structure
    };

    this.trackerDefinitionService.createDefinition(duplicateDto).subscribe({
      next: () => {
        this.loadDefinitions();
      },
      error: (err) => {
        alert('Failed to duplicate tracker definition');
        console.error('Error duplicating tracker definition:', err);
      }
    });
  }
}
