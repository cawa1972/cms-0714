// Production environment. Swapped in for environment.development.ts at prod build
// via angular.json fileReplacements.
export const environment = {
  production: true,
  // Relative path: the SPA and API are served from the same IIS host, so API
  // calls resolve against the page origin (e.g. https://host/api/...).
  apiBaseUrl: '/api',
};
