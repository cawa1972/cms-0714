import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { RowAuditEntry } from '@core/models/row-audit.model';

@Injectable({ providedIn: 'root' })
export class RowAuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rowaudit`;

  /** Change history of one record (by business table + pkid), newest first. */
  getHistory(tableName: string, pkid: number | string): Observable<RowAuditEntry[]> {
    return this.http.get<RowAuditEntry[]>(this.baseUrl, {
      params: { tableName, pkid },
    });
  }
}
