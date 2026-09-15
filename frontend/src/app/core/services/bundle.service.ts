import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { PublicBundle } from '../models/bundle.models';

@Injectable({ providedIn: 'root' })
export class BundleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getBundles(): Observable<PublicBundle[]> {
    return this.http.get<PublicBundle[]>(`${this.baseUrl}/api/bundles`);
  }
}
