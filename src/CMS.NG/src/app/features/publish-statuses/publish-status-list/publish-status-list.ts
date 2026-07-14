import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';

import { PublishStatus, PublishStatusQuery } from '@core/models/publish-status.model';
import { PublishStatusService } from '@core/services/publish-status.service';

const FILTERS_KEY = 'publish-status-list-filters';
const SORT_KEY = 'publish-status-list-sort';
const PAGE_KEY = 'publish-status-list-page';

const EMPTY_FILTERS: PublishStatusQuery = {
  keyword: null,
  isDraft: null,
  isPublished: null,
  isDiscontinued: null,
};

@Component({
  selector: 'app-publish-status-list',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    TooltipModule,
    TagModule,
  ],
  templateUrl: './publish-status-list.html',
  styleUrl: './publish-status-list.css',
})
export class PublishStatusList implements OnInit {
  private readonly service = inject(PublishStatusService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly statuses = signal<PublishStatus[]>([]);
  protected readonly loading = signal(false);
  protected readonly drawerVisible = signal(false);

  /** Tri-state options for the bit-flag filters. */
  protected readonly triStateOptions = [
    { label: '全部', value: null },
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  protected filters: PublishStatusQuery = { ...EMPTY_FILTERS };

  protected sortField: string | undefined;
  protected sortOrder = 1;
  protected first = 0;
  protected rows = 20;

  ngOnInit(): void {
    this.restoreState();
    this.load();
  }

  private restoreState(): void {
    const savedFilters = this.readJson<PublishStatusQuery>(FILTERS_KEY);
    if (savedFilters) {
      this.filters = { ...EMPTY_FILTERS, ...savedFilters };
    }
    const savedSort = this.readJson<{ sortField?: string; sortOrder: number }>(SORT_KEY);
    if (savedSort) {
      this.sortField = savedSort.sortField;
      this.sortOrder = savedSort.sortOrder;
    }
    const savedPage = this.readJson<{ first: number; rows: number }>(PAGE_KEY);
    if (savedPage) {
      this.first = savedPage.first;
      this.rows = savedPage.rows;
    }
  }

  load(): void {
    this.loading.set(true);
    this.service.query(this.filters).subscribe({
      next: (data) => {
        this.statuses.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得發布狀態資料。' });
        this.loading.set(false);
      },
    });
  }

  applyFilters(): void {
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filters));
    this.first = 0;
    this.persistPage();
    this.drawerVisible.set(false);
    this.load();
  }

  clearFilters(): void {
    this.filters = { ...EMPTY_FILTERS };
    sessionStorage.removeItem(FILTERS_KEY);
    this.first = 0;
    this.persistPage();
    this.load();
  }

  onSort(event: { field?: string; order?: number }): void {
    this.sortField = event.field;
    this.sortOrder = event.order ?? 1;
    sessionStorage.setItem(
      SORT_KEY,
      JSON.stringify({ sortField: this.sortField, sortOrder: this.sortOrder }),
    );
  }

  onPage(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? this.rows;
    this.persistPage();
  }

  private persistPage(): void {
    sessionStorage.setItem(PAGE_KEY, JSON.stringify({ first: this.first, rows: this.rows }));
  }

  add(): void {
    this.router.navigate(['/publish-statuses/new']);
  }

  view(status: PublishStatus): void {
    this.router.navigate(['/publish-statuses', status.pkid]);
  }

  edit(status: PublishStatus): void {
    this.router.navigate(['/publish-statuses', status.pkid, 'edit']);
  }

  remove(status: PublishStatus, event: Event): void {
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${status.pkid}</b>「${status.description}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.service.delete(status.pkid).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已刪除', detail: `發布狀態「${status.description}」已刪除。` });
            this.load();
          },
          error: () => {
            this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除發布狀態時發生錯誤。' });
          },
        });
      },
    });
  }

  private readJson<T>(key: string): T | null {
    const raw = sessionStorage.getItem(key);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  }
}
