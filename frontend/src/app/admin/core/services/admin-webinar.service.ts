import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { AdminWebinarEvent, SaveWebinarEventRequest, WebinarRegistrationSummary } from '../models/admin-webinar.models';

@Injectable({ providedIn: 'root' })
export class AdminWebinarService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getEvents(): Observable<AdminWebinarEvent[]> {
    return this.http.get<AdminWebinarEvent[]>(`${this.baseUrl}/api/admin/webinar/events`);
  }

  getEvent(id: number): Observable<AdminWebinarEvent> {
    return this.http.get<AdminWebinarEvent>(`${this.baseUrl}/api/admin/webinar/events/${id}`);
  }

  createEvent(request: SaveWebinarEventRequest): Observable<AdminWebinarEvent> {
    return this.http.post<AdminWebinarEvent>(`${this.baseUrl}/api/admin/webinar/events`, request);
  }

  updateEvent(id: number, request: SaveWebinarEventRequest): Observable<AdminWebinarEvent> {
    return this.http.put<AdminWebinarEvent>(`${this.baseUrl}/api/admin/webinar/events/${id}`, request);
  }

  getRegistrations(eventId: number): Observable<WebinarRegistrationSummary[]> {
    return this.http.get<WebinarRegistrationSummary[]>(`${this.baseUrl}/api/admin/webinar/events/${eventId}/registrations`);
  }
}
