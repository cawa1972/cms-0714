import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  FeaturedPromoItem,
  FeaturedPromoItemQuery,
  FeaturedPromoItemRequest,
  PromoCodeLookup,
} from '@core/models/featured-promo-item.model';

@Injectable({ providedIn: 'root' })
export class FeaturedPromoItemService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/featured-promo-items`;

  getAll(): Observable<FeaturedPromoItem[]> {
    return this.http.get<FeaturedPromoItem[]>(this.baseUrl);
  }

  query(query: FeaturedPromoItemQuery): Observable<FeaturedPromoItem[]> {
    return this.http.post<FeaturedPromoItem[]>(`${this.baseUrl}/query`, query);
  }

  // pkid is numeric (int IDENTITY) — no encodeURIComponent needed.
  getById(pkid: number): Observable<FeaturedPromoItem> {
    return this.http.get<FeaturedPromoItem>(`${this.baseUrl}/${pkid}`);
  }

  create(request: FeaturedPromoItemRequest): Observable<FeaturedPromoItem> {
    return this.http.post<FeaturedPromoItem>(this.baseUrl, request);
  }

  update(request: FeaturedPromoItemRequest): Observable<FeaturedPromoItem> {
    return this.http.put<FeaturedPromoItem>(this.baseUrl, request);
  }

  delete(pkid: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pkid}`);
  }

  /** Board's + / − buttons: move one slot down/up, swapping with the occupant if any. */
  move(pkid: number, direction: 'up' | 'down'): Observable<void> {
    const params = new HttpParams().set('direction', direction);
    return this.http.post<void>(`${this.baseUrl}/${pkid}/move`, null, { params });
  }

  /** Resolve a Promotion2 PromoCode to its pkid + default Topic/Description. 404 if unknown. */
  lookupPromoCode(code: string): Observable<PromoCodeLookup> {
    const params = new HttpParams().set('code', code);
    return this.http.get<PromoCodeLookup>(`${this.baseUrl}/promo-lookup`, { params });
  }
}
