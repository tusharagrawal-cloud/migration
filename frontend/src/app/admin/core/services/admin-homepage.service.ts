import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { HomepageConfig } from '../../../core/models/homepage.models';

@Injectable({ providedIn: 'root' })
export class AdminHomepageService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getConfig(): Observable<HomepageConfig> {
    return this.http.get<HomepageConfig>(`${this.baseUrl}/api/admin/homepage`);
  }

  uploadHeroImage(file: File): Observable<HomepageConfig> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<HomepageConfig>(`${this.baseUrl}/api/admin/homepage/hero-image`, formData);
  }

  removeHeroImage(): Observable<HomepageConfig> {
    return this.http.delete<HomepageConfig>(`${this.baseUrl}/api/admin/homepage/hero-image`);
  }
}
