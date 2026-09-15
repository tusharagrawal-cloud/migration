import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { LearnCategory, LearnCategoryDetail } from '../models/learn.models';

@Injectable({ providedIn: 'root' })
export class LearnService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getCategories(): Observable<LearnCategory[]> {
    return this.http.get<LearnCategory[]>(`${this.baseUrl}/api/learn/categories`);
  }

  getCategoryBySlug(slug: string): Observable<LearnCategoryDetail> {
    return this.http.get<LearnCategoryDetail>(`${this.baseUrl}/api/learn/categories/${encodeURIComponent(slug)}`);
  }
}
