import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminLearnService } from '../../core/services/admin-learn.service';
import { AdminLearnCategoryWithCount, LearnStatus, SaveLearnCategoryRequest } from '../../core/models/admin-learn.models';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';
type SaveState = 'idle' | 'saving' | 'error';

@Component({
  selector: 'app-admin-learn-categories-page',
  imports: [FormsModule, RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <nav class="sub-tabs">
      <span class="sub-tab active">Categories</span>
      <a routerLink="/admin/learn/entries" class="sub-tab-link">Articles</a>
    </nav>

    <div class="header-row">
      <div>
        <h1>Learn categories</h1>
        <p class="lede">Organize the guides shown to customers.</p>
      </div>
      <button type="button" class="admin-btn admin-btn-primary" (click)="startCreate()">+ Add category</button>
    </div>

    @if (formOpen()) {
      <app-admin-card>
        <h2>{{ editingId() ? 'Edit category' : 'New category' }}</h2>
        <div class="form">
          <label class="admin-field">
            Name
            <input type="text" [(ngModel)]="name" name="catName" />
          </label>
          <label class="admin-field">
            Description
            <textarea rows="2" [(ngModel)]="description" name="catDescription"></textarea>
          </label>
          <label class="admin-field">
            Status
            <select [(ngModel)]="status" name="catStatus">
              <option value="Draft">Draft</option>
              <option value="Live">Live (visible to customers)</option>
              <option value="Hidden">Hidden</option>
            </select>
          </label>
          @if (saveState() === 'error') {
            <p class="save-error" role="alert">Unable to save. Please try again.</p>
          }
          <div class="form-actions">
            <button type="button" class="admin-btn admin-btn-secondary" (click)="cancelForm()">Cancel</button>
            <button type="button" class="admin-btn admin-btn-primary" [disabled]="saveState() === 'saving'" (click)="save()">
              {{ saveState() === 'saving' ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </div>
      </app-admin-card>
    }

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading categories…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load categories. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (actionError()) {
          <p class="save-error" role="alert">{{ actionError() }}</p>
        }
        @if (categories().length === 0) {
          <app-admin-empty heading="No categories yet" />
        } @else {
          <div class="list">
            @for (cat of categories(); track cat.id) {
              <app-admin-card>
                <div class="row">
                  <div class="info">
                    <p class="title">{{ cat.name }}</p>
                    <p class="meta">{{ cat.entry_count }} article(s)</p>
                  </div>
                  <span class="admin-badge" [class]="statusBadgeClass(cat.status)">{{ cat.status }}</span>
                  <div class="actions">
                    <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="startEdit(cat)">Edit</button>
                    @if (cat.status !== 'Live') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="publish(cat.id)">Show on Website</button>
                    }
                    @if (cat.status === 'Live') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="archive(cat.id)">Hide from Website</button>
                    }
                    @if (cat.status === 'Hidden') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="unarchive(cat.id)">Move to Draft</button>
                    }
                  </div>
                </div>
              </app-admin-card>
            }
          </div>
        }
      }
    }
  `,
  styles: [
    `
      .sub-tabs {
        display: flex;
        gap: var(--admin-space-4);
        margin-bottom: var(--admin-space-5);
        font-size: 0.875rem;
      }
      .sub-tab {
        font-weight: 600;
        color: var(--admin-accent);
      }
      .sub-tab-link {
        color: var(--admin-text-muted);
        text-decoration: none;
      }
      .header-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: var(--admin-space-4);
        margin-bottom: var(--admin-space-5);
        flex-wrap: wrap;
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
      .form {
        display: grid;
        gap: var(--admin-space-4);
        max-width: 480px;
      }
      .form-actions {
        display: flex;
        gap: var(--admin-space-3);
      }
      .save-error {
        color: var(--admin-danger);
        margin: 0;
        font-size: 0.875rem;
      }
      .list {
        display: grid;
        gap: var(--admin-space-3);
        margin-top: var(--admin-space-5);
      }
      .row {
        display: flex;
        align-items: center;
        gap: var(--admin-space-4);
      }
      .info {
        flex: 1;
        min-width: 0;
      }
      .title {
        margin: 0;
        font-weight: 500;
      }
      .meta {
        margin: var(--admin-space-1) 0 0;
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
      }
      .actions {
        display: flex;
        gap: var(--admin-space-2);
        flex-wrap: wrap;
      }
    `,
  ],
})
export class AdminLearnCategoriesPage implements OnInit {
  private readonly learnService = inject(AdminLearnService);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected actionError = signal<string | null>(null);
  protected categories = signal<AdminLearnCategoryWithCount[]>([]);
  protected formOpen = signal(false);
  protected editingId = signal<number | null>(null);

  protected name = '';
  protected description = '';
  protected status: LearnStatus = 'Draft';

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.learnService.getCategories().subscribe({
      next: (res) => {
        this.categories.set(res.items);
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.name = '';
    this.description = '';
    this.status = 'Draft';
    this.formOpen.set(true);
  }

  protected startEdit(cat: AdminLearnCategoryWithCount): void {
    this.editingId.set(cat.id);
    this.name = cat.name;
    this.description = cat.description;
    this.status = cat.status;
    this.formOpen.set(true);
  }

  protected cancelForm(): void {
    this.formOpen.set(false);
    this.saveState.set('idle');
  }

  protected save(): void {
    if (!this.name.trim()) {
      return;
    }
    this.saveState.set('saving');
    const request: SaveLearnCategoryRequest = {
      name: this.name.trim(),
      description: this.description.trim(),
      sort_priority: 0,
      status: this.status,
    };

    const id = this.editingId();
    const save$ = id ? this.learnService.updateCategory(id, request) : this.learnService.createCategory(request);
    save$.subscribe({
      next: () => {
        this.formOpen.set(false);
        this.saveState.set('idle');
        this.load();
      },
      error: () => this.saveState.set('error'),
    });
  }

  protected publish(id: number): void {
    this.actionError.set(null);
    this.learnService.publishCategory(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this category. Please try again.'),
    });
  }

  protected archive(id: number): void {
    this.actionError.set(null);
    this.learnService.archiveCategory(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this category. Please try again.'),
    });
  }

  protected unarchive(id: number): void {
    this.actionError.set(null);
    this.learnService.unarchiveCategory(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this category. Please try again.'),
    });
  }

  protected statusBadgeClass(status: string): string {
    switch (status) {
      case 'Live':
        return 'admin-badge-success';
      case 'Hidden':
        return 'admin-badge';
      default:
        return 'admin-badge-warning';
    }
  }
}
