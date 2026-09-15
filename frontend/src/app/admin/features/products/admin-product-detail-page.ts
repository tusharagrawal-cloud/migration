import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ShopifyProductService } from '../../../core/services/shopify-product.service';
import { ShopifyProductDetail } from '../../../core/models/shopify-product.model';
import { AdminEnrichmentService } from '../../core/services/admin-enrichment.service';
import { EnrichmentSpecification, SaveEnrichmentRequest } from '../../core/models/admin-enrichment.models';
import { AdminLoading, AdminError } from '../../shared/admin-states';

const CALIBRE_OPTIONS = ['4.5mm', '5.5mm', '6.35mm'];
const POWERPLANT_OPTIONS = ['springer', 'nitro_piston', 'pcp', 'co2'];
const USE_CASE_OPTIONS = ['target_10m', 'plinking', 'competition', 'training', 'general'];

type LoadState = 'loading' | 'loaded' | 'not-found' | 'error';
type SaveState = 'idle' | 'saving' | 'saved' | 'error';

/**
 * "Edit ONE77 Details → Save" — one screen, one save action, exactly per
 * ADMIN_V1_USABILITY_CONTRACT.md, editing the coherent EnrichmentRecord the
 * API already assembles server-side rather than four separate table
 * screens. No JSON, no SortOrder field, no Shopify ID entry.
 */
@Component({
  selector: 'app-admin-product-detail-page',
  imports: [FormsModule, RouterLink, AdminLoading, AdminError],
  template: `
    <a routerLink="/admin/products" class="back-link">&larr; All products</a>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading product…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load this product. Please try again." (retry)="load()" />
      }
      @case ('not-found') {
        <app-admin-error message="We couldn't find that product." [retryable]="false" />
      }
      @case ('loaded') {
        <h1>{{ product()!.title }}</h1>
        <p class="lede">{{ product()!.vendor }}</p>

        <form (ngSubmit)="save()" class="form">
          <label class="admin-field">
            Category
            <select [(ngModel)]="category" name="category">
              <option value="airgun">Airgun</option>
              <option value="pellet">Pellet</option>
              <option value="accessory">Accessory</option>
            </select>
          </label>

          <label class="admin-field toggle-field">
            <input type="checkbox" [(ngModel)]="isActive" name="isActive" />
            Show in Match &amp; Bundle tools
          </label>

          @if (category === 'airgun' || category === 'pellet') {
            <label class="admin-field">
              Calibre
              <select [(ngModel)]="calibre" name="calibre">
                <option [ngValue]="null">Not set</option>
                @for (c of calibreOptions; track c) {
                  <option [value]="c">{{ c }}</option>
                }
              </select>
            </label>
          }

          @if (category === 'airgun') {
            <label class="admin-field">
              Powerplant type
              <select [(ngModel)]="powerplantType" name="powerplantType">
                <option [ngValue]="null">Not set</option>
                @for (p of powerplantOptions; track p) {
                  <option [value]="p">{{ powerplantLabel(p) }}</option>
                }
              </select>
            </label>

            <div class="field-pair">
              <label class="admin-field">
                Recommended pellet weight — min (grains)
                <input type="number" [(ngModel)]="recommendedMin" name="recommendedMin" />
              </label>
              <label class="admin-field">
                Recommended pellet weight — max (grains)
                <input type="number" [(ngModel)]="recommendedMax" name="recommendedMax" />
              </label>
            </div>
          }

          @if (category === 'pellet') {
            <label class="admin-field">
              Weight (grains)
              <input type="number" [(ngModel)]="weightGrains" name="weightGrains" />
            </label>
          }

          @if (category === 'accessory') {
            <fieldset class="admin-field checkbox-group">
              <legend>Compatible powerplants</legend>
              @for (p of powerplantOptions; track p) {
                <label class="checkbox-option">
                  <input type="checkbox" [checked]="compatiblePowerplants.includes(p)" (change)="toggle(compatiblePowerplants, p)" />
                  {{ powerplantLabel(p) }}
                </label>
              }
            </fieldset>
          }

          @if (category === 'pellet' || category === 'accessory') {
            <fieldset class="admin-field checkbox-group">
              <legend>Use cases</legend>
              @for (u of useCaseOptions; track u) {
                <label class="checkbox-option">
                  <input type="checkbox" [checked]="useCases.includes(u)" (change)="toggle(useCases, u)" />
                  {{ useCaseLabel(u) }}
                </label>
              }
            </fieldset>
          }

          <div class="admin-field">
            <span class="specs-legend">Specifications</span>
            @for (spec of specifications(); track $index) {
              <div class="spec-row">
                <input type="text" placeholder="Label" [(ngModel)]="spec.spec_key" [name]="'specKey' + $index" />
                <input type="text" placeholder="Value" [(ngModel)]="spec.spec_value" [name]="'specValue' + $index" />
                <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="moveSpec($index, -1)" [disabled]="$index === 0">Move up</button>
                <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="moveSpec($index, 1)" [disabled]="$index === specifications().length - 1">Move down</button>
                <button type="button" class="admin-btn admin-btn-danger admin-btn-sm" (click)="removeSpec($index)">Remove</button>
              </div>
            }
            <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="addSpec()">+ Add specification</button>
          </div>

          @if (saveState() === 'error') {
            <p class="save-error" role="alert">Unable to save. Please check the form and try again.</p>
          }
          @if (saveState() === 'saved') {
            <p class="save-success" role="status">Saved.</p>
          }

          <button type="submit" class="admin-btn admin-btn-primary" [disabled]="saveState() === 'saving'">
            {{ saveState() === 'saving' ? 'Saving…' : 'Save' }}
          </button>
        </form>
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
      h1 {
        font-size: 1.375rem;
        margin-bottom: var(--admin-space-1);
      }
      .lede {
        color: var(--admin-text-muted);
        margin-bottom: var(--admin-space-6);
      }
      .form {
        display: grid;
        gap: var(--admin-space-5);
        max-width: 560px;
      }
      .toggle-field {
        flex-direction: row;
        align-items: center;
        display: flex;
        gap: var(--admin-space-2);
      }
      .field-pair {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--admin-space-4);
      }
      fieldset.checkbox-group {
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-sm);
        padding: var(--admin-space-3);
        display: flex;
        flex-wrap: wrap;
        gap: var(--admin-space-3);
      }
      legend {
        font-weight: 500;
        padding: 0 var(--admin-space-2);
      }
      .checkbox-option {
        display: flex;
        align-items: center;
        gap: var(--admin-space-1);
        font-weight: 400;
        font-size: 0.875rem;
      }
      .specs-legend {
        font-weight: 500;
        display: block;
        margin-bottom: var(--admin-space-1);
      }
      .spec-row {
        display: flex;
        gap: var(--admin-space-2);
        align-items: center;
        margin-bottom: var(--admin-space-2);
      }
      .spec-row input {
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-sm);
        padding: var(--admin-space-2);
        font-size: 0.875rem;
        flex: 1;
        min-width: 0;
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

      @media (max-width: 600px) {
        .field-pair {
          grid-template-columns: 1fr;
        }
        .spec-row {
          flex-wrap: wrap;
        }
      }
    `,
  ],
})
export class AdminProductDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shopifyProducts = inject(ShopifyProductService);
  private readonly enrichmentService = inject(AdminEnrichmentService);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected product = signal<ShopifyProductDetail | null>(null);
  protected specifications = signal<EnrichmentSpecification[]>([]);

  protected readonly calibreOptions = CALIBRE_OPTIONS;
  protected readonly powerplantOptions = POWERPLANT_OPTIONS;
  protected readonly useCaseOptions = USE_CASE_OPTIONS;

  protected category = 'airgun';
  protected isActive = true;
  protected calibre: string | null = null;
  protected powerplantType: string | null = null;
  protected weightGrains: number | null = null;
  protected recommendedMin: number | null = null;
  protected recommendedMax: number | null = null;
  protected compatiblePowerplants: string[] = [];
  protected useCases: string[] = [];

  private handle = '';

  ngOnInit(): void {
    this.handle = this.route.snapshot.paramMap.get('handle') ?? '';
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.shopifyProducts.getByHandle(this.handle).subscribe({
      next: (product) => {
        if (!product) {
          this.state.set('not-found');
          return;
        }
        this.product.set(product);
        this.category = product.category;

        this.enrichmentService.getByProduct(product.id).subscribe({
          next: (record) => {
            this.category = record.category;
            this.isActive = record.is_active;
            this.calibre = record.calibre;
            this.powerplantType = record.powerplant_type;
            this.weightGrains = record.weight_grains;
            this.recommendedMin = record.recommended_pellet_weight_min;
            this.recommendedMax = record.recommended_pellet_weight_max;
            this.compatiblePowerplants = [...record.compatible_powerplants];
            this.useCases = [...record.use_cases];
            this.specifications.set(record.specifications.map((s) => ({ ...s })));
            this.state.set('loaded');
          },
          error: (err: HttpErrorResponse) => {
            if (err.status === 404) {
              // No ONE77 details yet — an empty, ready-to-fill form is the correct state, not an error.
              this.state.set('loaded');
              return;
            }
            this.state.set('error');
          },
        });
      },
      error: () => this.state.set('error'),
    });
  }

  protected toggle(list: string[], value: string): void {
    const index = list.indexOf(value);
    if (index === -1) {
      list.push(value);
    } else {
      list.splice(index, 1);
    }
  }

  protected addSpec(): void {
    this.specifications.update((specs) => [...specs, { spec_key: '', spec_value: '', sort_order: specs.length }]);
  }

  protected removeSpec(index: number): void {
    this.specifications.update((specs) => specs.filter((_, i) => i !== index));
  }

  protected moveSpec(index: number, direction: -1 | 1): void {
    this.specifications.update((specs) => {
      const target = index + direction;
      if (target < 0 || target >= specs.length) {
        return specs;
      }
      const copy = [...specs];
      [copy[index], copy[target]] = [copy[target], copy[index]];
      return copy;
    });
  }

  protected save(): void {
    const product = this.product();
    if (!product) {
      return;
    }

    this.saveState.set('saving');
    const request: SaveEnrichmentRequest = {
      shopify_product_id: product.id,
      category: this.category,
      is_active: this.isActive,
      calibre: this.calibre,
      powerplant_type: this.powerplantType,
      weight_grains: this.weightGrains,
      recommended_pellet_weight_min: this.recommendedMin,
      recommended_pellet_weight_max: this.recommendedMax,
      compatible_powerplants: this.compatiblePowerplants,
      use_cases: this.useCases,
      specifications: this.specifications().map((s, i) => ({ ...s, sort_order: i })),
    };

    this.enrichmentService.getByProduct(product.id).subscribe({
      next: () => this.doUpdate(request),
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.doCreate(request);
        } else {
          this.saveState.set('error');
        }
      },
    });
  }

  private doCreate(request: SaveEnrichmentRequest): void {
    this.enrichmentService.create(request).subscribe({
      next: () => this.saveState.set('saved'),
      error: () => this.saveState.set('error'),
    });
  }

  private doUpdate(request: SaveEnrichmentRequest): void {
    this.enrichmentService.update(request).subscribe({
      next: () => this.saveState.set('saved'),
      error: () => this.saveState.set('error'),
    });
  }

  protected powerplantLabel(value: string): string {
    switch (value) {
      case 'springer':
        return 'Springer';
      case 'nitro_piston':
        return 'Nitro Piston';
      case 'pcp':
        return 'PCP';
      case 'co2':
        return 'CO2';
      default:
        return value;
    }
  }

  protected useCaseLabel(value: string): string {
    switch (value) {
      case 'target_10m':
        return 'Target Practice';
      case 'plinking':
        return 'Plinking';
      case 'competition':
        return 'Competition';
      case 'training':
        return 'Training';
      case 'general':
        return 'General Use';
      default:
        return value;
    }
  }
}
