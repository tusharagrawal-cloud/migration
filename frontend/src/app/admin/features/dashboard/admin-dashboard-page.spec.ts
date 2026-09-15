import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminDashboardPage } from './admin-dashboard-page';

describe('AdminDashboardPage', () => {
  it('links to every top-level Admin area as a navigation starting point', async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardPage],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminDashboardPage);
    fixture.detectChanges();

    const hrefs = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a: any) => a.getAttribute('href'));
    expect(hrefs).toContain('/admin/products');
    expect(hrefs).toContain('/admin/match');
    expect(hrefs).toContain('/admin/bundles');
    expect(hrefs).toContain('/admin/learn');
    expect(hrefs).toContain('/admin/webinars');
  });
});
