import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminWebinarService } from '../../core/services/admin-webinar.service';
import { AdminWebinarEvent, SaveWebinarEventRequest, WebinarEventStatus } from '../../core/models/admin-webinar.models';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';
type SaveState = 'idle' | 'saving' | 'error';

@Component({
  selector: 'app-admin-webinar-events-page',
  imports: [FormsModule, RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <div class="header-row">
      <div>
        <h1>Webinars</h1>
        <p class="lede">Schedule events and view who registered.</p>
      </div>
      <button type="button" class="admin-btn admin-btn-primary" (click)="startCreate()">+ Add webinar</button>
    </div>

    @if (formOpen()) {
      <app-admin-card>
        <h2>{{ editingId() ? 'Edit webinar' : 'New webinar' }}</h2>
        <div class="form">
          <label class="admin-field">
            Title
            <input type="text" [(ngModel)]="title" name="webTitle" />
          </label>
          <div class="field-pair">
            <label class="admin-field">
              Date &amp; time
              <input type="datetime-local" [(ngModel)]="eventDateTimeLocal" name="webDateTime" />
            </label>
            <label class="admin-field">
              Status
              <select [(ngModel)]="status" name="webStatus">
                <option value="Active">Active</option>
                <option value="Cancelled">Cancelled</option>
                <option value="Completed">Completed</option>
              </select>
            </label>
          </div>
          <label class="admin-field">
            Join link (optional)
            <input type="url" [(ngModel)]="joinLink" name="webJoinLink" placeholder="https://…" />
          </label>
          @if (saveState() === 'error') {
            <p class="save-error" role="alert">Unable to save. Please check the date and try again.</p>
          }
          <div class="form-actions">
            <button type="button" class="admin-btn admin-btn-secondary" (click)="cancelForm()">Cancel</button>
            <button type="button" class="admin-btn admin-btn-primary" [disabled]="saveState() === 'saving'" (click)="save()">
              {{ saveState() === 'saving' ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </div>
      </app-admin-card>
    }

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading webinars…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load webinars. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (actionError()) {
          <p class="save-error" role="alert">{{ actionError() }}</p>
        }
        @if (events().length === 0) {
          <app-admin-empty heading="No webinars yet" />
        } @else {
          <div class="list">
            @for (event of events(); track event.event_id) {
              <app-admin-card>
                <div class="row">
                  <div class="info">
                    <p class="title">{{ event.title }}</p>
                    <p class="meta">{{ formatDate(event.event_date_time) }}</p>
                  </div>
                  <span class="admin-badge" [class]="statusBadgeClass(event.status)">{{ event.status }}</span>
                  <div class="actions">
                    <a [routerLink]="['/admin/webinars', event.event_id, 'registrations']" class="admin-btn admin-btn-secondary admin-btn-sm">
                      Registrations
                    </a>
                    <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="startEdit(event)">Edit</button>
                    @if (event.status === 'Active') {
                      <button type="button" class="admin-btn admin-btn-danger admin-btn-sm" (click)="cancelEvent(event)">Cancel</button>
                    }
                  </div>
                </div>
              </app-admin-card>
            }
          </div>
        }
      }
    }
  `,
  styles: [
    `
      .header-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: var(--admin-space-4);
        margin-bottom: var(--admin-space-5);
        flex-wrap: wrap;
      }
      h1 {
        font-size: 1.5rem;
        margin-bottom: var(--admin-space-2);
      }
      .lede {
        color: var(--admin-text-muted);
        margin: 0;
      }
      h2 {
        font-size: 1.0625rem;
        margin: 0 0 var(--admin-space-4);
      }
      .form {
        display: grid;
        gap: var(--admin-space-4);
        max-width: 480px;
      }
      .field-pair {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--admin-space-4);
      }
      .form-actions {
        display: flex;
        gap: var(--admin-space-3);
      }
      .save-error {
        color: var(--admin-danger);
        margin: 0;
        font-size: 0.875rem;
      }
      .list {
        display: grid;
        gap: var(--admin-space-3);
        margin-top: var(--admin-space-5);
      }
      .row {
        display: flex;
        align-items: center;
        gap: var(--admin-space-4);
        flex-wrap: wrap;
      }
      .info {
        flex: 1;
        min-width: 0;
      }
      .title {
        margin: 0;
        font-weight: 500;
      }
      .meta {
        margin: var(--admin-space-1) 0 0;
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
      }
      .actions {
        display: flex;
        gap: var(--admin-space-2);
        flex-wrap: wrap;
      }
      a.admin-btn {
        text-decoration: none;
      }

      @media (max-width: 600px) {
        .field-pair {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AdminWebinarEventsPage implements OnInit {
  private readonly webinarService = inject(AdminWebinarService);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected actionError = signal<string | null>(null);
  protected events = signal<AdminWebinarEvent[]>([]);
  protected formOpen = signal(false);
  protected editingId = signal<number | null>(null);

  protected title = '';
  protected eventDateTimeLocal = '';
  protected joinLink = '';
  protected status: WebinarEventStatus = 'Active';

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.webinarService.getEvents().subscribe({
      next: (events) => {
        this.events.set(events);
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.title = '';
    this.eventDateTimeLocal = '';
    this.joinLink = '';
    this.status = 'Active';
    this.formOpen.set(true);
  }

  protected startEdit(event: AdminWebinarEvent): void {
    this.editingId.set(event.event_id);
    this.title = event.title;
    this.eventDateTimeLocal = event.event_date_time.slice(0, 16);
    this.joinLink = event.join_link ?? '';
    this.status = event.status;
    this.formOpen.set(true);
  }

  protected cancelForm(): void {
    this.formOpen.set(false);
    this.saveState.set('idle');
  }

  protected save(): void {
    if (!this.title.trim() || !this.eventDateTimeLocal) {
      return;
    }
    this.saveState.set('saving');
    const request: SaveWebinarEventRequest = {
      title: this.title.trim(),
      event_date_time: new Date(this.eventDateTimeLocal).toISOString(),
      join_link: this.joinLink.trim() || null,
      status: this.status,
    };

    const id = this.editingId();
    const save$ = id ? this.webinarService.updateEvent(id, request) : this.webinarService.createEvent(request);
    save$.subscribe({
      next: () => {
        this.formOpen.set(false);
        this.saveState.set('idle');
        this.load();
      },
      error: () => this.saveState.set('error'),
    });
  }

  protected cancelEvent(event: AdminWebinarEvent): void {
    const confirmed = window.confirm(`Cancel "${event.title}"? This tells customers the event is no longer happening.`);
    if (!confirmed) {
      return;
    }
    this.actionError.set(null);
    this.webinarService
      .updateEvent(event.event_id, {
        title: event.title,
        event_date_time: event.event_date_time,
        join_link: event.join_link,
        status: 'Cancelled',
      })
      .subscribe({
        next: () => this.load(),
        error: () => this.actionError.set('Unable to cancel this event. Please try again.'),
      });
  }

  protected statusBadgeClass(status: string): string {
    switch (status) {
      case 'Active':
        return 'admin-badge-success';
      case 'Cancelled':
        return 'admin-badge-danger';
      default:
        return 'admin-badge';
    }
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
  }
}
