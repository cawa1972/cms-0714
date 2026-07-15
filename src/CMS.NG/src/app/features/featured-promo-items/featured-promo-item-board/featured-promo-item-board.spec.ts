import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { FeaturedPromoItemBoard } from './featured-promo-item-board';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { FeaturedPromoItem, PromoCodeLookup } from '@core/models/featured-promo-item.model';

const MONDAY_ISO = '2026-03-16'; // a real Monday; board state is seeded so tests are deterministic

function item(pkid: number, scheduleOn: string, slot: number, center = 1): FeaturedPromoItem {
  return {
    pkid,
    scheduleOn,
    trainingCenterPkid: center,
    slot,
    promotionPkid: 10 + slot,
    topic: `主題 ${pkid}`,
    description: `說明 ${pkid}`,
    promoCode: `CODE_${pkid}`,
    trainingCenterName: '台北',
  };
}

const WEEK_ITEMS: FeaturedPromoItem[] = [
  item(1, MONDAY_ISO, 1),
  item(2, MONDAY_ISO, 2),
  item(3, '2026-03-18', 1),
];

const PROMO: PromoCodeLookup = {
  promotionPkid: 42,
  promoCode: 'NEW_CODE',
  topic: '促銷主題',
  description: '促銷說明',
};

describe('FeaturedPromoItemBoard', () => {
  let fixture: ComponentFixture<FeaturedPromoItemBoard>;
  let component: FeaturedPromoItemBoard;
  let serviceSpy: jasmine.SpyObj<FeaturedPromoItemService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    sessionStorage.setItem(
      'featured-promo-item-board-state',
      JSON.stringify({ trainingCenterPkid: 1, weekStartIso: MONDAY_ISO }),
    );

    serviceSpy = jasmine.createSpyObj<FeaturedPromoItemService>('FeaturedPromoItemService', [
      'query',
      'create',
      'update',
      'delete',
      'move',
      'lookupPromoCode',
    ]);
    serviceSpy.query.and.returnValue(of(WEEK_ITEMS));
    serviceSpy.create.and.returnValue(of(item(7, '2026-03-19', 1)));
    serviceSpy.update.and.returnValue(of(item(1, MONDAY_ISO, 1)));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.move.and.returnValue(of(void 0));
    serviceSpy.lookupPromoCode.and.returnValue(of(PROMO));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getTrainingCenters']);
    lookupSpy.getTrainingCenters.and.returnValue(
      of([
        { value: '1', label: '台北' },
        { value: '2', label: '台中' },
      ]),
    );

    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [FeaturedPromoItemBoard],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: FeaturedPromoItemService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FeaturedPromoItemBoard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ---- List (board) ----------------------------------------------------

  it('creates, loads centers, and queries the restored week + training center on init', () => {
    expect(component).toBeTruthy();
    expect(component['centerOptions']()).toEqual([
      { value: 1, label: '台北' },
      { value: 2, label: '台中' },
    ]);
    expect(serviceSpy.query).toHaveBeenCalledWith({
      scheduleOnFrom: MONDAY_ISO,
      scheduleOnTo: '2026-03-22',
      trainingCenterPkid: 1,
    });
  });

  it('builds 7 Monday–Sunday days × 3 slots and maps items to their cells', () => {
    const days = component['days']();
    expect(days.length).toBe(7);
    expect(days[0].iso).toBe(MONDAY_ISO);
    expect(days[0].label).toBe('3/16 (一)');
    expect(days[6].iso).toBe('2026-03-22');
    expect(days[6].label).toBe('3/22 (日)');
    expect(days.every((d) => d.cells.length === 3)).toBeTrue();

    expect(days[0].cells[0].item?.pkid).toBe(1);
    expect(days[0].cells[1].item?.pkid).toBe(2);
    expect(days[0].cells[2].item).toBeNull();
    expect(days[2].cells[0].item?.pkid).toBe(3); // Wednesday slot 1
  });

  it('selectCenter reloads with the new training center and persists it', () => {
    component.selectCenter(2);

    expect(serviceSpy.query).toHaveBeenCalledWith(
      jasmine.objectContaining({ trainingCenterPkid: 2 }),
    );
    const saved = JSON.parse(sessionStorage.getItem('featured-promo-item-board-state')!);
    expect(saved.trainingCenterPkid).toBe(2);
  });

  it('nextWeek / prevWeek shift the Monday by ±7 days and reload', () => {
    component.nextWeek();
    expect(serviceSpy.query).toHaveBeenCalledWith(
      jasmine.objectContaining({ scheduleOnFrom: '2026-03-23', scheduleOnTo: '2026-03-29' }),
    );

    component.prevWeek();
    expect(serviceSpy.query).toHaveBeenCalledWith(
      jasmine.objectContaining({ scheduleOnFrom: MONDAY_ISO, scheduleOnTo: '2026-03-22' }),
    );
  });

  it('surfaces an error and stops loading when the query fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });

  // ---- Edit form ---------------------------------------------------------

  it('startEdit on an occupied slot opens the editor prefilled with the item', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[0]);

    expect(component.isEditing(MONDAY_ISO, 1)).toBeTrue();
    expect(component['form'].value).toEqual({
      promoCode: 'CODE_1',
      topic: '主題 1',
      description: '說明 1',
    });
    expect(component['resolvedPromo']()?.promotionPkid).toBe(11);
  });

  it('save on an existing item updates it, closes the editor, and reloads', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[0]);
    component['form'].patchValue({ topic: '改版主題' });
    const queryCalls = serviceSpy.query.calls.count();

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith({
      pkid: 1,
      scheduleOn: MONDAY_ISO,
      trainingCenterPkid: 1,
      slot: 1,
      promotionPkid: 11,
      topic: '改版主題',
      description: '說明 1',
    });
    expect(serviceSpy.lookupPromoCode).not.toHaveBeenCalled(); // code unchanged → reuse resolution
    expect(component['editing']()).toBeNull();
    expect(serviceSpy.query.calls.count()).toBe(queryCalls + 1);
  });

  // ---- New form ------------------------------------------------------------

  it('startEdit on an empty slot opens an empty editor', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[2]); // Monday slot 3 is empty

    expect(component.isEditing(MONDAY_ISO, 3)).toBeTrue();
    expect(component['form'].value).toEqual({ promoCode: '', topic: '', description: '' });
    expect(component['resolvedPromo']()).toBeNull();
  });

  it('lookupPromo resolves the code and defaults empty Topic/Description', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[2]);
    component['form'].controls.promoCode.setValue('NEW_CODE');

    component.lookupPromo();

    expect(serviceSpy.lookupPromoCode).toHaveBeenCalledWith('NEW_CODE');
    expect(component['resolvedPromo']()).toEqual(PROMO);
    expect(component['form'].value).toEqual({
      promoCode: 'NEW_CODE',
      topic: '促銷主題',
      description: '促銷說明',
    });
  });

  it('save on a new slot resolves the PromoCode then creates with Promotion_pkid set', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[2]);
    component['form'].setValue({ promoCode: 'NEW_CODE', topic: '新主題', description: '新說明' });

    component.save();

    expect(serviceSpy.lookupPromoCode).toHaveBeenCalledWith('NEW_CODE');
    expect(serviceSpy.create).toHaveBeenCalledWith({
      pkid: 0,
      scheduleOn: MONDAY_ISO,
      trainingCenterPkid: 1,
      slot: 3,
      promotionPkid: 42,
      topic: '新主題',
      description: '新說明',
    });
  });

  it('save with an unknown PromoCode does not create', () => {
    serviceSpy.lookupPromoCode.and.returnValue(throwError(() => new Error('404')));
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[2]);
    component['form'].setValue({ promoCode: 'NOPE', topic: 'x', description: 'y' });

    component.save();

    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component.isEditing(MONDAY_ISO, 3)).toBeTrue(); // editor stays open
  });

  it('save with an invalid form marks it touched and calls nothing', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[2]);

    component.save();

    expect(serviceSpy.lookupPromoCode).not.toHaveBeenCalled();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].touched).toBeTrue();
  });

  it('cancelEdit closes the editor without saving', () => {
    const days = component['days']();
    component.startEdit(days[0], days[0].cells[0]);
    component.cancelEdit();

    expect(component['editing']()).toBeNull();
    expect(serviceSpy.update).not.toHaveBeenCalled();
  });

  // ---- Copy / Paste ---------------------------------------------------------

  it('copy then paste prefills the editor on an empty slot with the copied values', () => {
    const days = component['days']();
    component.copy(days[0].cells[0].item!);

    expect(component['clipboard']()?.promoCode).toBe('CODE_1');

    component.paste(days[1], days[1].cells[0]); // Tuesday slot 1 is empty

    expect(component.isEditing('2026-03-17', 1)).toBeTrue();
    expect(component['form'].value).toEqual({
      promoCode: 'CODE_1',
      topic: '主題 1',
      description: '說明 1',
    });
    expect(component['resolvedPromo']()?.promotionPkid).toBe(11);
  });

  it('paste does nothing on an occupied slot', () => {
    const days = component['days']();
    component.copy(days[0].cells[0].item!);

    component.paste(days[0], days[0].cells[1]); // occupied by pkid 2

    expect(component['editing']()).toBeNull();
  });

  // ---- Move / Delete ---------------------------------------------------------

  it('move calls the service with the direction and reloads', () => {
    const queryCalls = serviceSpy.query.calls.count();
    component.move(WEEK_ITEMS[0], 'down');

    expect(serviceSpy.move).toHaveBeenCalledWith(1, 'down');
    expect(serviceSpy.query.calls.count()).toBe(queryCalls + 1);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });
    const queryCalls = serviceSpy.query.calls.count();

    component.remove(WEEK_ITEMS[1], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith(2);
    expect(serviceSpy.query.calls.count()).toBe(queryCalls + 1);
  });

  // ---- Week helpers -----------------------------------------------------------

  it('mondayOf returns the Monday of the containing week (Sunday belongs to the prior Monday)', () => {
    expect(FeaturedPromoItemBoard.mondayOf(new Date(2026, 2, 18))).toEqual(new Date(2026, 2, 16));
    expect(FeaturedPromoItemBoard.mondayOf(new Date(2026, 2, 22))).toEqual(new Date(2026, 2, 16));
    expect(FeaturedPromoItemBoard.mondayOf(new Date(2026, 2, 16))).toEqual(new Date(2026, 2, 16));
  });
});
