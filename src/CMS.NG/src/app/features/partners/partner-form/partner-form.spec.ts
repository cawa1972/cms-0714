import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { PartnerForm } from './partner-form';
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

function setup(routeId: string | null) {
  const serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(PARTNER));
  serviceSpy.create.and.returnValue(of({ ...PARTNER, pkid: 5 }));
  serviceSpy.update.and.returnValue(of(PARTNER));
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [PartnerForm],
    providers: [
      provideNoopAnimations(),
      MessageService,
      { provide: PartnerService, useValue: serviceSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) },
        },
      },
    ],
  });

  const fixture: ComponentFixture<PartnerForm> = TestBed.createComponent(PartnerForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

describe('PartnerForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with no pkid', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['pkid']()).toBeNull();
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the partner and navigates to the DB-assigned pkid', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].setValue({
      name: '紅帽',
      appKey: 'REDHAT',
      nameOnPartnerMenu: '紅帽 Linux 課程',
      nameOnCourseDetailPage: '紅帽',
      displayOrder: 4,
      imageFilename: 'redhat.png',
    });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith({
      pkid: 0,
      name: '紅帽',
      appKey: 'REDHAT',
      nameOnPartnerMenu: '紅帽 Linux 課程',
      nameOnCourseDetailPage: '紅帽',
      displayOrder: 4,
      imageFilename: 'redhat.png',
    });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners', 5]);
  });
});

describe('PartnerForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the partner and patches the form', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBeTrue();
    expect(component['pkid']()).toBe(2);
    expect(component['form'].controls.name.value).toBe('甲骨文');
  });

  it('updates the partner on save (pkid carried from the route)', () => {
    const { component, serviceSpy, routerSpy } = setup('2');
    component['form'].controls.name.setValue('甲骨文台灣');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ pkid: 2, name: '甲骨文台灣' }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/partners', 2]);
  });
});
