import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { AppRoleRequest, LookupItem } from '@core/models/app-role.model';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';

@Component({
  selector: 'app-app-role-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    MultiSelectModule,
    ButtonModule,
  ],
  templateUrl: './app-role-form.html',
  styleUrl: './app-role-form.css',
})
export class AppRoleForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly userOptions = signal<LookupItem[]>([]);

  protected readonly form = this.fb.group({
    roleId: ['', [Validators.required, Validators.maxLength(200)]],
    roleName: ['', [Validators.required, Validators.maxLength(200)]],
    permissionLevel: [100 as number, [Validators.required]],
    description: ['' as string | null, [Validators.required, Validators.maxLength(400)]],
    userIds: [[] as string[]],
  });

  private roleId: string | null = null;

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(this.roleId != null);

    forkJoin({
      users: this.lookups.getAppUsers(),
      role: this.roleId ? this.service.getById(this.roleId) : of(null),
    }).subscribe({
      next: ({ users, role }) => {
        this.userOptions.set(users);
        if (role) {
          this.form.patchValue({
            roleId: role.roleId,
            roleName: role.roleName,
            permissionLevel: role.permissionLevel,
            description: role.description,
            userIds: role.userIds,
          });
          this.form.controls.roleId.disable();
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
    const request: AppRoleRequest = {
      roleId: raw.roleId!.trim(),
      roleName: raw.roleName!.trim(),
      permissionLevel: raw.permissionLevel!,
      description: raw.description,
      userIds: raw.userIds ?? [],
    };

    this.saving.set(true);
    const op = this.isEdit() ? this.service.update(request) : this.service.create(request);
    op.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `角色「${request.roleId}」已儲存。` });
        this.router.navigate(['/app-roles', request.roleId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409
            ? `角色代碼「${request.roleId}」已存在。`
            : '儲存時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.roleId) {
      this.router.navigate(['/app-roles', this.roleId]);
    } else {
      this.router.navigate(['/app-roles']);
    }
  }

  protected invalid(control: string): boolean {
    const c = this.form.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
