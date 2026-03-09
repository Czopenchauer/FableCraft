export interface LorebookEntryResponseDto {
  id: string;
  adventureId: string;
  sceneId: string | null;
  title: string | null;
  description: string;
  priority: number;
  content: string;
  category: string;
  contentType: 'json' | 'txt';
}

export interface AdventureLoreResponseDto {
  worldSettings: string | null;
  entries: LorebookEntryResponseDto[];
}

export interface LorebookCategoryGroup {
  category: string;
  entries: LorebookEntryResponseDto[];
  isExpanded: boolean;
}

export interface CreateLorebookEntryDto {
  title: string;
  content: string;
  category: string;
  description?: string;
  priority?: number;
  contentType?: 'json' | 'txt';
}

export interface UpdateLorebookEntryDto {
  title: string;
  content: string;
  category: string;
  description?: string;
  priority: number;
  contentType: 'json' | 'txt';
}

export interface BulkCreateLorebookEntriesDto {
  entries: CreateLorebookEntryDto[];
}
