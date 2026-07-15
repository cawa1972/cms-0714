import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { FeaturedPromoItemService } from './featured-promo-item.service';
import {
  FeaturedPromoItem,
  FeaturedPromoItemRequest,
} from '@core/models/featured-promo-item.model';
import { environment } from '@env/environment';

const SAMPLE: FeaturedPromoItem = {
  pkid: 1,
  scheduleOn: '2026-03-16',
  trainingCenterPkid: 1,
  slot: 1,
  promotionPkid: 10,
  topic: '成為能AI協作的程式設計師',
  description: '轉職就業養成班，三大主流語言任你選',
  promoCode: '20251204_SkillTrainAI',
  trainingCenterName: '台北',
};

function newRequest(): FeaturedPromoItemRequest {
  return {
    pkid: 0,
    scheduleOn: '2026-03-18',
    trainingCenterPkid: 1,
    slot: 2,
    promotionPkid: 11,
    topic: 'Google AI工具一次掌握',
    description: '不需技術基礎！最新Google AI實戰課程',
  };
}

describe('FeaturedPromoItemService', () => {
  let service: FeaturedPromoItemService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/featured-promo-items`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FeaturedPromoItemService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeaturedPromoItemService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /featured-promo-items', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE]);
  });

  it('query POSTs the week bounds and training center to /query', () => {
    const query = {
      scheduleOnFrom: '2026-03-16',
      scheduleOnTo: '2026-03-22',
      trainingCenterPkid: 1,
    };
    service.query(query).subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(query);
    req.flush([SAMPLE]);
  });

  it('getById requests the numeric pkid in the URL', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
  });

  it('create POSTs the request body', () => {
    const request = newRequest();
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...SAMPLE, pkid: 7 });
  });

  it('update issues PUT with the request body', () => {
    const request = { ...newRequest(), pkid: 1 };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(SAMPLE);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('move POSTs to /{pkid}/move with the direction query param', () => {
    service.move(1, 'down').subscribe();
    const req = httpMock.expectOne((r) => r.url === `${base}/1/move`);
    expect(req.request.method).toBe('POST');
    expect(req.request.params.get('direction')).toBe('down');
    req.flush(null);
  });

  it('lookupPromoCode GETs /promo-lookup with the code param (URL-encoded)', () => {
    service.lookupPromoCode('20251204_SkillTrainAI').subscribe((promo) => {
      expect(promo.promotionPkid).toBe(10);
    });
    const req = httpMock.expectOne((r) => r.url === `${base}/promo-lookup`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('code')).toBe('20251204_SkillTrainAI');
    req.flush({
      promotionPkid: 10,
      promoCode: '20251204_SkillTrainAI',
      topic: '成為能AI協作的程式設計師',
      description: '轉職就業養成班',
    });
  });
});
