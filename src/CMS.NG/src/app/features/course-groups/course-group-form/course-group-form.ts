import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { CourseGroupRequest } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';

@Component({
  selector: 'app-course-group-form',
  imports: [ReactiveFormsModule, InputTextModule, ButtonModule],
  templateUrl: './course-group-form.html',
  styleUrl: './course-group-form.css',
})
export class CourseGroupForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  /** The pkid is database-assigned (IDENTITY) — not part of the editable form, shown read-only in edit mode. */
  protected readonly pkid = signal<number | null>(null);

  protected readonly form = this.fb.group({
    description: ['', [Validators.required, Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam != null ? Number(idParam) : null;
    this.isEdit.set(id != null);

    if (id != null) {
      this.service.getById(id).subscribe({
        next: (group) => {
          this.pkid.set(group.pkid);
          this.form.patchValue({ description: group.description });
          this.loading.set(false);
        },
        error: () => {
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入表單資料。' });
          this.loading.set(false);
        },
      });
    } else {
      this.loading.set(false);
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messages.add({ severity: 'warn', summary: '欄位未填', detail: '請填寫所有必填欄位。' });
      return;
    }

    const raw = this.form.getRawValue();
    const request: CourseGroupRequest = {
      pkid: this.pkid() ?? 0,
      description: raw.description!.trim(),
    };

    this.saving.set(true);
    const op = this.isEdit() ? this.service.update(request) : this.service.create(request);
    op.subscribe({
      next: (saved) => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `課程分類「${request.description}」已儲存。` });
        this.router.navigate(['/course-groups', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存時發生錯誤。' });
      },
    });
  }

  cancel(): void {
    const id = this.pkid();
    if (this.isEdit() && id != null) {
      this.router.navigate(['/course-groups', id]);
    } else {
      this.router.navigate(['/course-groups']);
    }
  }

  protected invalid(control: string): boolean {
    const c = this.form.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
