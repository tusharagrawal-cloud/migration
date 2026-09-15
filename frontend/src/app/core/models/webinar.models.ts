// Mirrors the ONE77 Webinar public API response shapes exactly
// (migration/backend/src/One77.Api.DevHost/Webinar — Milestone 5).
// EventDateTime is UTC throughout, per the backend's documented convention.

export interface WebinarEvent {
  event_id: number;
  title: string;
  event_date_time: string;
  join_link: string | null;
}

export interface WebinarRegistrationRequest {
  event_id: number;
  name: string;
  email: string;
  phone: string;
}

export interface WebinarRegistrationResult {
  registration_id: number;
  event_id: number;
  event_title: string;
  event_date_time: string;
}
