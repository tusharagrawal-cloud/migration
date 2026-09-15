import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WebinarService } from '../../core/services/webinar.service';
import { WebinarEvent, WebinarRegistrationResult } from '../../core/models/webinar.models';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorState } from '../../shared/ui/error-state';

type EventsState = 'loading' | 'loaded' | 'empty' | 'error';
type FormState = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-webinar-page',
  imports: [ReactiveFormsModule, PageContainer, LoadingState, EmptyState, ErrorState],
  template: `
    <app-page-container>
      <div class="intro">
        <p class="eyebrow">Human support</p>
        <h1>Ask us live</h1>
        <p class="lede">
          A free session with the ONE77 team. Bring any question about airguns, pellets or setup — pick a time below.
        </p>
      </div>

      @switch (eventsState()) {
        @case ('loading') {
          <app-loading-state label="Loading upcoming webinars…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load upcoming webinars right now." (retry)="loadEvents()" />
        }
        @case ('empty') {
          <app-empty-state heading="No webinars scheduled" body="Check back soon." />
        }
        @case ('loaded') {
          <div class="events">
            @for (event of events(); track event.event_id) {
              <button
                type="button"
                class="event-card"
                [class.selected]="selectedEvent()?.event_id === event.event_id"
                (click)="selectEvent(event)"
              >
                <span class="dot" aria-hidden="true"></span>
                <span>
                  <p class="event-title">{{ event.title }}</p>
                  <p class="event-date">{{ formatDate(event.event_date_time) }}</p>
                </span>
              </button>
            }
          </div>
        }
      }

      @if (selectedEvent()) {
        <section class="registration" aria-labelledby="register-heading">
          <h2 id="register-heading">Register for {{ selectedEvent()!.title }}</h2>

          @if (formState() === 'success') {
            <div class="confirmation-panel">
              <p class="confirmation">
                You're registered for {{ confirmation()!.event_title }}. We'll be in touch with the details.
              </p>
            </div>
          } @else {
            <form [formGroup]="form" (ngSubmit)="submit()">
              <label class="field">
                Name
                <input type="text" formControlName="name" />
                @if (showError('name')) {
                  <span class="field-error">Please enter your name.</span>
                }
              </label>
              <label class="field">
                Email
                <input type="email" formControlName="email" />
                @if (showError('email')) {
                  <span class="field-error">Please enter a valid email address.</span>
                }
              </label>
              <label class="field">
                Phone
                <input type="tel" formControlName="phone" />
                @if (showError('phone')) {
                  <span class="field-error">Please enter your phone number.</span>
                }
              </label>

              @if (formState() === 'error') {
                <p class="field-error">We couldn't submit your registration. Please try again.</p>
              }

              <button type="submit" class="submit-btn" [disabled]="formState() === 'submitting'">
                {{ formState() === 'submitting' ? 'Submitting…' : 'Reserve My Seat' }}
              </button>
            </form>
          }
        </section>
      }
    </app-page-container>
  `,
  styles: [
    `
      .intro {
        max-width: 60ch;
        margin-bottom: var(--space-7);
      }
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.18em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-2);
      }
      h1 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-2xl);
        margin: 0 0 var(--space-3);
      }
      .lede {
        color: var(--color-text-muted);
        margin: 0;
      }
      .events {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--space-4);
        margin-bottom: var(--space-8);
      }
      .event-card {
        all: unset;
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
        cursor: pointer;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-4) var(--space-5);
        transition: border-color var(--transition-base);
      }
      .event-card:hover {
        border-color: var(--color-accent);
      }
      .event-card.selected {
        border-color: var(--color-accent);
        background: var(--color-accent-soft);
      }
      .dot {
        margin-top: 6px;
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: var(--color-accent);
        flex-shrink: 0;
      }
      .event-title {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
      }
      .event-date {
        margin: var(--space-1) 0 0;
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
      .registration {
        max-width: 420px;
      }
      .registration h2 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-lg);
        margin: 0 0 var(--space-5);
      }
      form {
        display: grid;
        gap: var(--space-4);
      }
      .field {
        display: grid;
        gap: var(--space-2);
        font-size: var(--font-size-sm);
        color: var(--color-text-muted);
      }
      .field input {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        padding: var(--space-3);
        color: var(--color-text);
        font-size: var(--font-size-base);
        font-family: var(--font-family-body);
      }
      .field input:focus-visible {
        border-color: var(--color-accent);
      }
      .field-error {
        color: var(--color-danger);
        font-size: var(--font-size-xs);
      }
      .submit-btn {
        justify-self: start;
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        border: none;
        border-radius: var(--radius-sm);
        padding: var(--space-3) var(--space-6);
        cursor: pointer;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-sm);
      }
      .submit-btn:hover:not(:disabled) {
        background: var(--color-accent-strong);
      }
      .submit-btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .confirmation-panel {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-left: 3px solid var(--color-accent);
        border-radius: var(--radius-md);
        padding: var(--space-5);
      }
      .confirmation {
        margin: 0;
      }
    `,
  ],
})
export class WebinarPage implements OnInit {
  private readonly webinarService = inject(WebinarService);
  private readonly fb = inject(FormBuilder);

  protected eventsState = signal<EventsState>('loading');
  protected events = signal<WebinarEvent[]>([]);
  protected selectedEvent = signal<WebinarEvent | null>(null);
  protected formState = signal<FormState>('idle');
  protected confirmation = signal<WebinarRegistrationResult | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadEvents();
  }

  protected loadEvents(): void {
    this.eventsState.set('loading');
    this.webinarService.getUpcomingEvents().subscribe({
      next: (events) => {
        this.events.set(events);
        this.eventsState.set(events.length ? 'loaded' : 'empty');
      },
      error: () => this.eventsState.set('error'),
    });
  }

  protected selectEvent(event: WebinarEvent): void {
    this.selectedEvent.set(event);
    this.formState.set('idle');
    this.form.reset();
  }

  protected showError(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  protected submit(): void {
    if (this.form.invalid || !this.selectedEvent()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formState.set('submitting');
    const { name, email, phone } = this.form.getRawValue();
    this.webinarService
      .register({ event_id: this.selectedEvent()!.event_id, name, email, phone })
      .subscribe({
        next: (result) => {
          this.confirmation.set(result);
          this.formState.set('success');
        },
        error: () => this.formState.set('error'),
      });
  }

  protected formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }
}
