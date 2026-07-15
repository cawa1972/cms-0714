import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { RowAuditBadge } from './row-audit-badge';
import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

const ENTRIES: RowAuditEntry[] = [
  { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title, ListPrice' },
  { dateTime: '2026-06-01T09:00:00', userName: 'bob', actionType: 'Insert', actionDesc: '雲端課程' },
];

describe('RowAuditBadge', () => {
  let fixture: ComponentFixture<RowAuditBadge>;
  let serviceSpy: jasmine.SpyObj<RowAuditService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<RowAuditService>('RowAuditService', ['getHistory']);

    await TestBed.configureTestingModule({
      imports: [RowAuditBadge],
      providers: [provideNoopAnimations(), { provide: RowAuditService, useValue: serviceSpy }],
    }).compileComponents();
  });

  afterEach(() => {
    fixture?.destroy();
    // The dialog is appended to <body>; PrimeNG does not always detach it on fixture destroy,
    // so remove leftovers to keep the global document queries of later tests isolated.
    document.querySelectorAll('.p-dialog-mask, .p-dialog, .p-overlay-mask').forEach((el) => el.remove());
  });

  function create(pkid: number | string | null, tableName = 'Course'): void {
    fixture = TestBed.createComponent(RowAuditBadge);
    fixture.componentRef.setInput('tableName', tableName);
    fixture.componentRef.setInput('pkid', pkid);
    fixture.detectChanges();
  }

  it('fetches the history on load and shows the latest record inline on the badge', () => {
    serviceSpy.getHistory.and.returnValue(of(ENTRIES));

    create(123);

    expect(serviceSpy.getHistory).toHaveBeenCalledWith('Course', 123);
    const badge: HTMLElement = fixture.nativeElement.querySelector('.row-audit-badge');
    expect(badge.textContent).toContain('異動紀錄 History');
    expect(badge.textContent).toContain('Update by alice');
    expect(badge.textContent).toMatch(/\d{4}-\d{2}-\d{2} \d{2}:\d{2}/); // latest DateTime rendered
  });

  it('opens the dialog on click and lists the full audit trail, newest first', () => {
    serviceSpy.getHistory.and.returnValue(of(ENTRIES));
    create(123);

    fixture.nativeElement.querySelector('.row-audit-badge').click();
    fixture.detectChanges();

    expect(fixture.componentInstance['dialogVisible']()).toBeTrue();

    // The dialog is appended to <body> (appendTo="body"), so query the document.
    const rows = Array.from(document.querySelectorAll('.row-audit-table tbody tr'));
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('alice');   // newest (Update) first…
    expect(rows[0].textContent).toContain('Title, ListPrice');
    expect(rows[1].textContent).toContain('bob');     // …oldest (Insert) last
    expect(rows[1].textContent).toContain('雲端課程');
  });

  it('refreshes the history when the dialog opens', () => {
    serviceSpy.getHistory.and.returnValue(of(ENTRIES));
    create(123);
    expect(serviceSpy.getHistory).toHaveBeenCalledTimes(1);

    fixture.nativeElement.querySelector('.row-audit-badge').click();

    expect(serviceSpy.getHistory).toHaveBeenCalledTimes(2);
  });

  it('shows the neutral no-history state on the badge and in the dialog', () => {
    serviceSpy.getHistory.and.returnValue(of([]));

    create(123);

    const badge: HTMLElement = fixture.nativeElement.querySelector('.row-audit-badge');
    expect(badge.textContent).toContain('尚無紀錄 No history');

    badge.click();
    fixture.detectChanges();

    expect(document.querySelector('.row-audit-table')).toBeNull();
    expect(document.querySelector('.row-audit-empty')?.textContent).toContain('尚無異動紀錄 No history yet');
  });

  it('renders nothing and does not fetch while pkid is null (create mode)', () => {
    create(null);

    expect(serviceSpy.getHistory).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.row-audit-badge')).toBeNull();
  });
});
