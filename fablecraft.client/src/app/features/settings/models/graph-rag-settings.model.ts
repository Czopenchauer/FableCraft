export interface GraphRagSettingsDto {
  name: string;
  // LLM Configuration
  llmProvider: string;
  llmModel: string;
  llmEndpoint?: string | null;
  llmApiKey: string;
  llmApiVersion?: string | null;
  llmMaxTokens: number;
  llmRateLimitEnabled: boolean;
  llmRateLimitRequests: number;
  llmRateLimitInterval: number;
  // Embedding Configuration
  embeddingProvider: string;
  embeddingModel: string;
  embeddingEndpoint?: string | null;
  embeddingApiKey?: string | null;
  embeddingApiVersion?: string | null;
  embeddingDimensions: number;
  embeddingMaxTokens: number;
  embeddingBatchSize: number;
  huggingfaceTokenizer?: string | null;
}

export interface GraphRagSettingsResponseDto extends GraphRagSettingsDto {
  id: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface GraphRagSettingsSummaryDto {
  id: string;
  name: string;
  llmProvider: string;
  llmModel: string;
  embeddingProvider: string;
  embeddingModel: string;
}
