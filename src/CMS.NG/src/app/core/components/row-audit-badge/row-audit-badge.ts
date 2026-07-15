import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { RowAuditEntry } from '@core/models/row-audit.model';
import { RowAuditService } from '@core/services/row-audit.service';

/**
 * Toolbar badge showing a record's RowAudit history. Renders the most recent change inline
 * ("Update by alice · 2026-06-04 14:30"); clicking opens a dialog with the full audit trail,
 * newest first. Renders nothing while `pkid` is null (a form in create mode — no record yet).
 * Reusable on any detail/form page: pass the backend table name and the record's pkid.
 */
@Component({
  selector: 'app-row-audit-badge',
  imports: [DatePipe, DialogModule],
  templateUrl: './row-audit-badge.html',
  styleUrl: './row-audit-badge.css',
})
export class RowAuditBadge {
  /** Backend table name, e.g. "Course" — must match what the repositories audit under. */
  readonly tableName = input.required<string>();

  /** The record's pkid; null when the record does not exist yet (create mode). */
  readonly pkid = input.required<number | string | null>();

  private readonly service = inject(RowAuditService);

  protected readonly entries = signal<RowAuditEntry[]>([]);
  protected readonly dialogVisible = signal(false);

  /** Rows arrive newest first, so the inline summary is simply the first one. */
  protected readonly latest = computed(() => this.entries()[0] ?? null);

  protected readonly hasRecord = computed(() => this.pkid() !== null && this.pkid() !== '');

  constructor() {
    // Fetch whenever the record identity changes (and clear while there is no record).
    effect(() => {
      const tableName = this.tableName();
      const pkid = this.pkid();
      untracked(() => this.load(tableName, pkid));
    });
  }

  protected open(): void {
    // Refresh on open so the trail includes changes saved since the page loaded.
    this.load(this.tableName(), this.pkid());
    this.dialogVisible.set(true);
  }

  private load(tableName: string, pkid: number | string | null): void {
    if (pkid === null || pkid === '') {
      this.entries.set([]);
      return;
    }
    this.service.getHistory(tableName, pkid).subscribe({
      next: (rows) => this.entries.set(rows),
      error: () => this.entries.set([]),
    });
  }
}
