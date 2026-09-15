import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { AdminBundle, SaveBundleRequest } from '../models/admin-bundle.models';

@Injectable({ providedIn: 'root' })
export class AdminBundleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(status?: string, q?: string): Observable<{ items: AdminBundle[]; count: number }> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    if (q) {
      params = params.set('q', q);
    }
    return this.http.get<{ items: AdminBundle[]; count: number }>(`${this.baseUrl}/api/admin/bundles`, { params });
  }

  getById(id: number): Observable<AdminBundle> {
    return this.http.get<AdminBundle>(`${this.baseUrl}/api/admin/bundles/${id}`);
  }

  create(request: SaveBundleRequest): Observable<AdminBundle> {
    return this.http.post<AdminBundle>(`${this.baseUrl}/api/admin/bundles`, request);
  }

  update(id: number, request: SaveBundleRequest): Observable<AdminBundle> {
    return this.http.put<AdminBundle>(`${this.baseUrl}/api/admin/bundles/${id}`, request);
  }

  publish(id: number): Observable<AdminBundle> {
    return this.http.post<AdminBundle>(`${this.baseUrl}/api/admin/bundles/${id}/publish`, {});
  }

  archive(id: number): Observable<AdminBundle> {
    return this.http.post<AdminBundle>(`${this.baseUrl}/api/admin/bundles/${id}/archive`, {});
  }

  unarchive(id: number): Observable<AdminBundle> {
    return this.http.post<AdminBundle>(`${this.baseUrl}/api/admin/bundles/${id}/unarchive`, {});
  }
}
