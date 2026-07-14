import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { PartnerRequest } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';

@Component({
  selector: 'app-partner-form',
  imports: [ReactiveFormsModule, InputTextModule, InputNumberModule, ButtonModule],
  templateUrl: './partner-form.html',
  styleUrl: './partner-form.css',
})
export class PartnerForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  // pkid is a smallint IDENTITY — never an editable control. In edit mode it is displayed read-only.
  protected readonly pkid = signal<number | null>(null);

  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    appKey: ['', [Validators.required, Validators.maxLength(10)]],
    nameOnPartnerMenu: ['', [Validators.required, Validators.maxLength(200)]],
    nameOnCourseDetailPage: ['', [Validators.required, Validators.maxLength(50)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    imageFilename: [null as string | null, [Validators.maxLength(50)]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam != null ? Number(idParam) : null;
    this.pkid.set(id);
    this.isEdit.set(id != null);

    if (id != null) {
      this.service.getById(id).subscribe({
        next: (partner) => {
          this.form.patchValue({
            name: partner.name,
            appKey: partner.appKey,
            nameOnPartnerMenu: partner.nameOnPartnerMenu,
            nameOnCourseDetailPage: partner.nameOnCourseDetailPage,
            displayOrder: partner.displayOrder,
            imageFilename: partner.imageFilename,
          });
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
    const request: PartnerRequest = {
      pkid: this.pkid() ?? 0,
      name: raw.name!.trim(),
      appKey: raw.appKey!.trim(),
      nameOnPartnerMenu: raw.nameOnPartnerMenu!.trim(),
      nameOnCourseDetailPage: raw.nameOnCourseDetailPage!.trim(),
      displayOrder: raw.displayOrder ?? 0,
      imageFilename: raw.imageFilename?.trim() || null,
    };

    this.saving.set(true);
    const op = this.isEdit() ? this.service.update(request) : this.service.create(request);
    op.subscribe({
      next: (saved) => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `合作廠商「${request.name}」已儲存。` });
        this.router.navigate(['/partners', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存時發生錯誤。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid() != null) {
      this.router.navigate(['/partners', this.pkid()]);
    } else {
      this.router.navigate(['/partners']);
    }
  }

  protected invalid(control: string): boolean {
    const c = this.form.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
