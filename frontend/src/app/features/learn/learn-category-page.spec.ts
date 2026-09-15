import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { LearnCategoryPage } from './learn-category-page';
import { LearnCategoryDetail } from '../../core/models/learn.models';
import { API_BASE_URL } from '../../core/config/api-config';

describe('LearnCategoryPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LearnCategoryPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ slug: 'getting-started' }) } },
        },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders the category name and its entries inline', () => {
    const fixture = TestBed.createComponent(LearnCategoryPage);
    fixture.detectChanges();

    const detail: LearnCategoryDetail = {
      category: { id: 1, name: 'Getting Started', slug: 'getting-started', description: 'The basics.' },
      entries: [{ id: 10, title: 'Choosing your first airgun', body: 'Body text.' }],
    };
    httpMock.expectOne('/api/learn/categories/getting-started').flush(detail);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Getting Started');
    expect(text).toContain('Choosing your first airgun');
  });
});
