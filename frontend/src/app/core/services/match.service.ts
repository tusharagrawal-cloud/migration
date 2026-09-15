import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { PublicMatchResult } from '../models/match.models';

@Injectable({ providedIn: 'root' })
export class MatchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /**
   * shopifyProductId is passed as a query parameter, never a path segment —
   * canonical Shopify GIDs contain literal "/" characters that break
   * ASP.NET Core route matching. See migration/docs/MATCH_PARITY_REPORT.md,
   * "Bug found and fixed."
   */
  getMatchesForProduct(shopifyProductId: string, useCase?: string): Observable<PublicMatchResult> {
    let params = new HttpParams().set('shopify_product_id', shopifyProductId);
    if (useCase) {
      params = params.set('use_case', useCase);
    }
    return this.http.get<PublicMatchResult>(`${this.baseUrl}/api/products/matches`, { params });
  }
}
