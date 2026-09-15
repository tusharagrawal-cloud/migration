import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminLearnEntriesPage } from './admin-learn-entries-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminLearnEntriesPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLearnEntriesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists articles with their category name and status, using a plain textarea (no rich-text editor)', () => {
    const fixture = TestBed.createComponent(AdminLearnEntriesPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/learn/categories').flush({
      items: [{ id: 1, name: 'Getting Started', slug: 'getting-started', description: '', sort_priority: 0, status: 'Live', created_at: '', updated_at: '', entry_count: 1 }],
      count: 1,
    });
    httpMock.expectOne((r) => r.url === '/api/admin/learn/entries').flush({
      items: [{ id: 1, category_id: 1, title: 'Choosing an airgun', body: 'Plain text body.', sort_priority: 0, status: 'Live', created_at: '', updated_at: '', category_name: 'Getting Started' }],
      count: 1,
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Choosing an airgun');
    expect(text).toContain('Getting Started');
    expect(fixture.nativeElement.querySelector('iframe, [contenteditable]')).toBeNull();
  });

  it('shows a plain-language message if archiving an article fails, instead of failing silently', () => {
    const fixture = TestBed.createComponent(AdminLearnEntriesPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/learn/categories').flush({
      items: [{ id: 1, name: 'Getting Started', slug: 'getting-started', description: '', sort_priority: 0, status: 'Live', created_at: '', updated_at: '', entry_count: 1 }],
      count: 1,
    });
    httpMock.expectOne((r) => r.url === '/api/admin/learn/entries').flush({
      items: [{ id: 1, category_id: 1, title: 'Choosing an airgun', body: 'Plain text body.', sort_priority: 0, status: 'Live', created_at: '', updated_at: '', category_name: 'Getting Started' }],
      count: 1,
    });
    fixture.detectChanges();

    const archiveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Hide from Website',
    ) as HTMLButtonElement;
    archiveButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/learn/entries/1/archive');
    req.flush({ error: 'Unable to update.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to update this article. Please try again.');
  });
});
