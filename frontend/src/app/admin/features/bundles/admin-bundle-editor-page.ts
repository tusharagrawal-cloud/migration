import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AdminBundleService } from '../../core/services/admin-bundle.service';
import { BundleItemInput, SaveBundleRequest } from '../../core/models/admin-bundle.models';
import { ShopifyProductService } from '../../../core/services/shopify-product.service';
import { ShopifyProductSummary } from '../../../core/models/shopify-product.model';
import { ShopifyProductPicker } from '../../shared/shopify-product-picker';
import { AdminLoading, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'not-found' | 'error';
type SaveState = 'idle' | 'saving' | 'saved' | 'error';

interface PickedItem {
  shopify_product_id: string;
  title: string;
}

/**
 * "Create Bundle → Select Products → Arrange Products → Save"
 * (ADMIN_V1_USABILITY_CONTRACT.md). Item order in each list is the order
 * submitted — the API assigns sort_order from array position, so there is
 * no number for the operator to type. No price, savings, or media fields —
 * curation only.
 */
@Component({
  selector: 'app-admin-bundle-editor-page',
  imports: [FormsModule, RouterLink, ShopifyProductPicker, AdminLoading, AdminError],
  template: `
    <a routerLink="/admin/bundles" class="back-link">&larr; All bundles</a>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load this bundle. Please try again." (retry)="load()" />
      }
      @case ('not-found') {
        <app-admin-error message="We couldn't find that bundle." [retryable]="false" />
      }
      @case ('loaded') {
        <div class="header-row">
          <h1>{{ isNew ? 'Create bundle' : name }}</h1>
          @if (!isNew) {
            <div class="lifecycle-actions">
              @if (status === 'Draft') {
                <button type="button" class="admin-btn admin-btn-primary admin-btn-sm" (click)="publish()">Show on Website</button>
              }
              @if (status === 'Published') {
                <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="archive()">Hide from Website</button>
              }
              @if (status === 'Archived') {
                <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="unarchive()">Restore to Draft</button>
              }
              <span class="admin-badge" [class]="statusBadgeClass()">{{ status }}</span>
            </div>
          }
        </div>

        <div class="form">
          <label class="admin-field">
            Name
            <input type="text" [(ngModel)]="name" name="bundleName" />
          </label>

          <label class="admin-field">
            Tagline (optional)
            <input type="text" [(ngModel)]="tagline" name="bundleTagline" placeholder="Short curation copy shown to customers" />
          </label>

          <div class="admin-field">
            <span class="section-label">Primary product</span>
            @if (primary()) {
              <div class="picked-single">
                <span>{{ primary()!.title }}</span>
                <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="primary.set(null)">Change</button>
              </div>
            } @else {
              <app-shopify-product-picker category="airgun" (productPicked)="setPrimary($event)" />
            }
          </div>

          <div class="admin-field">
            <span class="section-label">Pellets</span>
            @if (pellets().length) {
              <ul class="picked-list">
                @for (item of pellets(); track item.shopify_product_id) {
                  <li>
                    <span>{{ item.title }}</span>
                    <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="removePellet(item)">Remove</button>
                  </li>
                }
              </ul>
            }
            <app-shopify-product-picker category="pellet" [excludeIds]="pelletIds()" (productPicked)="addPellet($event)" />
          </div>

          <div class="admin-field">
            <span class="section-label">Accessories</span>
            @if (accessories().length) {
              <ul class="picked-list">
                @for (item of accessories(); track item.shopify_product_id) {
                  <li>
                    <span>{{ item.title }}</span>
                    <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="removeAccessory(item)">Remove</button>
                  </li>
                }
              </ul>
            }
            <app-shopify-product-picker category="accessory" [excludeIds]="accessoryIds()" (productPicked)="addAccessory($event)" />
          </div>

          @if (validationMessage()) {
            <p class="save-error" role="alert">{{ validationMessage() }}</p>
          }
          @if (saveState() === 'error') {
            <p class="save-error" role="alert">Unable to save. Please try again.</p>
          }
          @if (saveState() === 'saved') {
            <p class="save-success" role="status">Saved.</p>
          }

          <button type="button" class="admin-btn admin-btn-primary" [disabled]="saveState() === 'saving'" (click)="save()">
            {{ saveState() === 'saving' ? 'Saving…' : 'Save' }}
          </button>
        </div>
      }
    }
  `,
  styles: [
    `
      .back-link {
        display: inline-block;
        margin-bottom: var(--admin-space-4);
        color: var(--admin-text-muted);
        text-decoration: none;
        font-size: 0.875rem;
      }
      .header-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: var(--admin-space-3);
        margin-bottom: var(--admin-space-6);
      }
      h1 {
        font-size: 1.375rem;
        margin: 0;
      }
      .lifecycle-actions {
        display: flex;
        align-items: center;
        gap: var(--admin-space-3);
      }
      .form {
        display: grid;
        gap: var(--admin-space-5);
        max-width: 520px;
      }
      .section-label {
        font-weight: 500;
        display: block;
        margin-bottom: var(--admin-space-2);
      }
      .picked-single,
      .picked-list li {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--admin-space-3);
        padding: var(--admin-space-3);
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-sm);
        background: var(--admin-surface);
        margin-bottom: var(--admin-space-2);
        font-size: 0.875rem;
      }
      .picked-list {
        list-style: none;
        margin: 0 0 var(--admin-space-2);
        padding: 0;
      }
      .save-error {
        color: var(--admin-danger);
        margin: 0;
        font-size: 0.875rem;
      }
      .save-success {
        color: var(--admin-success);
        margin: 0;
        font-size: 0.875rem;
      }
    `,
  ],
})
export class AdminBundleEditorPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bundleService = inject(AdminBundleService);
  private readonly shopifyProducts = inject(ShopifyProductService);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected validationMessage = signal<string | null>(null);

  protected isNew = true;
  protected name = '';
  protected tagline = '';
  protected status = 'Draft';
  protected primary = signal<PickedItem | null>(null);
  protected pellets = signal<PickedItem[]>([]);
  protected accessories = signal<PickedItem[]>([]);

  protected pelletIds = signal<string[]>([]);
  protected accessoryIds = signal<string[]>([]);

  private bundleId: number | null = null;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam || idParam === 'new') {
      this.isNew = true;
      this.state.set('loaded');
      return;
    }

    this.isNew = false;
    this.bundleId = Number(idParam);
    this.load();
  }

  protected load(): void {
    if (this.bundleId == null) {
      return;
    }
    this.state.set('loading');
    this.bundleService.getById(this.bundleId).subscribe({
      next: (bundle) => {
        this.name = bundle.name;
        this.tagline = bundle.tagline ?? '';
        this.status = bundle.status;

        const sorted = [...bundle.items].sort((a, b) => a.sort_order - b.sort_order);
        const withNames$ = sorted.map((item) =>
          this.shopifyProducts.getById(item.shopify_product_id).pipe(
            map((p): PickedItem & { role: string } => ({
              shopify_product_id: item.shopify_product_id,
              title: p?.title ?? 'Unnamed product',
              role: item.item_role,
            })),
            catchError(() =>
              of({ shopify_product_id: item.shopify_product_id, title: 'Unnamed product', role: item.item_role }),
            ),
          ),
        );

        if (withNames$.length === 0) {
          this.state.set('loaded');
          return;
        }

        forkJoin(withNames$).subscribe((items) => {
          const primaryItem = items.find((i) => i.role === 'Primary');
          this.primary.set(primaryItem ? { shopify_product_id: primaryItem.shopify_product_id, title: primaryItem.title } : null);
          const pelletItems = items.filter((i) => i.role === 'Pellet').map(({ shopify_product_id, title }) => ({ shopify_product_id, title }));
          const accessoryItems = items.filter((i) => i.role === 'Accessory').map(({ shopify_product_id, title }) => ({ shopify_product_id, title }));
          this.pellets.set(pelletItems);
          this.accessories.set(accessoryItems);
          this.pelletIds.set(pelletItems.map((i) => i.shopify_product_id));
          this.accessoryIds.set(accessoryItems.map((i) => i.shopify_product_id));
          this.state.set('loaded');
        });
      },
      error: () => this.state.set('not-found'),
    });
  }

  protected setPrimary(product: ShopifyProductSummary): void {
    this.primary.set({ shopify_product_id: product.id, title: product.title });
  }

  protected addPellet(product: ShopifyProductSummary): void {
    this.pellets.update((list) => [...list, { shopify_product_id: product.id, title: product.title }]);
    this.pelletIds.update((ids) => [...ids, product.id]);
  }

  protected removePellet(item: PickedItem): void {
    this.pellets.update((list) => list.filter((i) => i.shopify_product_id !== item.shopify_product_id));
    this.pelletIds.update((ids) => ids.filter((id) => id !== item.shopify_product_id));
  }

  protected addAccessory(product: ShopifyProductSummary): void {
    this.accessories.update((list) => [...list, { shopify_product_id: product.id, title: product.title }]);
    this.accessoryIds.update((ids) => [...ids, product.id]);
  }

  protected removeAccessory(item: PickedItem): void {
    this.accessories.update((list) => list.filter((i) => i.shopify_product_id !== item.shopify_product_id));
    this.accessoryIds.update((ids) => ids.filter((id) => id !== item.shopify_product_id));
  }

  protected statusBadgeClass(): string {
    switch (this.status) {
      case 'Published':
        return 'admin-badge-success';
      case 'Archived':
        return 'admin-badge';
      default:
        return 'admin-badge-warning';
    }
  }

  private buildItems(): BundleItemInput[] {
    const items: BundleItemInput[] = [];
    const primary = this.primary();
    if (primary) {
      items.push({ shopify_product_id: primary.shopify_product_id, item_role: 'Primary' });
    }
    this.pellets().forEach((p) => items.push({ shopify_product_id: p.shopify_product_id, item_role: 'Pellet' }));
    this.accessories().forEach((a) => items.push({ shopify_product_id: a.shopify_product_id, item_role: 'Accessory' }));
    return items;
  }

  protected save(): void {
    this.validationMessage.set(null);
    if (!this.name.trim()) {
      this.validationMessage.set('Please give this bundle a name.');
      return;
    }
    if (!this.primary()) {
      this.validationMessage.set('Please choose a primary product for this bundle.');
      return;
    }

    this.saveState.set('saving');
    const request: SaveBundleRequest = {
      name: this.name.trim(),
      tagline: this.tagline.trim() || null,
      status: this.status,
      sort_priority: 0,
      items: this.buildItems(),
    };

    const save$ = this.isNew ? this.bundleService.create(request) : this.bundleService.update(this.bundleId!, request);
    save$.subscribe({
      next: (bundle) => {
        this.saveState.set('saved');
        if (this.isNew) {
          // Angular reuses this component instance for the URL change below (same route
          // config, only the :id param differs), so ngOnInit will not run again — update
          // isNew/bundleId/status here directly or the lifecycle buttons never appear.
          this.isNew = false;
          this.bundleId = bundle.id;
          this.status = bundle.status;
          this.router.navigate(['/admin/bundles', bundle.id]);
        }
      },
      error: () => this.saveState.set('error'),
    });
  }

  protected publish(): void {
    if (this.bundleId == null) return;
    this.validationMessage.set(null);
    this.bundleService.publish(this.bundleId).subscribe({
      next: (bundle) => (this.status = bundle.status),
      error: (err) => this.validationMessage.set(this.lifecycleErrorMessage(err)),
    });
  }

  protected archive(): void {
    if (this.bundleId == null) return;
    this.validationMessage.set(null);
    this.bundleService.archive(this.bundleId).subscribe({
      next: (bundle) => (this.status = bundle.status),
      error: (err) => this.validationMessage.set(this.lifecycleErrorMessage(err)),
    });
  }

  protected unarchive(): void {
    if (this.bundleId == null) return;
    this.validationMessage.set(null);
    this.bundleService.unarchive(this.bundleId).subscribe({
      next: (bundle) => (this.status = bundle.status),
      error: (err) => this.validationMessage.set(this.lifecycleErrorMessage(err)),
    });
  }

  /** The API already returns a plain-language `error` message (e.g. "Add at least one
   * more product alongside the primary product.") — surface it as-is; fall back only
   * for the unexpected case where the response has no such message. */
  private lifecycleErrorMessage(err: unknown): string {
    const apiMessage = (err as { error?: { error?: string } })?.error?.error;
    return apiMessage || 'Unable to update this bundle right now. Please try again.';
  }
}
