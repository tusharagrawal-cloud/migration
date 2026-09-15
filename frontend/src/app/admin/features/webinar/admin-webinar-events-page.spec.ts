import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminWebinarEventsPage } from './admin-webinar-events-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminWebinarEventsPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminWebinarEventsPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists events with a formatted date and status, and links to registrations', () => {
    const fixture = TestBed.createComponent(AdminWebinarEventsPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/webinar/events').flush([
      { event_id: 1, title: 'Intro to Airgunning', event_date_time: '2026-09-15T18:00:00Z', join_link: null, status: 'Active', created_at: '', updated_at: '' },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Intro to Airgunning');
    expect(text).toContain('Active');
    const regLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a[href*="registrations"]');
    expect(regLink).toBeTruthy();
  });

  it('creates a new webinar via the inline form', () => {
    const fixture = TestBed.createComponent(AdminWebinarEventsPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/webinar/events').flush([]);
    fixture.detectChanges();

    const addButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find((b: any) =>
      b.textContent.includes('Add webinar'),
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const titleInput: HTMLInputElement = fixture.nativeElement.querySelector('input[name="webTitle"]');
    titleInput.value = 'New Session';
    titleInput.dispatchEvent(new Event('input'));
    const dateInput: HTMLInputElement = fixture.nativeElement.querySelector('input[name="webDateTime"]');
    dateInput.value = '2026-10-01T18:00';
    dateInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Save',
    ) as HTMLButtonElement;
    saveButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/webinar/events' && r.method === 'POST');
    expect(req.request.body.title).toBe('New Session');
    req.flush({ event_id: 2, title: 'New Session', event_date_time: '2026-10-01T18:00:00Z', join_link: null, status: 'Active', created_at: '', updated_at: '' });

    httpMock.expectOne('/api/admin/webinar/events').flush([]);
  });

  it('shows a plain-language message if cancelling an event fails, instead of failing silently', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = TestBed.createComponent(AdminWebinarEventsPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/webinar/events').flush([
      { event_id: 1, title: 'Intro to Airgunning', event_date_time: '2026-09-15T18:00:00Z', join_link: null, status: 'Active', created_at: '', updated_at: '' },
    ]);
    fixture.detectChanges();

    const cancelButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Cancel',
    ) as HTMLButtonElement;
    cancelButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/webinar/events/1' && r.method === 'PUT');
    req.flush({ error: 'Unable to update.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to cancel this event. Please try again.');
  });
});
