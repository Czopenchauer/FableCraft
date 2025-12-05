export interface LlmPresetDto {
  name: string;
  provider: string;
  model: string;
  baseUrl?: string | null;
  apiKey: string;
  maxTokens: number;
  temperature?: number | null;
  topP?: number | null;
  topK?: number | null;
  frequencyPenalty?: number | null;
  presencePenalty?: number | null;
}

export interface LlmPresetResponseDto {
  id: string;
  name: string;
  provider: string;
  model: string;
  baseUrl?: string | null;
  apiKey: string;
  maxTokens: number;
  temperature?: number | null;
  topP?: number | null;
  topK?: number | null;
  frequencyPenalty?: number | null;
  presencePenalty?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}
