import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserRequest } from '@core/models/app-user.model';
import { LookupItem } from '@core/models/app-role.model';
import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/services/auth.service';
import { LookupService } from '@core/services/lookup.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-app-user-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    CheckboxModule,
    MultiSelectModule,
    ButtonModule,
    RowAuditBadge,
  ],
  templateUrl: './app-user-form.html',
  styleUrl: './app-user-form.css',
})
export class AppUserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly auth = inject(AuthService);
  private readonly lookups = inject(LookupService);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly resetting = signal(false);
  protected readonly roleOptions = signal<LookupItem[]>([]);

  /** Numeric surrogate pkid of the edited user, for the audit-history badge (null in create mode). */
  protected readonly auditPkid = signal<number | null>(null);

  /** Gates the reset-password action; the backend enforces the same role with 403. */
  protected readonly isAdmin = computed(() => this.auth.hasRole('Admin'));

  protected readonly form = this.fb.group({
    userId: ['', [Validators.required, Validators.maxLength(200)]],
    userName: ['', [Validators.required, Validators.maxLength(200)]],
    isActive: [true],
    roleIds: [[] as string[]],
  });

  private userId: string | null = null;

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(this.userId != null);

    forkJoin({
      roles: this.lookups.getAppRoles(),
      user: this.userId ? this.service.getById(this.userId) : of(null),
    }).subscribe({
      next: ({ roles, user }) => {
        this.roleOptions.set(roles);
        if (user) {
          this.auditPkid.set(user.pkid);
          this.form.patchValue({
            userId: user.userId,
            userName: user.userName,
            isActive: user.isActive,
            roleIds: user.roleIds,
          });
          this.form.controls.userId.disable();
        }
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
    const request: AppUserRequest = {
      userId: raw.userId!.trim(),
      userName: raw.userName!.trim(),
      isActive: raw.isActive ?? true,
      roleIds: raw.roleIds ?? [],
    };

    this.saving.set(true);
    const op = this.isEdit() ? this.service.update(request) : this.service.create(request);
    op.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `使用者「${request.userId}」已儲存。` });
        this.router.navigate(['/app-users', request.userId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409
            ? `使用者代碼「${request.userId}」已存在。`
            : '儲存時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  /**
   * Reset the edited user's password to the system default (Admin only — the backend enforces the
   * role with 403). Sends only the UserId; no password or hash ever crosses the wire.
   */
  resetPassword(event: Event): void {
    if (!this.isEdit() || !this.userId) {
      return;
    }
    const targetId = this.userId;
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '重設密碼確認',
      message: `確定要將使用者「${targetId}」的密碼重設為系統預設值？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => {
        this.resetting.set(true);
        this.service.resetPassword(targetId).subscribe({
          next: () => {
            this.resetting.set(false);
            this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為系統預設值。' });
          },
          error: (err: HttpErrorResponse) => {
            this.resetting.set(false);
            const detail =
              err.status === 403 ? '僅系統管理員可重設密碼。' : '重設密碼時發生錯誤。';
            this.messages.add({ severity: 'error', summary: '重設失敗', detail });
          },
        });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.userId) {
      this.router.navigate(['/app-users', this.userId]);
    } else {
      this.router.navigate(['/app-users']);
    }
  }

  protected invalid(control: string): boolean {
    const c = this.form.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
