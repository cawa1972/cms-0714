import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DatePipe } from '@angular/common';
import { MessageService } from 'primeng/api';

import { Course } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import {
  buildFlyerFilename,
  filenameFromContentDisposition,
  saveBlob,
} from '@core/utils/file-download.util';
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
  protected readonly downloadingFlyer = signal(false);

  // Same URL template as the server-side CoursePublicUrl (src/CMS.API/Pdf/CoursePublicUrl.cs,
  // used for the flyer PDF's QR) — keep both in sync when the public site changes.
  protected readonly qrCodeUrl = computed(() => {
    const c = this.course();
    return c ? `https://www.uuu.com.tw/Course/Show/${c.pkid}/${encodeURIComponent(c.courseId)}` : '';
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

  /** Downloads the flyer PDF. The busy signal doubles as the double-click guard. */
  downloadFlyer(): void {
    const current = this.course();
    if (!current || this.downloadingFlyer()) {
      return;
    }

    this.downloadingFlyer.set(true);
    this.service.downloadFlyer(current.pkid).subscribe({
      next: (response) => {
        // Server-named file (RFC 5987 filename*, CORS-exposed); locally-built fallback.
        const filename =
          filenameFromContentDisposition(response.headers.get('Content-Disposition')) ??
          buildFlyerFilename(current.title, current.pkid);
        saveBlob(response.body!, filename);
        this.downloadingFlyer.set(false);
      },
      error: (error: unknown) => {
        this.downloadingFlyer.set(false);
        // 404 only: 5xx toasts are owned by the auth interceptor (avoids stacked toasts).
        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.messages.add({ severity: 'error', summary: '下載失敗', detail: '找不到課程資料。' });
        }
      },
    });
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
