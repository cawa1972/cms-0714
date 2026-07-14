import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { AppRoleDetail } from './app-role-detail';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole, LookupItem } from '@core/models/app-role.model';

const ROLE: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 2,
  userIds: ['helen', 'miles'],
};

const USERS: LookupItem[] = [
  { value: 'helen', label: 'helen (helen)' },
  { value: 'miles', label: 'Miles Sun (miles)' },
  { value: 'jenny', label: 'Jenny_Tsao (Jenny_Tsao)' },
];

describe('AppRoleDetail', () => {
  let fixture: ComponentFixture<AppRoleDetail>;
  let component: AppRoleDetail;
  let serviceSpy: jasmine.SpyObj<AppRoleService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['getById']);
    serviceSpy.getById.and.returnValue(of(ROLE));
    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppUsers']);
    lookupSpy.getAppUsers.and.returnValue(of(USERS));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [AppRoleDetail],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: AppRoleService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'Admin' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppRoleDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the role by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
    expect(component['role']()?.roleName).toBe('Administrator');
  });

  it('maps user ids to their lookup labels', () => {
    expect(component['userLabels']()).toEqual(['helen (helen)', 'Miles Sun (miles)']);
  });

  it('edit navigates to the edit form', () => {
    component.edit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles', 'Admin', 'edit']);
  });

  it('back navigates to the list', () => {
    component.back();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles']);
  });
});
