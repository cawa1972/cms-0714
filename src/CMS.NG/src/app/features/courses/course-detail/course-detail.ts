import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  private readonly destroyRef = inject(DestroyRef);

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
    this.service
      .downloadFlyer(current.pkid)
      // Without this, navigating away mid-download leaves the subscription alive and
      // saveBlob fires from a dead view.
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          // Reset busy BEFORE saveBlob: a throw in the next handler is not routed to the
          // error callback, and would otherwise leave the button disabled forever.
          this.downloadingFlyer.set(false);
          // Server-named file (RFC 5987 filename*, CORS-exposed); locally-built fallback.
          const filename =
            filenameFromContentDisposition(response.headers.get('Content-Disposition')) ??
            buildFlyerFilename(current.title, current.pkid);
          saveBlob(response.body!, filename);
        },
        error: (error: unknown) => {
          this.downloadingFlyer.set(false);
          // Toast ownership: 401 → interceptor redirects to login; 5xx → interceptor toasts.
          // Everything else (404, other 4xx, status 0 = network down) is ours — silence on a
          // dead server is worse than a generic message.
          const status = error instanceof HttpErrorResponse ? error.status : 0;
          if (status === 404) {
            this.messages.add({ severity: 'error', summary: '下載失敗', detail: '找不到課程資料。' });
          } else if (status !== 401 && status < 500) {
            this.messages.add({ severity: 'error', summary: '下載失敗', detail: '請稍後再試。' });
          }
        },
      });
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
