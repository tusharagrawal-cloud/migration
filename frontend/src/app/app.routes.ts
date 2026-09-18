import { Routes } from '@angular/router';
import { HomePage } from './features/home/home-page';
import { ProductListingPage } from './features/product-listing/product-listing-page';
import { ProductDetailPage } from './features/product-detail/product-detail-page';
import { BundlesPage } from './features/bundles/bundles-page';
import { LearnLandingPage } from './features/learn/learn-landing-page';
import { LearnCategoryPage } from './features/learn/learn-category-page';
import { LearnEntryPage } from './features/learn/learn-entry-page';
import { WebinarPage } from './features/webinar/webinar-page';
import { TermsPage } from './features/policies/terms-page';
import { PrivacyPage } from './features/policies/privacy-page';
import { RefundPage } from './features/policies/refund-page';
import { AboutPage } from './features/policies/about-page';
import { NotFoundPage } from './features/not-found/not-found-page';
import { authGuard } from './admin/core/auth.guard';

// The `title` on each route is Angular's built-in per-route document-title
// mechanism (Router applies it to document.title automatically on
// navigation — no extra provider needed). Distinct from `data.title`, which
// a couple of pages also read for their own in-page heading text.
export const routes: Routes = [
  { path: '', component: HomePage, title: 'ONE77 Sports' },

  {
    path: 'airguns',
    component: ProductListingPage,
    data: {
      category: 'airgun',
      title: 'Airguns',
      subtitle: 'Springers, PCPs and more — every listing is checked for what pellets and accessories actually fit it.',
    },
    title: 'Airguns — ONE77 Sports',
  },
  { path: 'airguns/:handle', component: ProductDetailPage, title: 'ONE77 Sports' },

  {
    path: 'pellets',
    component: ProductListingPage,
    data: { category: 'pellet', title: 'Pellets', subtitle: 'Matched to your calibre and use case, not just a spec sheet.' },
    title: 'Pellets — ONE77 Sports',
  },
  { path: 'pellets/:handle', component: ProductDetailPage, title: 'ONE77 Sports' },

  {
    path: 'accessories',
    component: ProductListingPage,
    data: {
      category: 'accessory',
      title: 'Accessories',
      subtitle: 'Scopes, cases and more — compatible with your setup by design.',
    },
    title: 'Accessories — ONE77 Sports',
  },
  { path: 'accessories/:handle', component: ProductDetailPage, title: 'ONE77 Sports' },

  { path: 'bundles', component: BundlesPage, title: 'Bundles — ONE77 Sports' },

  { path: 'learn', component: LearnLandingPage, title: 'Learn — ONE77 Sports' },
  { path: 'learn/:slug', component: LearnCategoryPage, title: 'Learn — ONE77 Sports' },
  { path: 'learn/:slug/:entryId', component: LearnEntryPage, title: 'Learn — ONE77 Sports' },

  { path: 'webinar', component: WebinarPage, title: 'Webinars — ONE77 Sports' },

  { path: 'terms', component: TermsPage, title: 'Terms & Conditions — ONE77 Sports' },
  { path: 'privacy', component: PrivacyPage, title: 'Privacy Policy — ONE77 Sports' },
  { path: 'refund', component: RefundPage, title: 'Refund Policy — ONE77 Sports' },
  { path: 'about', component: AboutPage, title: 'About Us — ONE77 Sports' },

  {
    path: 'admin/login',
    loadComponent: () => import('./admin/layout/admin-login-page').then((m) => m.AdminLoginPage),
    title: 'Admin Login — ONE77 Sports',
  },
  {
    path: 'admin',
    loadComponent: () => import('./admin/layout/admin-shell').then((m) => m.AdminShell),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./admin/features/dashboard/admin-dashboard-page').then((m) => m.AdminDashboardPage),
      },
      {
        path: 'homepage',
        loadComponent: () => import('./admin/features/homepage/admin-homepage-page').then((m) => m.AdminHomepagePage),
      },
      {
        path: 'products',
        loadComponent: () => import('./admin/features/products/admin-products-list-page').then((m) => m.AdminProductsListPage),
      },
      {
        path: 'products/:handle',
        loadComponent: () => import('./admin/features/products/admin-product-detail-page').then((m) => m.AdminProductDetailPage),
      },
      {
        path: 'match',
        loadComponent: () => import('./admin/features/match/admin-match-list-page').then((m) => m.AdminMatchListPage),
      },
      {
        path: 'match/:shopifyProductId',
        loadComponent: () => import('./admin/features/match/admin-match-editor-page').then((m) => m.AdminMatchEditorPage),
      },
      {
        path: 'bundles',
        loadComponent: () => import('./admin/features/bundles/admin-bundles-list-page').then((m) => m.AdminBundlesListPage),
      },
      {
        path: 'bundles/:id',
        loadComponent: () => import('./admin/features/bundles/admin-bundle-editor-page').then((m) => m.AdminBundleEditorPage),
      },
      {
        path: 'learn',
        loadComponent: () => import('./admin/features/learn/admin-learn-categories-page').then((m) => m.AdminLearnCategoriesPage),
      },
      {
        path: 'learn/entries',
        loadComponent: () => import('./admin/features/learn/admin-learn-entries-page').then((m) => m.AdminLearnEntriesPage),
      },
      {
        path: 'webinars',
        loadComponent: () => import('./admin/features/webinar/admin-webinar-events-page').then((m) => m.AdminWebinarEventsPage),
      },
      {
        path: 'webinars/:id/registrations',
        loadComponent: () =>
          import('./admin/features/webinar/admin-webinar-registrations-page').then((m) => m.AdminWebinarRegistrationsPage),
      },
    ],
  },

  { path: '**', component: NotFoundPage },
];