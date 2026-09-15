import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import {
  AdminLearnCategory,
  AdminLearnCategoryWithCount,
  AdminLearnEntry,
  AdminLearnEntryWithCategoryName,
  SaveLearnCategoryRequest,
  SaveLearnEntryRequest,
} from '../models/admin-learn.models';

@Injectable({ providedIn: 'root' })
export class AdminLearnService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getCategories(): Observable<{ items: AdminLearnCategoryWithCount[]; count: number }> {
    return this.http.get<{ items: AdminLearnCategoryWithCount[]; count: number }>(`${this.baseUrl}/api/admin/learn/categories`);
  }

  getCategory(id: number): Observable<AdminLearnCategory> {
    return this.http.get<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories/${id}`);
  }

  createCategory(request: SaveLearnCategoryRequest): Observable<AdminLearnCategory> {
    return this.http.post<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories`, request);
  }

  updateCategory(id: number, request: SaveLearnCategoryRequest): Observable<AdminLearnCategory> {
    return this.http.put<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories/${id}`, request);
  }

  publishCategory(id: number): Observable<AdminLearnCategory> {
    return this.http.post<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories/${id}/publish`, {});
  }

  archiveCategory(id: number): Observable<AdminLearnCategory> {
    return this.http.post<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories/${id}/archive`, {});
  }

  unarchiveCategory(id: number): Observable<AdminLearnCategory> {
    return this.http.post<AdminLearnCategory>(`${this.baseUrl}/api/admin/learn/categories/${id}/unarchive`, {});
  }

  getEntries(categoryId?: number): Observable<{ items: AdminLearnEntryWithCategoryName[]; count: number }> {
    let params = new HttpParams();
    if (categoryId != null) {
      params = params.set('category_id', categoryId);
    }
    return this.http.get<{ items: AdminLearnEntryWithCategoryName[]; count: number }>(`${this.baseUrl}/api/admin/learn/entries`, { params });
  }

  getEntry(id: number): Observable<AdminLearnEntry> {
    return this.http.get<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries/${id}`);
  }

  createEntry(request: SaveLearnEntryRequest): Observable<AdminLearnEntry> {
    return this.http.post<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries`, request);
  }

  updateEntry(id: number, request: SaveLearnEntryRequest): Observable<AdminLearnEntry> {
    return this.http.put<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries/${id}`, request);
  }

  publishEntry(id: number): Observable<AdminLearnEntry> {
    return this.http.post<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries/${id}/publish`, {});
  }

  archiveEntry(id: number): Observable<AdminLearnEntry> {
    return this.http.post<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries/${id}/archive`, {});
  }

  unarchiveEntry(id: number): Observable<AdminLearnEntry> {
    return this.http.post<AdminLearnEntry>(`${this.baseUrl}/api/admin/learn/entries/${id}/unarchive`, {});
  }
}
