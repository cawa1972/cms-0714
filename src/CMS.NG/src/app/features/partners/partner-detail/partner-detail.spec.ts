import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PartnerDetail } from './partner-detail';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const PARTNER: Partner = {
  pkid: 2,
  name: '甲骨文',
  appKey: 'ORACLE',
  nameOnPartnerMenu: '甲骨文原廠課程',
  nameOnCourseDetailPage: '甲骨文',
  displayOrder: 2,
  imageFilename: null,
};

function setup(getByIdReturn = of(PARTNER)) {
  const serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['getById']);
  serviceSpy.getById.and.returnValue(getByIdReturn);
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [PartnerDetail],
    providers: [
      provideNoopAnimations(),
      MessageService,
      { provide: PartnerService, useValue: serviceSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } },
      },
    ],
  });

  const fixture: ComponentFixture<PartnerDetail> = TestBed.createComponent(PartnerDetail);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

describe('PartnerDetail', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the partner by the route pkid', () => {
    const { component, serviceSpy } = setup();
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['partner']()?.name).toBe('甲骨文');
  });

  it('edit navigates to the edit route', () => {
    const { component, routerSpy } = setup();
    component.edit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners', 2, 'edit']);
  });

  it('back navigates to the list', () => {
    const { component, routerSpy } = setup();
    component.back();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners']);
  });

  it('redirects to the list when loading fails', () => {
    const { routerSpy } = setup(throwError(() => new Error('missing')));
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners']);
  });
});
