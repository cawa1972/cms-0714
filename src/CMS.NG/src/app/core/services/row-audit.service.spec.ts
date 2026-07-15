import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RowAuditService } from './row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';
import { environment } from '@env/environment';

describe('RowAuditService', () => {
  let service: RowAuditService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/rowaudit`;

  const sample: RowAuditEntry[] = [
    { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title' },
    { dateTime: '2026-06-01T09:00:00', userName: 'bob', actionType: 'Insert', actionDesc: '雲端課程' },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RowAuditService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RowAuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getHistory GETs /rowaudit with tableName and pkid as query params', () => {
    service.getHistory('Course', 123).subscribe((rows) => {
      expect(rows.length).toBe(2);
      expect(rows[0].userName).toBe('alice');
    });

    const req = httpMock.expectOne(
      (r) => r.url === base && r.params.get('tableName') === 'Course' && r.params.get('pkid') === '123',
    );
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('getHistory accepts a string pkid', () => {
    service.getHistory('AppRole', '7').subscribe();

    const req = httpMock.expectOne((r) => r.url === base && r.params.get('pkid') === '7');
    req.flush([]);
  });
});
