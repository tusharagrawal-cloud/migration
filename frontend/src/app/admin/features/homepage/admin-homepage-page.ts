import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { AdminHomepageService } from '../../core/services/admin-homepage.service';
import { API_BASE_URL } from '../../../core/config/api-config';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';
type SaveState = 'idle' | 'saving' | 'error';

/**
 * The one admin-controlled homepage content field: the hero photograph.
 * Deliberately not a page builder — upload, preview, save, replace, remove.
 * Everything about where/how the photograph is displayed (size, position,
 * overlay, responsive cropping) is locked, frontend-owned CSS; nothing here
 * can touch it.
 */
@Component({
  selector: 'app-admin-homepage-page',
  imports: [AdminCard, AdminLoading, AdminError],
  template: `
    <div class="header-row">
      <div>
        <h1>Homepage</h1>
        <p class="lede">Control the photograph shown behind the homepage headline.</p>
      </div>
    </div>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading homepage settings…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load homepage settings. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        <app-admin-card>
          <h2>Hero image</h2>

          @if (previewUrl(); as preview) {
            <div class="preview-block">
              <img [src]="preview" alt="Selected hero photograph preview" class="preview-image" />
              <p class="hint">This is how the photograph will fill the hero — save to publish it.</p>
              @if (saveState() === 'error') {
                <p class="save-error" role="alert">Unable to save this image. Please try again.</p>
              }
              <div class="actions">
                <button type="button" class="admin-btn admin-btn-secondary" (click)="cancelSelection()">Cancel</button>
                <button type="button" class="admin-btn admin-btn-primary" [disabled]="saveState() === 'saving'" (click)="save()">
                  {{ saveState() === 'saving' ? 'Saving…' : 'Save' }}
                </button>
              </div>
            </div>
          } @else if (currentHeroUrl(); as current) {
            <div class="preview-block">
              <img [src]="current" alt="Current homepage hero photograph" class="preview-image" />
              @if (actionError()) {
                <p class="save-error" role="alert">{{ actionError() }}</p>
              }
              <div class="actions">
                <label class="admin-btn admin-btn-secondary file-btn">
                  Replace image
                  <input type="file" accept="image/jpeg,image/png,image/webp" (change)="onFileSelected($event)" />
                </label>
                <button type="button" class="admin-btn admin-btn-danger" [disabled]="saveState() === 'saving'" (click)="removeImage()">
                  Remove image
                </button>
              </div>
            </div>
          } @else {
            <div class="empty-block">
              <p class="empty-heading">No hero image selected.</p>
              <p class="empty-body">The standard ONE77 hero will be used.</p>
              @if (actionError()) {
                <p class="save-error" role="alert">{{ actionError() }}</p>
              }
              <label class="admin-btn admin-btn-primary file-btn">
                Upload image
                <input type="file" accept="image/jpeg,image/png,image/webp" (change)="onFileSelected($event)" />
              </label>
            </div>
          }

          <p class="guidance">
            Use a wide landscape photograph, at least 1600 pixels wide. Keep important subjects away from the far
            left, right, top and bottom edges — the photograph crops differently on desktop and mobile. JPEG, PNG or
            WebP, up to 8 MB.
          </p>
        </app-admin-card>
      }
    }
  `,
  styles: [
    `
      .header-row {
        margin-bottom: var(--admin-space-5);
      }
      h1 {
        font-size: 1.5rem;
        margin-bottom: var(--admin-space-2);
      }
      .lede {
        color: var(--admin-text-muted);
        margin: 0;
      }
      h2 {
        font-size: 1.0625rem;
        margin: 0 0 var(--admin-space-4);
      }
      .preview-block,
      .empty-block {
        max-width: 480px;
      }
      .preview-image {
        display: block;
        width: 100%;
        aspect-ratio: 16 / 9;
        object-fit: cover;
        border-radius: var(--admin-radius-sm);
        border: 1px solid var(--admin-border);
        background: var(--admin-surface-hover);
      }
      .hint {
        margin: var(--admin-space-3) 0 0;
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
      }
      .empty-block {
        padding: var(--admin-space-6) var(--admin-space-5);
        text-align: center;
        border: 1px dashed var(--admin-border);
        border-radius: var(--admin-radius);
      }
      .empty-heading {
        margin: 0;
        font-weight: 500;
      }
      .empty-body {
        margin: var(--admin-space-2) 0 var(--admin-space-5);
        color: var(--admin-text-muted);
        font-size: 0.875rem;
      }
      .actions {
        display: flex;
        gap: var(--admin-space-3);
        margin-top: var(--admin-space-4);
      }
      .file-btn {
        position: relative;
        overflow: hidden;
        cursor: pointer;
      }
      .file-btn input[type='file'] {
        position: absolute;
        inset: 0;
        opacity: 0;
        cursor: pointer;
      }
      .save-error {
        color: var(--admin-danger);
        margin: var(--admin-space-3) 0 0;
        font-size: 0.875rem;
      }
      .guidance {
        margin: var(--admin-space-5) 0 0;
        padding-top: var(--admin-space-4);
        border-top: 1px solid var(--admin-border);
        color: var(--admin-text-muted);
        font-size: 0.8125rem;
        max-width: 480px;
      }
    `,
  ],
})
export class AdminHomepagePage implements OnInit, OnDestroy {
  private readonly homepageService = inject(AdminHomepageService);
  private readonly baseUrl = inject(API_BASE_URL);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected actionError = signal<string | null>(null);
  protected currentHeroUrl = signal<string | null>(null);
  protected previewUrl = signal<string | null>(null);

  private selectedFile: File | null = null;
  private objectUrl: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.revokeObjectUrl();
  }

  protected load(): void {
    this.state.set('loading');
    this.homepageService.getConfig().subscribe({
      next: (config) => {
        this.currentHeroUrl.set(config.hero_image_url ? `${this.baseUrl}${config.hero_image_url}` : null);
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    this.revokeObjectUrl();
    this.selectedFile = file;
    this.objectUrl = URL.createObjectURL(file);
    this.previewUrl.set(this.objectUrl);
    this.saveState.set('idle');
    this.actionError.set(null);
  }

  protected cancelSelection(): void {
    this.revokeObjectUrl();
    this.previewUrl.set(null);
    this.selectedFile = null;
    this.saveState.set('idle');
  }

  protected save(): void {
    if (!this.selectedFile) {
      return;
    }
    this.saveState.set('saving');
    this.homepageService.uploadHeroImage(this.selectedFile).subscribe({
      next: (config) => {
        this.cancelSelection();
        this.currentHeroUrl.set(config.hero_image_url ? `${this.baseUrl}${config.hero_image_url}` : null);
        this.saveState.set('idle');
      },
      error: () => this.saveState.set('error'),
    });
  }

  protected removeImage(): void {
    const confirmed = window.confirm(
      'Remove the current hero photograph? The standard ONE77 hero will be used until a new one is uploaded.',
    );
    if (!confirmed) {
      return;
    }
    this.actionError.set(null);
    this.homepageService.removeHeroImage().subscribe({
      next: () => this.currentHeroUrl.set(null),
      error: () => this.actionError.set('Unable to remove this image. Please try again.'),
    });
  }

  private revokeObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
