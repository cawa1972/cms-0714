import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { CourseGroup } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';

@Component({
  selector: 'app-course-group-detail',
  imports: [ButtonModule],
  templateUrl: './course-group-detail.html',
  styleUrl: './course-group-detail.css',
})
export class CourseGroupDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messages = inject(MessageService);

  protected readonly group = signal<CourseGroup | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (group) => {
        this.group.set(group);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '找不到課程分類資料。' });
        this.loading.set(false);
        this.router.navigate(['/course-groups']);
      },
    });
  }

  edit(): void {
    const current = this.group();
    if (current) {
      this.router.navigate(['/course-groups', current.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/course-groups']);
  }
}
