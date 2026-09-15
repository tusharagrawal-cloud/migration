import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { PublicEnrichment } from '../models/enrichment.models';

@Injectable({ providedIn: 'root' })
export class EnrichmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /**
   * shopifyProductId is passed as a query parameter, never a path segment —
   * see match.service.ts and migration/docs/MATCH_PARITY_REPORT.md,
   * "Bug found and fixed," for why (GIDs contain literal "/").
   */
  getEnrichmentForProduct(shopifyProductId: string): Observable<PublicEnrichment> {
    const params = new HttpParams().set('shopify_product_id', shopifyProductId);
    return this.http.get<PublicEnrichment>(`${this.baseUrl}/api/products/enrichment`, { params });
  }
}
