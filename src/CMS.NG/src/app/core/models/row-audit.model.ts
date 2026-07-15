/**
 * One line of a record's change history, from GET /api/rowaudit?tableName=X&pkid=N.
 * Mirrors the backend RowAuditEntry projection (newest first).
 */
export interface RowAuditEntry {
  /** datetime — append 'Z' before piping through DatePipe (see conventions §Dates). */
  dateTime: string;
  userName: string;
  /** 'Insert' | 'Update' | 'Delete'. */
  actionType: string;
  /** Insert/Delete: the record's display value; Update: comma-separated changed column names. */
  actionDesc: string | null;
}
