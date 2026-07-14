import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { AppRole, LookupItem } from '@core/models/app-role.model';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';

@Component({
  selector: 'app-app-role-detail',
  imports: [ButtonModule],
  templateUrl: './app-role-detail.html',
  styleUrl: './app-role-detail.css',
})
export class AppRoleDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly role = signal<AppRole | null>(null);
  protected readonly userLabels = signal<string[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      role: this.service.getById(id),
      users: this.lookups.getAppUsers(),
    }).subscribe({
      next: ({ role, users }) => {
        this.role.set(role);
        this.userLabels.set(this.mapUserLabels(role.userIds, users));
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '找不到角色資料。' });
        this.loading.set(false);
        this.router.navigate(['/app-roles']);
      },
    });
  }

  private mapUserLabels(userIds: string[], users: LookupItem[]): string[] {
    const byId = new Map(users.map((u) => [u.value, u.label]));
    return userIds.map((id) => byId.get(id) ?? id);
  }

  edit(): void {
    const current = this.role();
    if (current) {
      this.router.navigate(['/app-roles', current.roleId, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/app-roles']);
  }
}
