import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminHomepagePage } from './admin-homepage-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminHomepagePage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminHomepagePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function selectFile(fixture: ReturnType<typeof TestBed.createComponent>, name = 'hero.jpg') {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input[type="file"]');
    const file = new File(['fake-bytes'], name, { type: 'image/jpeg' });
    Object.defineProperty(input, 'files', { value: [file], configurable: true });
    input.dispatchEvent(new Event('change'));
  }

  it('shows the plain-language empty state when no hero image is configured, with no technical storage terms', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: null });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No hero image selected.');
    expect(text).toContain('The standard ONE77 hero will be used.');
    expect(text).not.toContain('HeroImagePath');
    expect(text).not.toMatch(/physical path|file system|media root/i);
  });

  it('shows the current preview and Replace/Remove actions when a hero image exists', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: '/media/hero-existing.jpg' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const img: HTMLImageElement = compiled.querySelector('.preview-image')!;
    expect(img.src).toContain('/media/hero-existing.jpg');
    const text = compiled.textContent ?? '';
    expect(text).toContain('Replace image');
    expect(text).toContain('Remove image');
  });

  it('previews a selected file before saving, without uploading it yet', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: null });
    fixture.detectChanges();

    selectFile(fixture);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('save to publish it');
    httpMock.expectNone('/api/admin/homepage/hero-image');
  });

  it('uploads the selected file on Save and shows the newly saved image', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: null });
    fixture.detectChanges();

    selectFile(fixture);
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim().startsWith('Save'),
    ) as HTMLButtonElement;
    saveButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/homepage/hero-image' && r.method === 'POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ hero_image_url: '/media/hero-new123.jpg' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const img: HTMLImageElement = compiled.querySelector('.preview-image')!;
    expect(img.src).toContain('/media/hero-new123.jpg');
    expect((compiled.textContent ?? '')).not.toContain('save to publish it');
  });

  it('shows a plain-language error if the upload fails, without leaking a raw server error', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: null });
    fixture.detectChanges();

    selectFile(fixture);
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim().startsWith('Save'),
    ) as HTMLButtonElement;
    saveButton.click();

    httpMock
      .expectOne((r) => r.url === '/api/admin/homepage/hero-image' && r.method === 'POST')
      .flush({ error: 'Please upload a JPEG, PNG, or WebP image.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Unable to save this image. Please try again.');
  });

  it('cancelling a selection discards it and returns to the current saved state', () => {
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: '/media/hero-existing.jpg' });
    fixture.detectChanges();

    selectFile(fixture);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent ?? '').toContain('save to publish it');

    const cancelButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Cancel',
    ) as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect((compiled.textContent ?? '')).not.toContain('save to publish it');
    const img: HTMLImageElement = compiled.querySelector('.preview-image')!;
    expect(img.src).toContain('/media/hero-existing.jpg');
  });

  it('removing the image, once confirmed, clears it and restores the "no image" state', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: '/media/hero-existing.jpg' });
    fixture.detectChanges();

    const removeButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Remove image',
    ) as HTMLButtonElement;
    removeButton.click();

    httpMock.expectOne((r) => r.url === '/api/admin/homepage/hero-image' && r.method === 'DELETE').flush({ hero_image_url: null });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No hero image selected.');
  });

  it('does not remove the image if the confirmation is declined', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const fixture = TestBed.createComponent(AdminHomepagePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/homepage').flush({ hero_image_url: '/media/hero-existing.jpg' });
    fixture.detectChanges();

    const removeButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Remove image',
    ) as HTMLButtonElement;
    removeButton.click();

    httpMock.expectNone((r) => r.url === '/api/admin/homepage/hero-image');
  });
});
