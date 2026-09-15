import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminLearnCategoriesPage } from './admin-learn-categories-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminLearnCategoriesPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLearnCategoriesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('preserves Draft/Live/Hidden status vocabulary — never Published/Archived', () => {
    const fixture = TestBed.createComponent(AdminLearnCategoriesPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/learn/categories').flush({
      items: [{ id: 1, name: 'Getting Started', slug: 'getting-started', description: 'Basics', sort_priority: 0, status: 'Live', created_at: '', updated_at: '', entry_count: 2 }],
      count: 1,
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Live');
    expect(text).not.toContain('Published');
    expect(text).not.toContain('Archived');
  });

  it('creates a new category via the inline form', () => {
    const fixture = TestBed.createComponent(AdminLearnCategoriesPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/learn/categories').flush({ items: [], count: 0 });
    fixture.detectChanges();

    const addButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find((b: any) =>
      b.textContent.includes('Add category'),
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('input[name="catName"]');
    nameInput.value = 'Safety';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Save',
    ) as HTMLButtonElement;
    saveButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/learn/categories' && r.method === 'POST');
    expect(req.request.body.name).toBe('Safety');
    req.flush({ id: 2, name: 'Safety', slug: 'safety', description: '', sort_priority: 0, status: 'Draft', created_at: '', updated_at: '' });

    httpMock.expectOne('/api/admin/learn/categories').flush({ items: [], count: 0 });
  });

  it('shows a plain-language message if publishing a category fails, instead of failing silently', () => {
    const fixture = TestBed.createComponent(AdminLearnCategoriesPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/learn/categories').flush({
      items: [{ id: 1, name: 'Getting Started', slug: 'getting-started', description: '', sort_priority: 0, status: 'Draft', created_at: '', updated_at: '', entry_count: 0 }],
      count: 1,
    });
    fixture.detectChanges();

    const publishButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Show on Website',
    ) as HTMLButtonElement;
    publishButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/learn/categories/1/publish');
    req.flush({ error: 'Unable to update.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to update this category. Please try again.');
  });
});
