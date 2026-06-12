export interface ChatSessionDto {
  adventureId: string;
  llmPresetId: string;
  title?: string;
}

export interface ChatSessionResponseDto {
  id: string;
  adventureId: string;
  adventureName: string;
  llmPresetId: string;
  llmPresetName: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface ChatSessionWithMessagesDto extends ChatSessionResponseDto {
  messages: ChatMessageResponseDto[];
}

export interface UpdateChatSessionPresetDto {
  llmPresetId: string;
}

export interface ChatMessageDto {
  content: string;
}

export interface ChatMessageResponseDto {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}