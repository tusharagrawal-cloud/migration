import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminMatchService } from '../../core/services/admin-match.service';
import { MatchCandidateItem, MatchRelationshipView } from '../../core/models/admin-match.models';
import { AdminEnrichmentService } from '../../core/services/admin-enrichment.service';
import { ShopifyProductService } from '../../../core/services/shopify-product.service';
import { ShopifyProductDetail } from '../../../core/models/shopify-product.model';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type TargetCategory = 'pellet' | 'accessory';
type LoadState = 'loading' | 'loaded' | 'error';

const USE_CASE_OPTIONS: Record<TargetCategory, string[]> = {
  pellet: ['target_10m', 'plinking', 'competition', 'general'],
  accessory: ['target_10m', 'plinking', 'training', 'general'],
};

const USE_CASE_LABELS: Record<string, string> = {
  target_10m: 'Target Practice',
  plinking: 'Plinking',
  competition: 'Competition',
  training: 'Training',
  general: 'General Use',
};

interface EditingState {
  relationship: MatchRelationshipView;
  status: string;
  priority: number;
  useCases: string[];
  reason: string;
  adminNotes: string;
}

/**
 * The Match Admin workflow (Milestone 11, Section 9 — high importance).
 * Preserves all Milestone 6 business behavior; presents it in plain
 * language. Names shown for candidates/relationships always come from the
 * Shopify picker fixtures — the underlying GID never appears on screen.
 */
@Component({
  selector: 'app-admin-match-editor-page',
  imports: [FormsModule, RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <a routerLink="/admin/match" class="back-link">&larr; All airguns</a>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load this airgun's matches. Please try again." (retry)="loadAll()" />
      }
      @case ('loaded') {
        <h1>{{ airgun()?.title ?? 'Airgun' }}</h1>
        <p class="lede">{{ airgun()?.vendor }} · {{ airgunCalibre() || '—' }} · {{ airgunPowerplantLabel() || '—' }}</p>
        <a [routerLink]="['/admin/products', airgun()?.handle]" class="edit-product-link">Edit product details →</a>

        <div class="tabs">
          <button type="button" class="tab" [class.active]="tab() === 'pellet'" (click)="switchTab('pellet')">Pellets</button>
          <button type="button" class="tab" [class.active]="tab() === 'accessory'" (click)="switchTab('accessory')">Accessories</button>
        </div>

        @if (actionError()) {
          <p class="action-error" role="alert">{{ actionError() }}</p>
        }

        <div class="columns">
          <div class="column">
            <input
              type="text"
              class="admin-field candidate-search"
              placeholder="Search {{ tab() }}s by name…"
              [value]="candidateQuery()"
              (input)="candidateQuery.set($any($event.target).value)"
            />

            <div class="column-header">
              <span class="column-count">{{ filteredCandidates().length }} {{ tab() }}(s)</span>
              <button
                type="button"
                class="admin-btn admin-btn-secondary admin-btn-sm"
                [disabled]="selectedIds().size === 0"
                (click)="bulkMarkCompatible()"
              >
                Mark selected as Compatible
              </button>
            </div>

            @if (candidates().length === 0) {
              <app-admin-empty heading="No candidates" body="Nothing available in this category yet." />
            } @else {
              <div class="candidate-list">
                @for (candidate of filteredCandidates(); track candidate.product.shopify_product_id) {
                  <app-admin-card>
                    <div class="candidate-row">
                      <input
                        type="checkbox"
                        [checked]="selectedIds().has(candidate.product.shopify_product_id)"
                        (change)="toggleSelected(candidate.product.shopify_product_id)"
                        [attr.aria-label]="'Select ' + productName(candidate.product.shopify_product_id)"
                      />
                      <div class="candidate-info">
                        <p class="candidate-name">{{ productName(candidate.product.shopify_product_id) }}</p>
                        <p class="candidate-meta">
                          {{ candidate.product.calibre || '—' }}
                          @if (tab() === 'pellet' && candidate.product.weight_grains) {
                            · {{ candidate.product.weight_grains }}gr
                          }
                        </p>
                        @if (calibreMismatch(candidate)) {
                          <p class="mismatch-warning">Calibre does not match this airgun.</p>
                        }
                      </div>
                      @if (candidate.current_relationship) {
                        <span class="admin-badge" [class]="statusBadgeClass(candidate.current_relationship.status)">
                          {{ statusLabel(candidate.current_relationship.status) }}
                        </span>
                      }
                      <div class="candidate-actions">
                        <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="quickMark(candidate, 'compatible')">Compatible</button>
                        <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="quickMark(candidate, 'recommended')">Recommend</button>
                        <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="quickMark(candidate, 'not_recommended')">Exclude</button>
                      </div>
                    </div>
                  </app-admin-card>
                }
              </div>
            }
          </div>

          <div class="column">
            <p class="column-count">Current matches ({{ relationships().length }})</p>
            @if (relationships().length === 0) {
              <app-admin-empty heading="No matches yet" body="Use Compatible / Recommend / Exclude on a candidate to the left to add one." />
            } @else {
              <div class="candidate-list">
                @for (rel of relationships(); track rel.id) {
                  <app-admin-card>
                    <div class="rel-row">
                      <div class="candidate-info">
                        <p class="candidate-name">{{ productName(rel.target_shopify_product_id) }}</p>
                        <div class="rel-badges">
                          <span class="admin-badge" [class]="statusBadgeClass(rel.status)">{{ statusLabel(rel.status) }}</span>
                          @if (rel.status === 'recommended' && rel.priority) {
                            <span class="admin-badge">{{ rel.priority_label }}</span>
                          }
                          @for (uc of rel.use_cases; track uc) {
                            <span class="admin-badge">{{ useCaseLabel(uc) }}</span>
                          }
                        </div>
                        @if (rel.reason) {
                          <p class="rel-reason">&ldquo;{{ rel.reason }}&rdquo;</p>
                        }
                      </div>
                      <div class="candidate-actions">
                        <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="startEdit(rel)">Edit</button>
                        <button type="button" class="admin-btn admin-btn-danger admin-btn-sm" (click)="confirmRemove(rel)">Remove</button>
                      </div>
                    </div>
                  </app-admin-card>
                }
              </div>
            }
          </div>
        </div>

        @if (editing(); as edit) {
          <div class="modal-backdrop" (click)="cancelEdit()">
            <div class="modal" (click)="$event.stopPropagation()">
              <h2>Edit match — {{ productName(edit.relationship.target_shopify_product_id) }}</h2>

              <div class="admin-field">
                <span class="field-label">Classification</span>
                <div class="choice-group">
                  <button type="button" class="choice" [class.selected]="edit.status === 'compatible'" (click)="edit.status = 'compatible'">Compatible</button>
                  <button type="button" class="choice" [class.selected]="edit.status === 'recommended'" (click)="edit.status = 'recommended'">Recommended</button>
                  <button type="button" class="choice" [class.selected]="edit.status === 'not_recommended'" (click)="edit.status = 'not_recommended'">Not Recommended</button>
                </div>
              </div>

              @if (edit.status === 'recommended') {
                <div class="admin-field">
                  <span class="field-label">Priority</span>
                  <div class="choice-group">
                    <button type="button" class="choice" [class.selected]="edit.priority === 1" (click)="edit.priority = 1">Best Match</button>
                    <button type="button" class="choice" [class.selected]="edit.priority === 2" (click)="edit.priority = 2">Recommended</button>
                    <button type="button" class="choice" [class.selected]="edit.priority === 3" (click)="edit.priority = 3">Alternative</button>
                  </div>
                </div>
              }

              <div class="admin-field">
                <span class="field-label">Use cases</span>
                <div class="choice-group">
                  @for (uc of useCaseOptionsFor(tab()); track uc) {
                    <button type="button" class="choice" [class.selected]="edit.useCases.includes(uc)" (click)="toggleEditUseCase(uc)">{{ useCaseLabel(uc) }}</button>
                  }
                </div>
              </div>

              <label class="admin-field">
                Customer-facing reason (optional)
                <textarea rows="3" [(ngModel)]="edit.reason" name="editReason" placeholder="Why this is a good match…"></textarea>
              </label>

              <label class="admin-field">
                Internal note (optional, not shown to customers)
                <input type="text" [(ngModel)]="edit.adminNotes" name="editNotes" />
              </label>

              @if (actionError()) {
                <p class="action-error" role="alert">{{ actionError() }}</p>
              }

              <div class="modal-actions">
                <button type="button" class="admin-btn admin-btn-secondary" (click)="cancelEdit()">Cancel</button>
                <button type="button" class="admin-btn admin-btn-primary" (click)="saveEdit()">Save</button>
              </div>
            </div>
          </div>
        }
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
        margin-bottom: var(--admin-space-1);
      }
      .edit-product-link {
        font-size: 0.8125rem;
        color: var(--admin-accent);
        text-decoration: none;
      }
      .tabs {
        display: flex;
        gap: var(--admin-space-2);
        border-bottom: 1px solid var(--admin-border);
        margin: var(--admin-space-5) 0 var(--admin-space-5);
      }
      .tab {
        background: none;
        border: none;
        border-bottom: 2px solid transparent;
        padding: var(--admin-space-3) var(--admin-space-2);
        font-size: 0.9375rem;
        font-weight: 500;
        color: var(--admin-text-muted);
        cursor: pointer;
      }
      .tab.active {
        color: var(--admin-text);
        border-bottom-color: var(--admin-accent);
      }
      .action-error {
        color: var(--admin-danger);
        background: var(--admin-danger-bg);
        border-radius: var(--admin-radius-sm);
        padding: var(--admin-space-3);
        font-size: 0.875rem;
      }
      .columns {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--admin-space-6);
      }
      .candidate-search {
        margin-bottom: var(--admin-space-3);
      }
      .column-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: var(--admin-space-3);
      }
      .column-count {
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
      }
      .candidate-list {
        display: grid;
        gap: var(--admin-space-3);
        max-height: 640px;
        overflow-y: auto;
      }
      .candidate-row,
      .rel-row {
        display: flex;
        align-items: flex-start;
        gap: var(--admin-space-3);
      }
      .candidate-info {
        flex: 1;
        min-width: 0;
      }
      .candidate-name {
        margin: 0;
        font-weight: 500;
        font-size: 0.9375rem;
      }
      .candidate-meta {
        margin: var(--admin-space-1) 0 0;
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
      }
      .mismatch-warning {
        margin: var(--admin-space-1) 0 0;
        font-size: 0.75rem;
        color: var(--admin-warning);
      }
      .rel-badges {
        display: flex;
        gap: var(--admin-space-1);
        flex-wrap: wrap;
        margin-top: var(--admin-space-1);
      }
      .rel-reason {
        margin: var(--admin-space-2) 0 0;
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
        font-style: italic;
      }
      .candidate-actions {
        display: flex;
        flex-direction: column;
        gap: var(--admin-space-1);
        flex-shrink: 0;
      }
      .modal-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(20, 24, 30, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        padding: var(--admin-space-5);
        z-index: 200;
      }
      .modal {
        background: var(--admin-surface);
        border-radius: var(--admin-radius-lg);
        padding: var(--admin-space-6);
        max-width: 480px;
        width: 100%;
        max-height: 90vh;
        overflow-y: auto;
        display: grid;
        gap: var(--admin-space-4);
      }
      .modal h2 {
        font-size: 1.125rem;
        margin: 0;
      }
      .field-label {
        font-weight: 500;
        display: block;
        margin-bottom: var(--admin-space-1);
      }
      .choice-group {
        display: flex;
        flex-wrap: wrap;
        gap: var(--admin-space-2);
      }
      .choice {
        border: 1px solid var(--admin-border);
        background: var(--admin-surface);
        border-radius: var(--admin-radius-sm);
        padding: var(--admin-space-2) var(--admin-space-3);
        font-size: 0.8125rem;
        cursor: pointer;
      }
      .choice.selected {
        background: var(--admin-accent);
        border-color: var(--admin-accent);
        color: var(--admin-accent-contrast);
      }
      .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--admin-space-3);
      }

      @media (max-width: 860px) {
        .columns {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AdminMatchEditorPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly matchService = inject(AdminMatchService);
  private readonly enrichmentService = inject(AdminEnrichmentService);
  private readonly shopifyProducts = inject(ShopifyProductService);

  protected state = signal<LoadState>('loading');
  protected airgun = signal<ShopifyProductDetail | null>(null);
  protected airgunCalibreValue = signal<string | null>(null);
  protected airgunPowerplantValue = signal<string | null>(null);
  protected tab = signal<TargetCategory>('pellet');
  protected candidates = signal<MatchCandidateItem[]>([]);
  protected relationships = signal<MatchRelationshipView[]>([]);
  protected names = signal<Map<string, string>>(new Map());
  protected selectedIds = signal<Set<string>>(new Set());
  protected candidateQuery = signal('');
  protected actionError = signal<string | null>(null);
  protected editing = signal<EditingState | null>(null);

  protected filteredCandidates = computed(() => {
    const q = this.candidateQuery().trim().toLowerCase();
    if (!q) {
      return this.candidates();
    }
    return this.candidates().filter((c) => this.productName(c.product.shopify_product_id).toLowerCase().includes(q));
  });

  private shopifyProductId = '';

  ngOnInit(): void {
    // ActivatedRoute.paramMap already decodes the URL segment — routerLink
    // (see admin-match-list-page.ts) and Router.navigate handle percent-
    // encoding a Shopify GID's "/" characters transparently on the way in.
    this.shopifyProductId = this.route.snapshot.paramMap.get('shopifyProductId') ?? '';
    this.loadAll();
  }

  protected loadAll(): void {
    this.state.set('loading');
    this.actionError.set(null);

    this.shopifyProducts.getById(this.shopifyProductId).subscribe({
      next: (product) => {
        this.airgun.set(product ?? null);
        this.enrichmentService.getByProduct(this.shopifyProductId).subscribe({
          next: (record) => {
            this.airgunCalibreValue.set(record.calibre);
            this.airgunPowerplantValue.set(record.powerplant_type);
            this.loadTabData();
          },
          error: () => {
            // No ONE77 details set for this airgun yet — calibre/powerplant simply unknown, not an error.
            this.airgunCalibreValue.set(null);
            this.airgunPowerplantValue.set(null);
            this.loadTabData();
          },
        });
      },
      error: () => this.state.set('error'),
    });
  }

  private loadTabData(): void {
    const targetCategory = this.tab();
    this.matchService.getRelationships(this.shopifyProductId, targetCategory).subscribe({
      next: (relRes) => {
        this.relationships.set(relRes.items);
        this.matchService.getCandidates(this.shopifyProductId, targetCategory).subscribe({
          next: (candRes) => {
            this.candidates.set(candRes.items);
            this.resolveNames(relRes.items, candRes.items);
          },
          error: () => this.state.set('error'),
        });
      },
      error: () => this.state.set('error'),
    });
  }

  private resolveNames(relationships: MatchRelationshipView[], candidates: MatchCandidateItem[]): void {
    const ids = new Set<string>();
    relationships.forEach((r) => ids.add(r.target_shopify_product_id));
    candidates.forEach((c) => ids.add(c.product.shopify_product_id));

    const idList = Array.from(ids);
    if (idList.length === 0) {
      this.state.set('loaded');
      return;
    }

    const results = new Map<string, string>();
    let remaining = idList.length;
    idList.forEach((id) => {
      this.shopifyProducts.getById(id).subscribe({
        next: (product) => {
          results.set(id, product?.title ?? 'Unnamed product');
          remaining -= 1;
          if (remaining === 0) {
            this.names.set(results);
            this.state.set('loaded');
          }
        },
        error: () => {
          results.set(id, 'Unnamed product');
          remaining -= 1;
          if (remaining === 0) {
            this.names.set(results);
            this.state.set('loaded');
          }
        },
      });
    });
  }

  protected switchTab(tab: TargetCategory): void {
    if (this.tab() === tab) {
      return;
    }
    this.tab.set(tab);
    this.selectedIds.set(new Set());
    this.candidateQuery.set('');
    this.loadTabData();
  }

  protected productName(shopifyProductId: string): string {
    return this.names().get(shopifyProductId) ?? 'Unnamed product';
  }

  protected airgunCalibre(): string | null {
    return this.airgunCalibreValue();
  }

  protected airgunPowerplantLabel(): string | null {
    const value = this.airgunPowerplantValue();
    return value ? this.powerplantLabel(value) : null;
  }

  private powerplantLabel(value: string): string {
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

  /**
   * A non-blocking hint only — the server is authoritative (a mismatch
   * surfaces as a 400 with a plain-language message on save, handled by
   * handleUpsertError below, which is what actually gates the override).
   */
  protected calibreMismatch(candidate: MatchCandidateItem): boolean {
    if (this.tab() !== 'pellet') {
      return false;
    }
    const airgunCalibre = this.airgunCalibreValue();
    const pelletCalibre = candidate.product.calibre;
    return !!airgunCalibre && !!pelletCalibre && airgunCalibre !== pelletCalibre;
  }

  protected statusLabel(status: string): string {
    switch (status) {
      case 'compatible':
        return 'Compatible';
      case 'recommended':
        return 'Recommended';
      case 'not_recommended':
        return 'Not Recommended';
      default:
        return status;
    }
  }

  protected statusBadgeClass(status: string): string {
    switch (status) {
      case 'recommended':
        return 'admin-badge-success';
      case 'not_recommended':
        return 'admin-badge-danger';
      default:
        return 'admin-badge';
    }
  }

  protected useCaseLabel(value: string): string {
    return USE_CASE_LABELS[value] ?? value;
  }

  protected useCaseOptionsFor(tab: TargetCategory): string[] {
    return USE_CASE_OPTIONS[tab];
  }

  protected toggleSelected(id: string): void {
    this.selectedIds.update((set) => {
      const next = new Set(set);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  protected quickMark(candidate: MatchCandidateItem, status: string): void {
    this.actionError.set(null);
    this.matchService
      .upsertRelationship({
        source_shopify_product_id: this.shopifyProductId,
        target_shopify_product_id: candidate.product.shopify_product_id,
        status,
        priority: status === 'recommended' ? 2 : null,
        use_cases: [],
        reason: null,
        admin_notes: null,
        calibre_override: false,
      })
      .subscribe({
        next: () => this.loadTabData(),
        error: (err: HttpErrorResponse) => this.handleUpsertError(err, candidate, status),
      });
  }

  private handleUpsertError(err: HttpErrorResponse, candidate: MatchCandidateItem, status: string): void {
    const message = typeof err.error?.error === 'string' ? err.error.error : '';
    if (err.status === 400 && message.toLowerCase().includes('calibre')) {
      const confirmed = window.confirm(`${message}\n\nSave anyway?`);
      if (confirmed) {
        this.matchService
          .upsertRelationship({
            source_shopify_product_id: this.shopifyProductId,
            target_shopify_product_id: candidate.product.shopify_product_id,
            status,
            priority: status === 'recommended' ? 2 : null,
            use_cases: [],
            reason: null,
            admin_notes: null,
            calibre_override: true,
          })
          .subscribe({
            next: () => this.loadTabData(),
            error: () => this.actionError.set('Unable to save this match. Please try again.'),
          });
        return;
      }
    }
    this.actionError.set('Unable to save this match. Please try again.');
  }

  protected bulkMarkCompatible(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) {
      return;
    }
    this.actionError.set(null);
    this.matchService.bulkMark(this.shopifyProductId, ids, 'compatible').subscribe({
      next: () => {
        this.selectedIds.set(new Set());
        this.loadTabData();
      },
      error: () => this.actionError.set('Unable to save these matches. Please try again.'),
    });
  }

  protected startEdit(rel: MatchRelationshipView): void {
    this.actionError.set(null);
    this.editing.set({
      relationship: rel,
      status: rel.status,
      priority: rel.priority ?? 2,
      useCases: [...rel.use_cases],
      reason: rel.reason ?? '',
      adminNotes: rel.admin_notes ?? '',
    });
  }

  protected toggleEditUseCase(value: string): void {
    const edit = this.editing();
    if (!edit) {
      return;
    }
    const index = edit.useCases.indexOf(value);
    if (index === -1) {
      edit.useCases.push(value);
    } else {
      edit.useCases.splice(index, 1);
    }
  }

  protected cancelEdit(): void {
    this.editing.set(null);
    this.actionError.set(null);
  }

  protected saveEdit(): void {
    const edit = this.editing();
    if (!edit) {
      return;
    }
    this.matchService
      .patchRelationship(edit.relationship.id, {
        status: edit.status,
        priority: edit.status === 'recommended' ? edit.priority : null,
        use_cases: edit.useCases,
        reason: edit.reason,
        admin_notes: edit.adminNotes,
      })
      .subscribe({
        next: () => {
          this.editing.set(null);
          this.loadTabData();
        },
        error: () => this.actionError.set('Unable to save. Please try again.'),
      });
  }

  protected confirmRemove(rel: MatchRelationshipView): void {
    const confirmed = window.confirm(
      `Remove ${this.productName(rel.target_shopify_product_id)} from this airgun's matches? You can add it again anytime.`,
    );
    if (!confirmed) {
      return;
    }
    this.matchService.deleteRelationship(rel.id).subscribe({
      next: () => this.loadTabData(),
      error: () => this.actionError.set('Unable to remove this match. Please try again.'),
    });
  }
}
