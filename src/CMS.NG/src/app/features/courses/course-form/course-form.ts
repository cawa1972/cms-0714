import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { Course, CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { toIso, fromIso } from '@core/utils/date.util';

interface NumOption {
  value: number;
  label: string;
}

@Component({
  selector: 'app-course-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    SelectModule,
    DatePickerModule,
    CheckboxModule,
    ButtonModule,
  ],
  templateUrl: './course-form.html',
  styleUrl: './course-form.css',
})
export class CourseForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  // pkid is int IDENTITY — never an editable control. In edit mode it is displayed read-only.
  protected readonly pkid = signal<number | null>(null);

  protected readonly partnerOptions = signal<NumOption[]>([]);
  protected readonly courseGroupOptions = signal<NumOption[]>([]);
  protected readonly publishStatusOptions = signal<NumOption[]>([]);

  protected readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    officialTitle: [null as string | null, [Validators.maxLength(300)]],
    courseId: ['', [Validators.required, Validators.maxLength(50)]],
    prodCourseId: ['', [Validators.required, Validators.maxLength(50)]],
    friendlyUrl: ['', [Validators.required, Validators.maxLength(100)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    partnerPkid: [null as number | null, [Validators.required]],
    courseGroupPkid: [null as number | null],
    publishStatusPkid: [null as number | null, [Validators.required]],
    scheduleOn: [null as Date | null, [Validators.required]],
    scheduleOff: [null as Date | null, [Validators.required]],
    hour: [0, [Validators.required, Validators.min(0)]],
    listPrice: [0, [Validators.required, Validators.min(0)]],
    learningCredit: [0, [Validators.required, Validators.min(0)]],
    material: [null as string | null, [Validators.maxLength(500)]],
    objective: [null as string | null, [Validators.maxLength(4000)]],
    target: [null as string | null, [Validators.maxLength(500)]],
    prerequisites: [null as string | null, [Validators.maxLength(4000)]],
    outline: [null as string | null],
    towardCertOrExam: [null as string | null],
    note: [null as string | null, [Validators.maxLength(4000)]],
    otherInfo: [null as string | null, [Validators.maxLength(4000)]],
    canRepeat: [false],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam != null ? Number(idParam) : null;
    this.pkid.set(id);
    this.isEdit.set(id != null);

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

        if (id != null) {
          this.loadCourse(id);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得下拉選項。' });
        this.loading.set(false);
      },
    });
  }

  private loadCourse(id: number): void {
    this.service.getById(id).subscribe({
      next: (c: Course) => {
        this.form.patchValue({
          title: c.title,
          officialTitle: c.officialTitle,
          courseId: c.courseId,
          prodCourseId: c.prodCourseId,
          friendlyUrl: c.friendlyUrl,
          displayOrder: c.displayOrder,
          partnerPkid: c.partnerPkid,
          courseGroupPkid: c.courseGroupPkid,
          publishStatusPkid: c.publishStatusPkid,
          scheduleOn: fromIso(c.scheduleOn),
          scheduleOff: fromIso(c.scheduleOff),
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
        });
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入表單資料。' });
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messages.add({ severity: 'warn', summary: '欄位未填', detail: '請填寫所有必填欄位。' });
      return;
    }

    const raw = this.form.getRawValue();
    const request: CourseRequest = {
      pkid: this.pkid() ?? 0,
      title: raw.title!.trim(),
      officialTitle: raw.officialTitle?.trim() || null,
      courseId: raw.courseId!.trim(),
      prodCourseId: raw.prodCourseId!.trim(),
      friendlyUrl: raw.friendlyUrl!.trim(),
      displayOrder: raw.displayOrder ?? 0,
      partnerPkid: raw.partnerPkid!,
      courseGroupPkid: raw.courseGroupPkid ?? null,
      publishStatusPkid: raw.publishStatusPkid!,
      scheduleOn: toIso(raw.scheduleOn)!,
      scheduleOff: toIso(raw.scheduleOff)!,
      hour: raw.hour ?? 0,
      listPrice: raw.listPrice ?? 0,
      learningCredit: raw.learningCredit ?? 0,
      material: raw.material?.trim() || null,
      objective: raw.objective?.trim() || null,
      target: raw.target?.trim() || null,
      prerequisites: raw.prerequisites?.trim() || null,
      outline: raw.outline?.trim() || null,
      towardCertOrExam: raw.towardCertOrExam?.trim() || null,
      note: raw.note?.trim() || null,
      otherInfo: raw.otherInfo?.trim() || null,
      canRepeat: raw.canRepeat ?? false,
    };

    this.saving.set(true);
    const op = this.isEdit() ? this.service.update(request) : this.service.create(request);
    op.subscribe({
      next: (saved) => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `課程「${request.title}」已儲存。` });
        this.router.navigate(['/courses', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存時發生錯誤。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid() != null) {
      this.router.navigate(['/courses', this.pkid()]);
    } else {
      this.router.navigate(['/courses']);
    }
  }

  protected invalid(control: string): boolean {
    const c = this.form.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
