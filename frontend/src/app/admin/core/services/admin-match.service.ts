import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import {
  MatchAirgunSummaryItem,
  MatchBulkResult,
  MatchCandidateItem,
  MatchCompleteness,
  MatchRelationshipView,
  PatchMatchRelationshipRequest,
  UpsertMatchRelationshipRequest,
} from '../models/admin-match.models';

/**
 * Every Shopify Product ID here is a query parameter, never a path segment
 * — canonical GIDs contain literal "/" characters that break ASP.NET Core
 * route matching. See migration/docs/MATCH_PARITY_REPORT.md, "Bug found and
 * fixed," and match.service.ts (the public-site equivalent of this rule).
 */
@Injectable({ providedIn: 'root' })
export class AdminMatchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAirguns(): Observable<{ items: MatchAirgunSummaryItem[]; count: number }> {
    return this.http.get<{ items: MatchAirgunSummaryItem[]; count: number }>(`${this.baseUrl}/api/admin/match/airguns`);
  }

  getRelationships(shopifyProductId: string, targetCategory?: string): Observable<{ items: MatchRelationshipView[]; count: number }> {
    let params = new HttpParams().set('shopify_product_id', shopifyProductId);
    if (targetCategory) {
      params = params.set('target_category', targetCategory);
    }
    return this.http.get<{ items: MatchRelationshipView[]; count: number }>(`${this.baseUrl}/api/admin/match/relationships`, { params });
  }

  getCandidates(shopifyProductId: string, targetCategory: string, calibre?: string, weight?: number): Observable<{ items: MatchCandidateItem[]; count: number }> {
    let params = new HttpParams().set('shopify_product_id', shopifyProductId).set('target_category', targetCategory);
    if (calibre) {
      params = params.set('calibre', calibre);
    }
    if (weight != null) {
      params = params.set('weight', weight);
    }
    return this.http.get<{ items: MatchCandidateItem[]; count: number }>(`${this.baseUrl}/api/admin/match/candidates`, { params });
  }

  upsertRelationship(request: UpsertMatchRelationshipRequest): Observable<MatchRelationshipView> {
    return this.http.post<MatchRelationshipView>(`${this.baseUrl}/api/admin/match/relationships`, request);
  }

  patchRelationship(id: number, request: PatchMatchRelationshipRequest): Observable<MatchRelationshipView> {
    return this.http.patch<MatchRelationshipView>(`${this.baseUrl}/api/admin/match/relationships/${id}`, request);
  }

  deleteRelationship(id: number): Observable<{ ok: boolean }> {
    return this.http.delete<{ ok: boolean }>(`${this.baseUrl}/api/admin/match/relationships/${id}`);
  }

  bulkMark(sourceShopifyProductId: string, targetShopifyProductIds: string[], status: string): Observable<MatchBulkResult> {
    return this.http.post<MatchBulkResult>(`${this.baseUrl}/api/admin/match/bulk`, {
      source_shopify_product_id: sourceShopifyProductId,
      target_shopify_product_ids: targetShopifyProductIds,
      status,
    });
  }

  getSummary(shopifyProductId: string): Observable<MatchCompleteness> {
    const params = new HttpParams().set('shopify_product_id', shopifyProductId);
    return this.http.get<MatchCompleteness>(`${this.baseUrl}/api/admin/match/summary`, { params });
  }
}
