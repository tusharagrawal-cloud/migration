import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminLearnService } from '../../core/services/admin-learn.service';
import {
  AdminLearnCategoryWithCount,
  AdminLearnEntryWithCategoryName,
  LearnStatus,
  SaveLearnEntryRequest,
} from '../../core/models/admin-learn.models';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';
type SaveState = 'idle' | 'saving' | 'error';

@Component({
  selector: 'app-admin-learn-entries-page',
  imports: [FormsModule, RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <nav class="sub-tabs">
      <a routerLink="/admin/learn" class="sub-tab-link">Categories</a>
      <span class="sub-tab active">Articles</span>
    </nav>

    <div class="header-row">
      <div>
        <h1>Learn articles</h1>
        <p class="lede">Write and manage the articles inside each category.</p>
      </div>
      <button type="button" class="admin-btn admin-btn-primary" [disabled]="categories().length === 0" (click)="startCreate()">
        + Add article
      </button>
    </div>

    @if (formOpen()) {
      <app-admin-card>
        <h2>{{ editingId() ? 'Edit article' : 'New article' }}</h2>
        <div class="form">
          <label class="admin-field">
            Title
            <input type="text" [(ngModel)]="title" name="entryTitle" />
          </label>
          <label class="admin-field">
            Category
            <select [(ngModel)]="categoryId" name="entryCategory">
              @for (cat of categories(); track cat.id) {
                <option [ngValue]="cat.id">{{ cat.name }}</option>
              }
            </select>
          </label>
          <label class="admin-field">
            Body
            <textarea rows="8" [(ngModel)]="body" name="entryBody"></textarea>
          </label>
          <label class="admin-field">
            Status
            <select [(ngModel)]="status" name="entryStatus">
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
        <app-admin-loading label="Loading articles…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load articles. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (actionError()) {
          <p class="save-error" role="alert">{{ actionError() }}</p>
        }
        @if (entries().length === 0) {
          <app-admin-empty heading="No articles yet" body="Add a category first if you haven't already." />
        } @else {
          <div class="list">
            @for (entry of entries(); track entry.id) {
              <app-admin-card>
                <div class="row">
                  <div class="info">
                    <p class="title">{{ entry.title }}</p>
                    <p class="meta">{{ entry.category_name }}</p>
                  </div>
                  <span class="admin-badge" [class]="statusBadgeClass(entry.status)">{{ entry.status }}</span>
                  <div class="actions">
                    <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="startEdit(entry)">Edit</button>
                    @if (entry.status !== 'Live') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="publish(entry.id)">Show on Website</button>
                    }
                    @if (entry.status === 'Live') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="archive(entry.id)">Hide from Website</button>
                    }
                    @if (entry.status === 'Hidden') {
                      <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="unarchive(entry.id)">Move to Draft</button>
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
        max-width: 560px;
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
export class AdminLearnEntriesPage implements OnInit {
  private readonly learnService = inject(AdminLearnService);

  protected state = signal<LoadState>('loading');
  protected saveState = signal<SaveState>('idle');
  protected actionError = signal<string | null>(null);
  protected entries = signal<AdminLearnEntryWithCategoryName[]>([]);
  protected categories = signal<AdminLearnCategoryWithCount[]>([]);
  protected formOpen = signal(false);
  protected editingId = signal<number | null>(null);

  protected title = '';
  protected body = '';
  protected categoryId: number | null = null;
  protected status: LearnStatus = 'Draft';

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.learnService.getCategories().subscribe({
      next: (catRes) => {
        this.categories.set(catRes.items);
        this.learnService.getEntries().subscribe({
          next: (entryRes) => {
            this.entries.set(entryRes.items);
            this.state.set('loaded');
          },
          error: () => this.state.set('error'),
        });
      },
      error: () => this.state.set('error'),
    });
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.title = '';
    this.body = '';
    this.categoryId = this.categories()[0]?.id ?? null;
    this.status = 'Draft';
    this.formOpen.set(true);
  }

  protected startEdit(entry: AdminLearnEntryWithCategoryName): void {
    this.editingId.set(entry.id);
    this.title = entry.title;
    this.body = entry.body;
    this.categoryId = entry.category_id;
    this.status = entry.status;
    this.formOpen.set(true);
  }

  protected cancelForm(): void {
    this.formOpen.set(false);
    this.saveState.set('idle');
  }

  protected save(): void {
    if (!this.title.trim() || this.categoryId == null) {
      return;
    }
    this.saveState.set('saving');
    const request: SaveLearnEntryRequest = {
      category_id: this.categoryId,
      title: this.title.trim(),
      body: this.body,
      sort_priority: 0,
      status: this.status,
    };

    const id = this.editingId();
    const save$ = id ? this.learnService.updateEntry(id, request) : this.learnService.createEntry(request);
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
    this.learnService.publishEntry(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this article. Please try again.'),
    });
  }

  protected archive(id: number): void {
    this.actionError.set(null);
    this.learnService.archiveEntry(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this article. Please try again.'),
    });
  }

  protected unarchive(id: number): void {
    this.actionError.set(null);
    this.learnService.unarchiveEntry(id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Unable to update this article. Please try again.'),
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
