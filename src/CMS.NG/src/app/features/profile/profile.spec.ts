import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { Profile } from './profile';
import { AuthService } from '@core/services/auth.service';
import { UpdateProfileResponse } from '@core/models/auth.model';

describe('Profile', () => {
  let fixture: ComponentFixture<Profile>;
  let component: Profile;

  // A shared userName signal stands in for what the app shell binds to: updating it here is exactly
  // how the real AuthService refreshes the shell after a successful save.
  let shellUserName: ReturnType<typeof signal<string | null>>;
  let updateSpy: jasmine.Spy;

  beforeEach(async () => {
    shellUserName = signal<string | null>('Helen Chen');

    updateSpy = jasmine.createSpy('updateUserName').and.callFake((userName: string) => {
      const res: UpdateProfileResponse = { userId: 'helen', userName };
      shellUserName.set(userName); // mirrors AuthService updating session storage + signal
      return of(res);
    });

    const mockAuth = {
      userId: signal<string | null>('helen'),
      userName: shellUserName,
      roles: signal<string[]>(['Admin', 'Editor']),
      updateUserName: updateSpy,
    };

    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: AuthService, useValue: mockAuth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('shows the UserId as read-only text (not an input)', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('helen');

    // The only editable input on the page is UserName; UserId and roles are display-only.
    const inputs = el.querySelectorAll('input');
    expect(inputs.length).toBe(1);
    expect(inputs[0].getAttribute('id')).toBe('userName');
  });

  it('shows the roles as read-only chips', () => {
    const el = fixture.nativeElement as HTMLElement;
    const chips = Array.from(el.querySelectorAll('.chip')).map((c) => c.textContent?.trim());
    expect(chips).toEqual(['Admin', 'Editor']);
  });

  it('seeds the UserName field from the current profile', () => {
    expect(component['form'].getRawValue().userName).toBe('Helen Chen');
  });

  it('saving updates the UserName in the shell', () => {
    component['form'].get('userName')!.setValue('Helen Wu');

    component.save();

    expect(updateSpy).toHaveBeenCalledWith('Helen Wu');
    // The shell-bound signal now reflects the new name.
    expect(shellUserName()).toBe('Helen Wu');
  });

  it('trims the UserName before saving', () => {
    component['form'].get('userName')!.setValue('   Helen Wu   ');

    component.save();

    expect(updateSpy).toHaveBeenCalledWith('Helen Wu');
  });

  it('does not call the service when UserName is blank', () => {
    component['form'].get('userName')!.setValue('   ');

    component.save();

    expect(updateSpy).not.toHaveBeenCalled();
  });
});
