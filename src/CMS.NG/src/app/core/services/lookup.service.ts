import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { LookupItem } from '@core/models/app-role.model';

@Injectable({ providedIn: 'root' })
export class LookupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/lookups`;

  /** AppUser options for the role's user multiselect. */
  getAppUsers(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/app-users`);
  }

  /** Partner options (value = pkid, label = Name). FK target for Course/Certification. */
  getPartners(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/partners`);
  }
}
