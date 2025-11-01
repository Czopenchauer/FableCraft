// This file is used for development (ng serve)
// For dev server, use empty string (relative URLs) and let proxy.conf.js handle routing
// For dev builds, this will be replaced by set-env.js with the actual Aspire URL
export const environment = {
  production: false,
  // Empty string = relative URLs (proxy handles routing during ng serve)
  apiUrl: ''
};
