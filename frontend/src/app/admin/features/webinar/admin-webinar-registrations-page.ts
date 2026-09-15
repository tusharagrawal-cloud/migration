import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminWebinarService } from '../../core/services/admin-webinar.service';
import { AdminWebinarEvent, WebinarRegistrationSummary } from '../../core/models/admin-webinar.models';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-admin-webinar-registrations-page',
  imports: [RouterLink, AdminLoading, AdminEmpty, AdminError],
  template: `
    <a routerLink="/admin/webinars" class="back-link">&larr; All webinars</a>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading registrations…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load registrations. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        <h1>Registrations — {{ event()?.title }}</h1>
        <p class="lede">{{ registrations().length }} registration(s)</p>

        @if (registrations().length === 0) {
          <app-admin-empty heading="No registrations yet" />
        } @else {
          <div class="table-wrap">
            <table class="admin-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Phone</th>
                  <th>Registered</th>
                </tr>
              </thead>
              <tbody>
                @for (reg of registrations(); track reg.registration_id) {
                  <tr>
                    <td>{{ reg.name }}</td>
                    <td>{{ reg.email }}</td>
                    <td>{{ reg.phone }}</td>
                    <td>{{ formatDate(reg.registered_at) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    }
  `,
  styles: [
    `
      .back-link {
        display: inline-block;
        margin-bottom: var(--admin-space-4);
        color: var(--admin-text-muted);
        text-decoration: none;
        font-size: 0.875rem;
      }
      h1 {
        font-size: 1.375rem;
        margin-bottom: var(--admin-space-1);
      }
      .lede {
        color: var(--admin-text-muted);
        margin-bottom: var(--admin-space-5);
      }
      .table-wrap {
        overflow-x: auto;
      }
      .admin-table {
        width: 100%;
        border-collapse: collapse;
        background: var(--admin-surface);
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius);
        overflow: hidden;
        font-size: 0.875rem;
      }
      .admin-table th,
      .admin-table td {
        text-align: left;
        padding: var(--admin-space-3) var(--admin-space-4);
        border-bottom: 1px solid var(--admin-border);
        white-space: nowrap;
      }
      .admin-table th {
        color: var(--admin-text-muted);
        font-weight: 500;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.03em;
      }
      .admin-table tr:last-child td {
        border-bottom: none;
      }
    `,
  ],
})
export class AdminWebinarRegistrationsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly webinarService = inject(AdminWebinarService);

  protected state = signal<LoadState>('loading');
  protected event = signal<AdminWebinarEvent | null>(null);
  protected registrations = signal<WebinarRegistrationSummary[]>([]);

  private eventId = 0;

  ngOnInit(): void {
    this.eventId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.webinarService.getEvent(this.eventId).subscribe({
      next: (event) => {
        this.event.set(event);
        this.webinarService.getRegistrations(this.eventId).subscribe({
          next: (registrations) => {
            this.registrations.set(registrations);
            this.state.set('loaded');
          },
          error: () => this.state.set('error'),
        });
      },
      error: () => this.state.set('error'),
    });
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
  }
}
