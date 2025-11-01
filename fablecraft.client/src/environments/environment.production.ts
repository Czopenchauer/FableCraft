// This file is used for production builds
// With Aspire, you have two options:
// 1. Use relative URLs (empty string) if the backend serves the Angular app
// 2. Set API_URL environment variable during build for the backend URL

export const environment = {
  production: true,
  // Will be replaced during build by environment variable
  // Use relative URL as fallback (works when backend serves the frontend)
  apiUrl: ''
};
