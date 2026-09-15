// Mirrors One77.Core.Webinar's admin-facing shapes exactly (Milestone 5 / 10).

export type WebinarEventStatus = 'Active' | 'Cancelled' | 'Completed';

export interface AdminWebinarEvent {
  event_id: number;
  title: string;
  event_date_time: string;
  join_link: string | null;
  status: WebinarEventStatus;
  created_at: string;
  updated_at: string;
}

export interface SaveWebinarEventRequest {
  title: string;
  event_date_time: string;
  join_link: string | null;
  status: WebinarEventStatus;
}

export interface WebinarRegistrationSummary {
  registration_id: number;
  name: string;
  email: string;
  phone: string;
  registered_at: string;
}
