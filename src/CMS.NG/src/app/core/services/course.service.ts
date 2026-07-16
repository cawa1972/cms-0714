import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';

@Injectable({ providedIn: 'root' })
export class CourseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/courses`;

  getAll(): Observable<Course[]> {
    return this.http.get<Course[]>(this.baseUrl);
  }

  query(query: CourseQuery): Observable<Course[]> {
    return this.http.post<Course[]>(`${this.baseUrl}/query`, query);
  }

  // pkid is numeric (int IDENTITY) — no encodeURIComponent needed.
  getById(pkid: number): Observable<Course> {
    return this.http.get<Course>(`${this.baseUrl}/${pkid}`);
  }

  create(request: CourseRequest): Observable<Course> {
    return this.http.post<Course>(this.baseUrl, request);
  }

  update(request: CourseRequest): Observable<Course> {
    return this.http.put<Course>(this.baseUrl, request);
  }

  delete(pkid: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pkid}`);
  }

  /**
   * Downloads the course flyer PDF. Full response (not just the body) so the caller can read
   * the CORS-exposed Content-Disposition header for the server-named filename.
   */
  downloadFlyer(pkid: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/${pkid}/pdf`, {
      responseType: 'blob',
      observe: 'response',
    });
  }
}
