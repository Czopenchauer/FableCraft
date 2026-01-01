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

export interface LorebookCategoryGroup {
  category: string;
  entries: LorebookEntryResponseDto[];
  isExpanded: boolean;
}
