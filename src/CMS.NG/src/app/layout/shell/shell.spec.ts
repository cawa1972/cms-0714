import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { Shell } from './shell';
import { AuthService } from '@core/services/auth.service';

function mountShell(roles: string[]) {
  const mockAuth = {
    userName: signal<string | null>('Helen Chen'),
    hasRole: (role: string) => roles.includes(role),
    clearSession: () => {},
  };

  TestBed.configureTestingModule({
    imports: [Shell],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: AuthService, useValue: mockAuth },
    ],
  });

  const fixture = TestBed.createComponent(Shell);
  fixture.detectChanges();
  return fixture;
}

describe('Shell', () => {
  it('shows the 系統管理 Admin menu when roles include "Admin"', () => {
    const fixture = mountShell(['Admin']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('系統管理 Admin');
    expect(text).toContain('角色 AppRole');
  });

  it('hides the 系統管理 Admin menu for users without the "Admin" role', () => {
    const fixture = mountShell(['Editor']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).not.toContain('系統管理 Admin');
    expect(text).not.toContain('角色 AppRole');
    // The non-admin menu (e.g. Course) is still there.
    expect(text).toContain('課程管理 Course');
  });

  it('shows the signed-in user name in the shell', () => {
    const fixture = mountShell(['Admin']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Helen Chen');
  });
});
