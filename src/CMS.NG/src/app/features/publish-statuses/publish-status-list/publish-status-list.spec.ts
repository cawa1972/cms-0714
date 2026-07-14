import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PublishStatusList } from './publish-status-list';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUSES: PublishStatus[] = [
  { pkid: 1, description: '草稿', isDraft: true, isPublished: false, isDiscontinued: false },
  { pkid: 2, description: '已發布', isDraft: false, isPublished: true, isDiscontinued: false },
];

describe('PublishStatusList', () => {
  let fixture: ComponentFixture<PublishStatusList>;
  let component: PublishStatusList;
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(STATUSES));
    serviceSpy.delete.and.returnValue(of(void 0));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [PublishStatusList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: PublishStatusService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublishStatusList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates and loads statuses on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['statuses']().length).toBe(2);
  });

  it('applyFilters persists filters and reloads', () => {
    component['filters'] = { keyword: '發布', isDraft: null, isPublished: true, isDiscontinued: null };
    component.applyFilters();

    expect(JSON.parse(sessionStorage.getItem('publish-status-list-filters')!)).toEqual({
      keyword: '發布',
      isDraft: null,
      isPublished: true,
      isDiscontinued: null,
    });
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets and removes saved filters', () => {
    sessionStorage.setItem('publish-status-list-filters', JSON.stringify({ keyword: 'x' }));
    component.clearFilters();

    expect(sessionStorage.getItem('publish-status-list-filters')).toBeNull();
    expect(component['filters']).toEqual({
      keyword: null,
      isDraft: null,
      isPublished: null,
      isDiscontinued: null,
    });
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses/new']);
  });

  it('view and edit navigate with the pkid', () => {
    component.view(STATUSES[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses', 1]);
    component.edit(STATUSES[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses', 2, 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(STATUSES[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
