import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { WebinarPage } from './webinar-page';
import { WebinarEvent } from '../../core/models/webinar.models';
import { API_BASE_URL } from '../../core/config/api-config';

describe('WebinarPage', () => {
  let httpMock: HttpTestingController;
  const upcomingEvents: WebinarEvent[] = [
    { event_id: 1, title: 'Intro to Airgunning', event_date_time: '2026-09-01T18:00:00Z', join_link: null },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WebinarPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function createAndSelectEvent() {
    const fixture = TestBed.createComponent(WebinarPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/webinar/events').flush(upcomingEvents);
    fixture.detectChanges();

    const eventButton: HTMLButtonElement = fixture.nativeElement.querySelector('.event-card');
    eventButton.click();
    fixture.detectChanges();
    return fixture;
  }

  function fillRegistrationForm(fixture: ReturnType<typeof createAndSelectEvent>): void {
    const setValue = (selector: string, value: string) => {
      const input: HTMLInputElement = fixture.nativeElement.querySelector(selector);
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    setValue('input[formcontrolname="name"]', 'Jane Doe');
    setValue('input[formcontrolname="email"]', 'jane@example.com');
    setValue('input[formcontrolname="phone"]', '555-0100');
    fixture.detectChanges();
  }

  it('renders real upcoming events from the Webinar API', () => {
    const fixture = TestBed.createComponent(WebinarPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/webinar/events').flush(upcomingEvents);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Intro to Airgunning');
  });

  it('shows validation messages when submitting an empty registration form', () => {
    const fixture = createAndSelectEvent();
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Please enter your name.');
    expect(text).toContain('Please enter a valid email address.');
    expect(text).toContain('Please enter your phone number.');
  });

  it('shows a confirmation state after a successful registration', () => {
    const fixture = createAndSelectEvent();
    fillRegistrationForm(fixture);

    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/webinar/register');
    expect(req.request.method).toBe('POST');
    req.flush({
      registration_id: 5,
      event_id: 1,
      event_title: 'Intro to Airgunning',
      event_date_time: '2026-09-01T18:00:00Z',
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain("You're registered for Intro to Airgunning");
  });

  it('shows a plain-language error when registration submission fails', () => {
    const fixture = createAndSelectEvent();
    fillRegistrationForm(fixture);

    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    httpMock.expectOne('/api/webinar/register').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain("couldn't submit your registration");
    expect(text).not.toContain('500');
  });
});
