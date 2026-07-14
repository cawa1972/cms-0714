import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppRole, AppRoleQuery } from '@core/models/app-role.model';
import { AppRoleService } from '@core/services/app-role.service';

const FILTERS_KEY = 'app-role-list-filters';
const SORT_KEY = 'app-role-list-sort';
const PAGE_KEY = 'app-role-list-page';

@Component({
  selector: 'app-app-role-list',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    InputNumberModule,
    TooltipModule,
    TagModule,
  ],
  templateUrl: './app-role-list.html',
  styleUrl: './app-role-list.css',
})
export class AppRoleList implements OnInit {
  private readonly service = inject(AppRoleService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly roles = signal<AppRole[]>([]);
  protected readonly loading = signal(false);
  protected readonly drawerVisible = signal(false);

  protected filters: AppRoleQuery = { keyword: null, permissionLevel: null };

  protected sortField: string | undefined;
  protected sortOrder = 1;
  protected first = 0;
  protected rows = 20;

  ngOnInit(): void {
    this.restoreState();
    this.load();
  }

  private restoreState(): void {
    const savedFilters = this.readJson<AppRoleQuery>(FILTERS_KEY);
    if (savedFilters) {
      this.filters = { keyword: null, permissionLevel: null, ...savedFilters };
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
        this.roles.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得角色資料。' });
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
    this.filters = { keyword: null, permissionLevel: null };
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
    this.router.navigate(['/app-roles/new']);
  }

  view(role: AppRole): void {
    this.router.navigate(['/app-roles', role.roleId]);
  }

  edit(role: AppRole): void {
    this.router.navigate(['/app-roles', role.roleId, 'edit']);
  }

  remove(role: AppRole, event: Event): void {
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${role.pkid}</b>「${role.roleId}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.service.delete(role.roleId).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已刪除', detail: `角色「${role.roleId}」已刪除。` });
            this.load();
          },
          error: () => {
            this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除角色時發生錯誤。' });
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
