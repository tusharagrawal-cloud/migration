import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { WebinarEvent, WebinarRegistrationRequest, WebinarRegistrationResult } from '../models/webinar.models';

@Injectable({ providedIn: 'root' })
export class WebinarService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getUpcomingEvents(): Observable<WebinarEvent[]> {
    return this.http.get<WebinarEvent[]>(`${this.baseUrl}/api/webinar/events`);
  }

  register(request: WebinarRegistrationRequest): Observable<WebinarRegistrationResult> {
    return this.http.post<WebinarRegistrationResult>(`${this.baseUrl}/api/webinar/register`, request);
  }
}
