import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { Partner } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';

@Component({
  selector: 'app-partner-detail',
  imports: [ButtonModule],
  templateUrl: './partner-detail.html',
  styleUrl: './partner-detail.css',
})
export class PartnerDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messages = inject(MessageService);

  protected readonly partner = signal<Partner | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (partner) => {
        this.partner.set(partner);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '找不到合作廠商資料。' });
        this.loading.set(false);
        this.router.navigate(['/partners']);
      },
    });
  }

  edit(): void {
    const current = this.partner();
    if (current) {
      this.router.navigate(['/partners', current.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/partners']);
  }
}
