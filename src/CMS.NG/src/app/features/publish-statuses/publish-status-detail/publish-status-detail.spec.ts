import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { PublishStatusDetail } from './publish-status-detail';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUS: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

describe('PublishStatusDetail', () => {
  let fixture: ComponentFixture<PublishStatusDetail>;
  let component: PublishStatusDetail;
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['getById']);
    serviceSpy.getById.and.returnValue(of(STATUS));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [PublishStatusDetail],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: PublishStatusService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublishStatusDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the status by numeric id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['status']()?.description).toBe('已發布');
  });

  it('edit navigates to the edit form', () => {
    component.edit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses', 2, 'edit']);
  });

  it('back navigates to the list', () => {
    component.back();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses']);
  });
});
