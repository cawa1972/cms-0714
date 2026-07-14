import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  CourseGroup,
  CourseGroupQuery,
  CourseGroupRequest,
} from '@core/models/course-group.model';

@Injectable({ providedIn: 'root' })
export class CourseGroupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/course-groups`;

  getAll(): Observable<CourseGroup[]> {
    return this.http.get<CourseGroup[]>(this.baseUrl);
  }

  query(query: CourseGroupQuery): Observable<CourseGroup[]> {
    return this.http.post<CourseGroup[]>(`${this.baseUrl}/query`, query);
  }

  getById(pkid: number): Observable<CourseGroup> {
    return this.http.get<CourseGroup>(`${this.baseUrl}/${pkid}`);
  }

  create(request: CourseGroupRequest): Observable<CourseGroup> {
    return this.http.post<CourseGroup>(this.baseUrl, request);
  }

  update(request: CourseGroupRequest): Observable<CourseGroup> {
    return this.http.put<CourseGroup>(this.baseUrl, request);
  }

  delete(pkid: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pkid}`);
  }
}
