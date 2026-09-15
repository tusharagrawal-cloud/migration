import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LearnLandingPage } from './learn-landing-page';
import { LearnCategory } from '../../core/models/learn.models';
import { API_BASE_URL } from '../../core/config/api-config';

describe('LearnLandingPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LearnLandingPage],
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

  it('renders categories returned by the real Learn API', () => {
    const fixture = TestBed.createComponent(LearnLandingPage);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/learn/categories');
    expect(req.request.method).toBe('GET');
    const categories: LearnCategory[] = [
      { id: 1, name: 'Getting Started', slug: 'getting-started', description: 'The basics.' },
    ];
    req.flush(categories);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Getting Started');
  });

  it('shows an empty state when there are no categories', () => {
    const fixture = TestBed.createComponent(LearnLandingPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/learn/categories').flush([]);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No categories yet');
  });

  it('shows a plain-language error state when the API call fails', () => {
    const fixture = TestBed.createComponent(LearnLandingPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/learn/categories').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain("couldn't load Learn categories");
    expect(text).not.toContain('500');
  });
});
