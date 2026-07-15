import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppUserList } from './app-user-list';
import { AppUserService } from '@core/services/app-user.service';
import { AppUser } from '@core/models/app-user.model';

const USERS: AppUser[] = [
  { pkid: 1, userId: 'helen', userName: 'Helen Chen', isActive: true, passwordUpdatedTime: null, roleCount: 2, roleIds: [] },
  { pkid: 2, userId: 'miles', userName: 'Miles Sun', isActive: false, passwordUpdatedTime: null, roleCount: 0, roleIds: [] },
];

describe('AppUserList', () => {
  let fixture: ComponentFixture<AppUserList>;
  let component: AppUserList;
  let serviceSpy: jasmine.SpyObj<AppUserService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(USERS));
    serviceSpy.delete.and.returnValue(of(void 0));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [AppUserList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: AppUserService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppUserList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates and loads users on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['users']().length).toBe(2);
  });

  it('applyFilters persists filters and reloads', () => {
    component['filters'] = { keyword: 'helen', isActive: true };
    component.applyFilters();

    expect(JSON.parse(sessionStorage.getItem('app-user-list-filters')!)).toEqual({
      keyword: 'helen',
      isActive: true,
    });
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets and removes saved filters', () => {
    sessionStorage.setItem('app-user-list-filters', JSON.stringify({ keyword: 'x' }));
    component.clearFilters();

    expect(sessionStorage.getItem('app-user-list-filters')).toBeNull();
    expect(component['filters']).toEqual({ keyword: null, isActive: null });
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users/new']);
  });

  it('view and edit navigate with the user id', () => {
    component.view(USERS[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users', 'helen']);
    component.edit(USERS[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users', 'miles', 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(USERS[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith('helen');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
