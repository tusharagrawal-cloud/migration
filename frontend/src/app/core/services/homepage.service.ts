import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { HomepageConfig } from '../models/homepage.models';

@Injectable({ providedIn: 'root' })
export class HomepageService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getConfig(): Observable<HomepageConfig> {
    return this.http.get<HomepageConfig>(`${this.baseUrl}/api/homepage`);
  }
}
