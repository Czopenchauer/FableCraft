import {GraphRagSettingsSummaryDto} from '../../settings/models/graph-rag-settings.model';

export interface WorldbookDto {
  name: string;
  lorebooks?: CreateLorebookDto[];
  graphRagSettingsId?: string | null;
}

export interface WorldbookUpdateDto {
  name: string;
  lorebooks?: UpdateLorebookDto[];
  graphRagSettingsId?: string | null;
}

export interface WorldbookResponseDto {
  id: string;
  name: string;
  lorebooks: LorebookResponseDto[];
  graphRagSettingsId?: string | null;
  graphRagSettings?: GraphRagSettingsSummaryDto | null;
}

export interface LorebookDto {
  worldbookId: string;
  title: string;
  content: string;
  category: string;
}

export type ContentType = 'json' | 'txt';

export interface CreateLorebookDto {
  title: string;
  content: string;
  category: string;
  contentType: ContentType;
}

export interface UpdateLorebookDto {
  id?: string;  // null/undefined = new, string = update existing
  title: string;
  content: string;
  category: string;
  contentType: ContentType;
}

export interface LorebookResponseDto {
  id: string;
  worldbookId: string;
  title: string;
  content: string;
  category: string;
  contentType: ContentType;
}

export type IndexingStatus = 'NotIndexed' | 'Indexing' | 'Indexed' | 'Failed';

export interface IndexStatusResponse {
  worldbookId: string;
  status: IndexingStatus;
  error?: string;
}

export interface IndexStartResponse {
  worldbookId: string;
  message: string;
  lorebookCount: number;
}
