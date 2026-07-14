import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { Partner, PartnerQuery, PartnerRequest } from '@core/models/partner.model';

@Injectable({ providedIn: 'root' })
export class PartnerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/partners`;

  getAll(): Observable<Partner[]> {
    return this.http.get<Partner[]>(this.baseUrl);
  }

  query(query: PartnerQuery): Observable<Partner[]> {
    return this.http.post<Partner[]>(`${this.baseUrl}/query`, query);
  }

  // pkid is numeric (smallint) — no encodeURIComponent needed.
  getById(pkid: number): Observable<Partner> {
    return this.http.get<Partner>(`${this.baseUrl}/${pkid}`);
  }

  create(request: PartnerRequest): Observable<Partner> {
    return this.http.post<Partner>(this.baseUrl, request);
  }

  update(request: PartnerRequest): Observable<Partner> {
    return this.http.put<Partner>(this.baseUrl, request);
  }

  delete(pkid: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pkid}`);
  }
}
