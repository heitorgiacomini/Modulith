import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { environment } from './environments/environment';

fetch('/config.json', { cache: 'no-store' })
  .then(async response => {
    if (!response.ok) throw new Error('Application configuration could not be loaded.');
    const config = await response.json();
    if (!config.apiUrl || !config.graphqlUrl || !config.keycloak?.url || !config.keycloak?.realm || !config.keycloak?.clientId)
      throw new Error('Application configuration is incomplete.');
    Object.assign(environment, config);
    return bootstrapApplication(App, appConfig);
  })
  .catch((err) => console.error(err));
