import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DatePipe } from '@angular/common';
import { MessageService } from 'primeng/api';

import { Course } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { QrCode } from '@core/components/qr-code/qr-code';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-course-detail',
  imports: [ButtonModule, TagModule, DatePipe, QrCode, RowAuditBadge],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.css',
})
export class CourseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly messages = inject(MessageService);

  protected readonly course = signal<Course | null>(null);
  protected readonly loading = signal(true);

  protected readonly qrCodeUrl = computed(() => {
    const c = this.course();
    return c ? `https://www.uuu.com.tw/Course/Show/${c.pkid}/${c.courseId}` : '';
  });

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (course) => {
        this.course.set(course);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '找不到課程資料。' });
        this.loading.set(false);
        this.router.navigate(['/courses']);
      },
    });
  }

  edit(): void {
    const current = this.course();
    if (current) {
      this.router.navigate(['/courses', current.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
