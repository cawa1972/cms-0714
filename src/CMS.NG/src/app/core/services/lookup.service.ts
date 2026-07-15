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

  /** AppRole options for the user's role multiselect (value = roleId, label = "RoleName (RoleId)"). */
  getAppRoles(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/app-roles`);
  }

  /** Partner options (value = pkid, label = Name). FK target for Course/Certification. */
  getPartners(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/partners`);
  }

  /** CourseGroup options (value = pkid, label = Description). FK target for Course. */
  getCourseGroups(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/course-groups`);
  }

  /** PublishStatus options (value = pkid, label = Description). FK target for Course. */
  getPublishStatuses(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/publish-statuses`);
  }
}
