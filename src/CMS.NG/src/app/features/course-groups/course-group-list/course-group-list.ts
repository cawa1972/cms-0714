import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CourseGroup, CourseGroupQuery } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';

const FILTERS_KEY = 'course-group-list-filters';
const SORT_KEY = 'course-group-list-sort';
const PAGE_KEY = 'course-group-list-page';

const EMPTY_FILTERS: CourseGroupQuery = {
  keyword: null,
};

@Component({
  selector: 'app-course-group-list',
  imports: [FormsModule, TableModule, ButtonModule, DrawerModule, InputTextModule, TooltipModule],
  templateUrl: './course-group-list.html',
  styleUrl: './course-group-list.css',
})
export class CourseGroupList implements OnInit {
  private readonly service = inject(CourseGroupService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly groups = signal<CourseGroup[]>([]);
  protected readonly loading = signal(false);
  protected readonly drawerVisible = signal(false);

  protected filters: CourseGroupQuery = { ...EMPTY_FILTERS };

  protected sortField: string | undefined;
  protected sortOrder = 1;
  protected first = 0;
  protected rows = 20;

  ngOnInit(): void {
    this.restoreState();
    this.load();
  }

  private restoreState(): void {
    const savedFilters = this.readJson<CourseGroupQuery>(FILTERS_KEY);
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
        this.groups.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程分類資料。' });
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
    this.router.navigate(['/course-groups/new']);
  }

  view(group: CourseGroup): void {
    this.router.navigate(['/course-groups', group.pkid]);
  }

  edit(group: CourseGroup): void {
    this.router.navigate(['/course-groups', group.pkid, 'edit']);
  }

  remove(group: CourseGroup, event: Event): void {
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${group.pkid}</b>「${group.description}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.service.delete(group.pkid).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已刪除', detail: `課程分類「${group.description}」已刪除。` });
            this.load();
          },
          error: () => {
            this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除課程分類時發生錯誤。' });
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
