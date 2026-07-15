import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { AutoFocus } from 'primeng/autofocus';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';

import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { toIso, fromIso } from '@core/utils/date.util';

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

/**
 * Columns that support inline editing on the list table. The three read-only
 * columns — pkid (主代碼), partnerName (原廠), courseGroupDescription (課程群組) —
 * are intentionally excluded and never appear here.
 */
export type EditableField =
  | 'displayOrder'
  | 'courseId'
  | 'prodCourseId'
  | 'title'
  | 'publishStatusPkid'
  | 'scheduleOn'
  | 'scheduleOff'
  | 'hour'
  | 'listPrice'
  | 'learningCredit'
  | 'canRepeat';

const TEXT_FIELDS: EditableField[] = ['title', 'courseId', 'prodCourseId'];
const NUMBER_FIELDS: EditableField[] = ['displayOrder', 'hour', 'listPrice', 'learningCredit'];
const DATE_FIELDS: EditableField[] = ['scheduleOn', 'scheduleOff'];

type NumberField = 'displayOrder' | 'hour' | 'listPrice' | 'learningCredit';

/** Per-column numeric-editor bounds and precision. */
export interface NumberFieldConfig {
  min: number;
  max: number;
  /** Allowed decimal places — matches the DB column's scale. */
  fractionDigits: number;
}

/**
 * Bounds for the p-inputNumber editors, chosen to sit within the DB column type
 * while staying domain-reasonable:
 *   DisplayOrder int           → whole number, 0–9,999
 *   Hour         smallint      → whole number, 0–9,999 (smallint tops out at 32,767)
 *   ListPrice    decimal(9,0)  → whole number, 0–9,999,999
 *   LearningCredit decimal(9,1)→ one decimal place, 0–9,999.9
 */
export const NUMBER_FIELD_CONFIG: Record<NumberField, NumberFieldConfig> = {
  displayOrder: { min: 0, max: 9_999, fractionDigits: 0 },
  hour: { min: 0, max: 9_999, fractionDigits: 0 },
  listPrice: { min: 0, max: 9_999_999, fractionDigits: 0 },
  learningCredit: { min: 0, max: 9_999.9, fractionDigits: 1 },
};

@Component({
  selector: 'app-course-list',
  imports: [
    FormsModule,
    DatePipe,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    DatePickerModule,
    CheckboxModule,
    AutoFocus,
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

  // ---- Inline cell editing ----------------------------------------------
  // Which cell (row pkid + column) is currently in edit mode; null = none.
  protected readonly editingPkid = signal<number | null>(null);
  protected readonly editingField = signal<EditableField | null>(null);
  // Inline validation error shown under the active editor; null = valid.
  protected readonly editError = signal<string | null>(null);
  // Working value bound to the active editor (Date for date columns).
  protected editValue: unknown = null;
  // Guards blur from re-firing commit while an update request is in flight.
  private savingCell = false;
  // Numeric-editor bounds/precision, keyed by column; read from the template.
  protected readonly numberCfg = NUMBER_FIELD_CONFIG;

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

  // ---- Inline cell editing ----------------------------------------------

  /** True when the given cell is the one currently in edit mode. */
  isEditing(course: Course, field: EditableField): boolean {
    return this.editingPkid() === course.pkid && this.editingField() === field;
  }

  /** Enter edit mode for a cell. Triggered by double-click only. */
  startEdit(course: Course, field: EditableField): void {
    if (this.savingCell) {
      return;
    }
    this.editingPkid.set(course.pkid);
    this.editingField.set(field);
    this.editError.set(null);
    this.editValue = DATE_FIELDS.includes(field)
      ? fromIso(course[field] as string)
      : (course[field] as unknown);
  }

  /**
   * Panel editors (上架狀態 dropdown, 上架/下架日期 datepickers) persist on their
   * selection event (`onChange` / `onSelect`), not on blur: an option/date lives
   * in a body-appended overlay, and the mousedown that picks it blurs the input
   * *before* the value lands — a blur-based commit would fire with the old value
   * and tear the editor down before the pick registers. When such a panel closes
   * we drop out of edit mode, unless a save is in flight or a validation error is
   * still showing (which must keep the cell in edit mode).
   */
  onEditorPanelHide(course: Course): void {
    if (this.editingPkid() === course.pkid && !this.savingCell && !this.editError()) {
      this.cancelEdit();
    }
  }

  /** Enter commits, Escape cancels — for editors without native key bindings. */
  onEditorKeydown(event: KeyboardEvent, course: Course): void {
    if (event.key === 'Enter') {
      this.commitEdit(course);
    } else if (event.key === 'Escape') {
      this.cancelEdit();
    }
  }

  /** Leave edit mode without persisting; the row keeps its previous value. */
  cancelEdit(): void {
    this.editingPkid.set(null);
    this.editingField.set(null);
    this.editError.set(null);
    this.editValue = null;
  }

  /**
   * Validate then persist the active edit. Called on the editor's blur (and
   * Enter). On validation failure the cell stays in edit mode with an inline
   * error. On a failed save the cell reverts to its previous value.
   */
  commitEdit(course: Course): void {
    const field = this.editingField();
    if (!field || this.editingPkid() !== course.pkid || this.savingCell) {
      return;
    }

    const error = this.validate(field, this.editValue, course);
    if (error) {
      this.editError.set(error);
      return; // keep the cell in edit mode
    }

    const request = this.toRequest(course);
    this.applyEdit(request, field, this.editValue);

    // No effective change → just close the editor, no round-trip.
    if ((request as unknown as Record<string, unknown>)[field] === (course as unknown as Record<string, unknown>)[field]) {
      this.cancelEdit();
      return;
    }

    this.savingCell = true;
    this.service.update(request).subscribe({
      next: (saved) => {
        this.courses.update((list) => list.map((c) => (c.pkid === saved.pkid ? saved : c)));
        this.savingCell = false;
        this.cancelEdit();
        this.messages.add({ severity: 'success', summary: '已更新', detail: `主代碼 ${saved.pkid} 已更新。` });
      },
      error: () => {
        this.savingCell = false;
        this.cancelEdit(); // row is untouched, so this reverts the cell
        this.messages.add({
          severity: 'error',
          summary: '更新失敗',
          detail: '儲存變更時發生錯誤，已還原為原值。',
        });
      },
    });
  }

  /** Returns an error message for an invalid edit, or null when valid. */
  private validate(field: EditableField, value: unknown, course: Course): string | null {
    if (TEXT_FIELDS.includes(field)) {
      if (!String(value ?? '').trim()) {
        return '此欄位為必填，不可清空。';
      }
      return null;
    }
    if (NUMBER_FIELDS.includes(field)) {
      if (value === null || value === undefined || value === '') {
        return '此欄位為必填，請輸入數值。';
      }
      const n = Number(value);
      if (Number.isNaN(n)) {
        return '請輸入有效的數值。';
      }
      const cfg = NUMBER_FIELD_CONFIG[field as NumberField];
      if (n < cfg.min) {
        return `數值不可小於 ${cfg.min}。`;
      }
      if (n > cfg.max) {
        return `數值不可大於 ${cfg.max.toLocaleString()}。`;
      }
      return null;
    }
    if (DATE_FIELDS.includes(field)) {
      if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
        return '請輸入有效的日期。';
      }
      const on = field === 'scheduleOn' ? value : fromIso(course.scheduleOn);
      const off = field === 'scheduleOff' ? value : fromIso(course.scheduleOff);
      if (on && off && on.getTime() > off.getTime()) {
        return '上架日期不可晚於下架日期。';
      }
      return null;
    }
    if (field === 'publishStatusPkid') {
      if (value === null || value === undefined) {
        return '請選擇上架狀態。';
      }
      return null;
    }
    return null; // canRepeat — boolean, always valid
  }

  /** Build a full write DTO from a row so a single edited field can be sent. */
  private toRequest(c: Course): CourseRequest {
    return {
      pkid: c.pkid,
      title: c.title,
      officialTitle: c.officialTitle,
      courseId: c.courseId,
      prodCourseId: c.prodCourseId,
      friendlyUrl: c.friendlyUrl,
      displayOrder: c.displayOrder,
      partnerPkid: c.partnerPkid,
      courseGroupPkid: c.courseGroupPkid,
      publishStatusPkid: c.publishStatusPkid,
      scheduleOn: c.scheduleOn,
      scheduleOff: c.scheduleOff,
      hour: c.hour,
      listPrice: c.listPrice,
      learningCredit: c.learningCredit,
      material: c.material,
      objective: c.objective,
      target: c.target,
      prerequisites: c.prerequisites,
      outline: c.outline,
      towardCertOrExam: c.towardCertOrExam,
      note: c.note,
      otherInfo: c.otherInfo,
      canRepeat: c.canRepeat,
    };
  }

  /** Write the (already-validated) editor value onto the request DTO. */
  private applyEdit(request: CourseRequest, field: EditableField, value: unknown): void {
    if (TEXT_FIELDS.includes(field)) {
      (request as unknown as Record<string, unknown>)[field] = String(value).trim();
    } else if (NUMBER_FIELDS.includes(field)) {
      (request as unknown as Record<string, unknown>)[field] = Number(value);
    } else if (DATE_FIELDS.includes(field)) {
      (request as unknown as Record<string, unknown>)[field] = toIso(value as Date);
    } else if (field === 'publishStatusPkid') {
      request.publishStatusPkid = Number(value);
    } else if (field === 'canRepeat') {
      request.canRepeat = !!value;
    }
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
