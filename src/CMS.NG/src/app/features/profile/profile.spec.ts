import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { Profile } from './profile';
import { AuthService } from '@core/services/auth.service';
import { ChangePasswordRequest, UpdateProfileResponse } from '@core/models/auth.model';

describe('Profile', () => {
  let fixture: ComponentFixture<Profile>;
  let component: Profile;

  // A shared userName signal stands in for what the app shell binds to: updating it here is exactly
  // how the real AuthService refreshes the shell after a successful save.
  let shellUserName: ReturnType<typeof signal<string | null>>;
  let updateSpy: jasmine.Spy;
  let changePasswordSpy: jasmine.Spy;

  beforeEach(async () => {
    shellUserName = signal<string | null>('Helen Chen');

    updateSpy = jasmine.createSpy('updateUserName').and.callFake((userName: string) => {
      const res: UpdateProfileResponse = { userId: 'helen', userName };
      shellUserName.set(userName); // mirrors AuthService updating session storage + signal
      return of(res);
    });

    changePasswordSpy = jasmine
      .createSpy('changePassword')
      .and.callFake((_: ChangePasswordRequest) => of(void 0));

    const mockAuth = {
      userId: signal<string | null>('helen'),
      userName: shellUserName,
      roles: signal<string[]>(['Admin', 'Editor']),
      updateUserName: updateSpy,
      changePassword: changePasswordSpy,
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

    // The only editable non-password input is UserName; UserId and roles are display-only.
    const inputs = Array.from(el.querySelectorAll('input')).filter(
      (i) => i.getAttribute('type') !== 'password',
    );
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

  // ---- Change Password: client-side validation --------------------------------

  function fillPw(current: string, next: string, confirm: string): void {
    component['pwForm'].setValue({
      currentPassword: current,
      newPassword: next,
      confirmPassword: confirm,
    });
  }

  it('requires all three password fields', () => {
    fillPw('', '', '');
    expect(component['pwForm'].invalid).toBeTrue();

    component.changePassword();
    expect(changePasswordSpy).not.toHaveBeenCalled();
  });

  it('rejects a new password shorter than 8 characters', () => {
    fillPw('old-pw', 'Ab1!', 'Ab1!');

    expect(component['pwForm'].get('newPassword')!.hasError('complexity')).toBeTrue();
    component.changePassword();
    expect(changePasswordSpy).not.toHaveBeenCalled();
  });

  it('rejects a new password with fewer than 3 of the 4 character classes', () => {
    // 8+ chars but only lowercase + digits = 2 classes.
    fillPw('old-pw', 'abcd1234', 'abcd1234');

    expect(component['pwForm'].get('newPassword')!.hasError('complexity')).toBeTrue();
    component.changePassword();
    expect(changePasswordSpy).not.toHaveBeenCalled();
  });

  it('accepts an 8-char password with 3 of the 4 classes', () => {
    fillPw('old-pw', 'Abcdefg1', 'Abcdefg1');

    expect(component['pwForm'].get('newPassword')!.hasError('complexity')).toBeFalse();
    expect(component['pwForm'].valid).toBeTrue();
  });

  it('flags a new/confirm mismatch as a group error', () => {
    fillPw('old-pw', 'Abcdefg1', 'Different1!');

    expect(component['pwForm'].hasError('mismatch')).toBeTrue();
    component.changePassword();
    expect(changePasswordSpy).not.toHaveBeenCalled();
  });

  it('shows the bilingual complexity message for a weak new password', () => {
    fillPw('old-pw', 'abcd1234', 'abcd1234');
    component['pwForm'].markAllAsTouched();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('密碼長度至少需 8 碼');
    expect(text).toContain('at least 3 of the 4 classes');
  });

  it('submits a valid change and resets the form', () => {
    fillPw('old-pw', 'Abcdefg1', 'Abcdefg1');

    component.changePassword();

    expect(changePasswordSpy).toHaveBeenCalledWith({
      currentPassword: 'old-pw',
      newPassword: 'Abcdefg1',
      confirmNewPassword: 'Abcdefg1',
    });
    // Cleared after success so passwords never linger in the form.
    expect(component['pwForm'].getRawValue()).toEqual({
      currentPassword: null,
      newPassword: null,
      confirmPassword: null,
    });
  });
});
