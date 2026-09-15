import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/** ONE77 API base URL — environment-driven, never hardcoded. See src/environments/. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
