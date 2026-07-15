import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import {
  AuthProfile,
  ChangePasswordRequest,
  LoginRequest,
  UpdateProfileResponse,
} from '@core/models/auth.model';

/** SESSION storage key holding the serialized {@link AuthProfile}. */
const STORAGE_KEY = 'cms.auth';

/**
 * Owns the signed-in session: authenticates against the API, persists the profile in
 * <em>session</em> storage (cleared when the tab closes — never local storage), and exposes the
 * token, user name, and roles decoded from the JWT.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly loginUrl = `${environment.apiBaseUrl}/Auth/login`;
  private readonly profileUrl = `${environment.apiBaseUrl}/Auth/profile`;

  /** Reactive view of the stored profile; seeded from session storage on construction. */
  private readonly profileSignal = signal<AuthProfile | null>(readStoredProfile());

  readonly profile = this.profileSignal.asReadonly();
  readonly userId = computed(() => this.profileSignal()?.userId ?? null);
  readonly userName = computed(() => this.profileSignal()?.userName ?? null);
  readonly isAuthenticated = computed(() => !!this.profileSignal()?.accessToken);
  /** Roles decoded from the JWT `role` claim(s) — read from the token, not a separate API call. */
  readonly roles = computed(() => decodeRoles(this.profileSignal()?.accessToken));

  /** POST the credentials; on success store the returned profile in session storage. */
  login(request: LoginRequest): Observable<AuthProfile> {
    return this.http
      .post<AuthProfile>(this.loginUrl, request)
      .pipe(tap((profile) => this.setProfile(profile)));
  }

  /**
   * Update the signed-in user's own display name. The backend takes the UserId from the JWT, so only
   * `userName` is sent. On success the stored profile (session storage + signal) is refreshed with
   * the server-canonical name, so the app shell reflects it immediately.
   */
  updateUserName(userName: string): Observable<UpdateProfileResponse> {
    return this.http
      .put<UpdateProfileResponse>(this.profileUrl, { userName })
      .pipe(tap((res) => this.applyUserName(res.userName)));
  }

  /**
   * Change the signed-in user's own password. The backend takes the UserId from the JWT and verifies
   * the current password server-side; nothing about the session (token, profile) changes on success.
   */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/Auth/change-password`, request);
  }

  /** Current access token, or null when signed out. */
  get token(): string | null {
    return this.profileSignal()?.accessToken ?? null;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  /** Clear the session (used on logout and on any 401). */
  clearSession(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.profileSignal.set(null);
  }

  private setProfile(profile: AuthProfile): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
    this.profileSignal.set(profile);
  }

  /** Replace just the userName on the stored profile (keeping the same token), or no-op if signed out. */
  private applyUserName(userName: string): void {
    const current = this.profileSignal();
    if (!current) {
      return;
    }
    this.setProfile({ ...current, userName });
  }
}

function readStoredProfile(): AuthProfile | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as AuthProfile;
  } catch {
    return null;
  }
}

/** Decode the roles carried in a JWT's `role` claim (which may be a single value or an array). */
function decodeRoles(token: string | null | undefined): string[] {
  const payload = decodeJwtPayload(token);
  if (!payload) {
    return [];
  }
  const raw = payload['role'] ?? payload['roles'];
  if (Array.isArray(raw)) {
    return raw.filter((r): r is string => typeof r === 'string');
  }
  return typeof raw === 'string' ? [raw] : [];
}

function decodeJwtPayload(token: string | null | undefined): Record<string, unknown> | null {
  if (!token) {
    return null;
  }
  const segments = token.split('.');
  if (segments.length < 2) {
    return null;
  }
  try {
    let base64 = segments[1].replace(/-/g, '+').replace(/_/g, '/');
    base64 += '='.repeat((4 - (base64.length % 4)) % 4);
    return JSON.parse(atob(base64)) as Record<string, unknown>;
  } catch {
    return null;
  }
}
