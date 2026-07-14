import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PartnerList } from './partner-list';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const PARTNERS: Partner[] = [
  { pkid: 1, name: '台灣微軟', appKey: 'MS', nameOnPartnerMenu: '台灣微軟合作課程', nameOnCourseDetailPage: '台灣微軟', displayOrder: 1, imageFilename: 'ms.png' },
  { pkid: 2, name: '甲骨文', appKey: 'ORACLE', nameOnPartnerMenu: '甲骨文原廠課程', nameOnCourseDetailPage: '甲骨文', displayOrder: 2, imageFilename: null },
];

describe('PartnerList', () => {
  let fixture: ComponentFixture<PartnerList>;
  let component: PartnerList;
  let serviceSpy: jasmine.SpyObj<PartnerService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(PARTNERS));
    serviceSpy.delete.and.returnValue(of(void 0));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [PartnerList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: PartnerService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates and loads partners on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['partners']().length).toBe(2);
  });

  it('applyFilters persists filters and reloads', () => {
    component['filters'] = { keyword: '甲骨文' };
    component.applyFilters();

    expect(JSON.parse(sessionStorage.getItem('partner-list-filters')!)).toEqual({ keyword: '甲骨文' });
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets and removes saved filters', () => {
    sessionStorage.setItem('partner-list-filters', JSON.stringify({ keyword: 'x' }));
    component.clearFilters();

    expect(sessionStorage.getItem('partner-list-filters')).toBeNull();
    expect(component['filters']).toEqual({ keyword: null });
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners/new']);
  });

  it('view and edit navigate with the pkid', () => {
    component.view(PARTNERS[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners', 1]);
    component.edit(PARTNERS[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners', 2, 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(PARTNERS[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
