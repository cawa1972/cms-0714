import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { AuthProfile, LoginRequest } from '@core/models/auth.model';

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

  /** Reactive view of the stored profile; seeded from session storage on construction. */
  private readonly profileSignal = signal<AuthProfile | null>(readStoredProfile());

  readonly profile = this.profileSignal.asReadonly();
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
