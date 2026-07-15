import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { LookupService } from './lookup.service';
import { environment } from '@env/environment';

describe('LookupService', () => {
  let service: LookupService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LookupService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LookupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAppUsers issues GET /lookups/app-users', () => {
    service.getAppUsers().subscribe((items) => {
      expect(items.length).toBe(1);
      expect(items[0].label).toBe('helen (helen)');
    });
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/lookups/app-users`);
    expect(req.request.method).toBe('GET');
    req.flush([{ value: 'helen', label: 'helen (helen)' }]);
  });

  it('getTrainingCenters issues GET /lookups/training-centers', () => {
    service.getTrainingCenters().subscribe((items) => {
      expect(items.length).toBe(2);
      expect(items[0].label).toBe('台北');
    });
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/lookups/training-centers`);
    expect(req.request.method).toBe('GET');
    req.flush([
      { value: '1', label: '台北' },
      { value: '2', label: '台中' },
    ]);
  });
});
