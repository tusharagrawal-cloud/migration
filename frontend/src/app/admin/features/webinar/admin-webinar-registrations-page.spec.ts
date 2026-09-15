import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AdminWebinarRegistrationsPage } from './admin-webinar-registrations-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminWebinarRegistrationsPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminWebinarRegistrationsPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows name, email, phone, and registration time for each registrant', () => {
    const fixture = TestBed.createComponent(AdminWebinarRegistrationsPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/webinar/events/1').flush({ event_id: 1, title: 'Intro to Airgunning', event_date_time: '2026-09-15T18:00:00Z', join_link: null, status: 'Active', created_at: '', updated_at: '' });
    httpMock.expectOne('/api/admin/webinar/events/1/registrations').flush([
      { registration_id: 1, name: 'Jane Doe', email: 'jane@example.com', phone: '555-0100', registered_at: '2026-08-01T10:00:00Z' },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Intro to Airgunning');
    expect(text).toContain('Jane Doe');
    expect(text).toContain('jane@example.com');
    expect(text).toContain('555-0100');
  });

  it('shows an empty state with no registrations', () => {
    const fixture = TestBed.createComponent(AdminWebinarRegistrationsPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/webinar/events/1').flush({ event_id: 1, title: 'Intro to Airgunning', event_date_time: '2026-09-15T18:00:00Z', join_link: null, status: 'Active', created_at: '', updated_at: '' });
    httpMock.expectOne('/api/admin/webinar/events/1/registrations').flush([]);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No registrations yet');
  });
});
