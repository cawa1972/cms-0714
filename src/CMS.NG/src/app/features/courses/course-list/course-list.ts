import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';

import { Course, CourseQuery } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { toIso } from '@core/utils/date.util';

const FILTERS_KEY = 'course-list-filters';
const SORT_KEY = 'course-list-sort';
const PAGE_KEY = 'course-list-page';

const EMPTY_FILTERS: CourseQuery = {
  keyword: null,
  partnerPkid: null,
  courseGroupPkid: null,
  publishStatusPkid: null,
  scheduleOnFrom: null,
  scheduleOnTo: null,
  scheduleOffFrom: null,
  scheduleOffTo: null,
  canRepeat: null,
};

interface NumOption {
  value: number;
  label: string;
}

@Component({
  selector: 'app-course-list',
  imports: [
    FormsModule,
    DatePipe,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    DatePickerModule,
    TooltipModule,
    TagModule,
  ],
  templateUrl: './course-list.html',
  styleUrl: './course-list.css',
})
export class CourseList implements OnInit {
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly courses = signal<Course[]>([]);
  protected readonly loading = signal(false);
  protected readonly drawerVisible = signal(false);

  protected readonly partnerOptions = signal<NumOption[]>([]);
  protected readonly courseGroupOptions = signal<NumOption[]>([]);
  protected readonly publishStatusOptions = signal<NumOption[]>([]);

  /** Tri-state options for the CanRepeat filter. */
  protected readonly triStateOptions = [
    { label: '全部', value: null },
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  // Bound to two p-datepickers per range; converted to ISO on apply.
  protected scheduleOnFrom: Date | null = null;
  protected scheduleOnTo: Date | null = null;
  protected scheduleOffFrom: Date | null = null;
  protected scheduleOffTo: Date | null = null;

  protected filters: CourseQuery = { ...EMPTY_FILTERS };

  protected sortField: string | undefined;
  protected sortOrder = 1;
  protected first = 0;
  protected rows = 20;

  ngOnInit(): void {
    this.restoreState();
    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses }) => {
        this.partnerOptions.set(partners.map((o) => ({ value: Number(o.value), label: o.label })));
        this.courseGroupOptions.set(
          courseGroups.map((o) => ({ value: Number(o.value), label: o.label })),
        );
        this.publishStatusOptions.set(
          publishStatuses.map((o) => ({ value: Number(o.value), label: o.label })),
        );
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得下拉選項。' });
      },
    });
    this.load();
  }

  private restoreState(): void {
    const savedFilters = this.readJson<CourseQuery>(FILTERS_KEY);
    if (savedFilters) {
      this.filters = { ...EMPTY_FILTERS, ...savedFilters };
      this.scheduleOnFrom = this.filters.scheduleOnFrom ? new Date(this.filters.scheduleOnFrom + 'T00:00:00') : null;
      this.scheduleOnTo = this.filters.scheduleOnTo ? new Date(this.filters.scheduleOnTo + 'T00:00:00') : null;
      this.scheduleOffFrom = this.filters.scheduleOffFrom ? new Date(this.filters.scheduleOffFrom + 'T00:00:00') : null;
      this.scheduleOffTo = this.filters.scheduleOffTo ? new Date(this.filters.scheduleOffTo + 'T00:00:00') : null;
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
        this.courses.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程資料。' });
        this.loading.set(false);
      },
    });
  }

  applyFilters(): void {
    this.filters.scheduleOnFrom = toIso(this.scheduleOnFrom);
    this.filters.scheduleOnTo = toIso(this.scheduleOnTo);
    this.filters.scheduleOffFrom = toIso(this.scheduleOffFrom);
    this.filters.scheduleOffTo = toIso(this.scheduleOffTo);
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filters));
    this.first = 0;
    this.persistPage();
    this.drawerVisible.set(false);
    this.load();
  }

  clearFilters(): void {
    this.filters = { ...EMPTY_FILTERS };
    this.scheduleOnFrom = this.scheduleOnTo = this.scheduleOffFrom = this.scheduleOffTo = null;
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
    this.router.navigate(['/courses/new']);
  }

  view(course: Course): void {
    this.router.navigate(['/courses', course.pkid]);
  }

  edit(course: Course): void {
    this.router.navigate(['/courses', course.pkid, 'edit']);
  }

  remove(course: Course, event: Event): void {
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${course.pkid}</b>「${course.courseId} ${course.title}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.service.delete(course.pkid).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已刪除', detail: `課程「${course.title}」已刪除。` });
            this.load();
          },
          error: () => {
            this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除課程時發生錯誤。' });
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
