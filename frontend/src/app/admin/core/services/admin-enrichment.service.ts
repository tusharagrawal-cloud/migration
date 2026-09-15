import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { EnrichmentRecord, SaveEnrichmentRequest } from '../models/admin-enrichment.models';

@Injectable({ providedIn: 'root' })
export class AdminEnrichmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<{ items: EnrichmentRecord[]; count: number }> {
    return this.http.get<{ items: EnrichmentRecord[]; count: number }>(`${this.baseUrl}/api/admin/enrichment`);
  }

  /** shopify_product_id is always a query parameter, never a path segment — GIDs contain literal "/". */
  getByProduct(shopifyProductId: string): Observable<EnrichmentRecord> {
    const params = new HttpParams().set('shopify_product_id', shopifyProductId);
    return this.http.get<EnrichmentRecord>(`${this.baseUrl}/api/admin/enrichment/by-product`, { params });
  }

  create(request: SaveEnrichmentRequest): Observable<EnrichmentRecord> {
    return this.http.post<EnrichmentRecord>(`${this.baseUrl}/api/admin/enrichment`, request);
  }

  update(request: SaveEnrichmentRequest): Observable<EnrichmentRecord> {
    return this.http.put<EnrichmentRecord>(`${this.baseUrl}/api/admin/enrichment`, request);
  }
}
