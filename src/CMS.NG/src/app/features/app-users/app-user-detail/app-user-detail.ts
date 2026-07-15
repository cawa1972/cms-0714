import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUser } from '@core/models/app-user.model';
import { LookupItem } from '@core/models/app-role.model';
import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/services/auth.service';
import { LookupService } from '@core/services/lookup.service';

@Component({
  selector: 'app-app-user-detail',
  imports: [DatePipe, ButtonModule, TagModule],
  templateUrl: './app-user-detail.html',
  styleUrl: './app-user-detail.css',
})
export class AppUserDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly auth = inject(AuthService);
  private readonly lookups = inject(LookupService);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly user = signal<AppUser | null>(null);
  protected readonly roleLabels = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly resetting = signal(false);

  /** Gates the reset-password action; the backend enforces the same role with 403. */
  protected readonly isAdmin = computed(() => this.auth.hasRole('Admin'));

  private userId!: string;

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id')!;
    this.reload();
  }

  private reload(): void {
    forkJoin({
      user: this.service.getById(this.userId),
      roles: this.lookups.getAppRoles(),
    }).subscribe({
      next: ({ user, roles }) => {
        this.user.set(user);
        this.roleLabels.set(this.mapRoleLabels(user.roleIds, roles));
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '找不到使用者資料。' });
        this.loading.set(false);
        this.router.navigate(['/app-users']);
      },
    });
  }

  private mapRoleLabels(roleIds: string[], roles: LookupItem[]): string[] {
    const byId = new Map(roles.map((r) => [r.value, r.label]));
    return roleIds.map((id) => byId.get(id) ?? id);
  }

  resetPassword(event: Event): void {
    const current = this.user();
    if (!current) {
      return;
    }
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '重設密碼確認',
      message: `確定要將使用者「${current.userId}」的密碼重設為系統預設值？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => {
        this.resetting.set(true);
        this.service.resetPassword(current.userId).subscribe({
          next: () => {
            this.resetting.set(false);
            this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為系統預設值。' });
            this.reload();
          },
          error: () => {
            this.resetting.set(false);
            this.messages.add({ severity: 'error', summary: '重設失敗', detail: '重設密碼時發生錯誤。' });
          },
        });
      },
    });
  }

  edit(): void {
    const current = this.user();
    if (current) {
      this.router.navigate(['/app-users', current.userId, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/app-users']);
  }
}
