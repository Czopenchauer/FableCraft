export interface ProjectDto {
  name: string;
  description?: string | null;
  graphRagSettingsId?: string | null;
  llmPresetId?: string | null;
}

export interface ProjectResponseDto {
  id: string;
  name: string;
  description?: string | null;
  graphRagSettingsId?: string | null;
  graphRagSettingsName?: string | null;
  llmPresetId?: string | null;
  llmPresetName?: string | null;
  indexingStatus: IndexingStatusDto;
  lastIndexedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  files: ProjectFileSummaryDto[];
}

export interface ProjectUpdateDto {
  name?: string | null;
  description?: string | null;
  graphRagSettingsId?: string | null;
  llmPresetId?: string | null;
}

export type IndexingStatusDto = 'NotIndexed' | 'Indexing' | 'Indexed' | 'NeedsReindexing';

export interface ProjectFileDto {
  name: string;
  content: string;
  category?: string | null;
}

export interface ProjectFileUpdateDto {
  content: string;
  category?: string | null;
}

export interface ProjectFileResponseDto {
  id: string;
  projectId: string;
  name: string;
  content: string;
  category?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ProjectFileSummaryDto {
  id: string;
  name: string;
  category?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ProjectChatSessionDto {
  llmPresetId: string;
  title: string;
}

export interface ProjectChatSessionResponseDto {
  id: string;
  projectId: string;
  llmPresetId: string;
  llmPresetName: string;
  title: string;
  messages?: ProjectChatMessageEntry[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface ProjectChatMessageEntry {
  role: string;
  content: string;
}

export interface ProjectChatMessageDto {
  content: string;
}

export interface IndexingStatusResponse {
  status: IndexingStatusDto;
  lastIndexedAt?: string | null;
  pendingChanges: IndexingFileStatus[];
}

export interface IndexingFileStatus {
  fileId: string;
  fileName: string;
  isNew: boolean;
  isModified: boolean;
}