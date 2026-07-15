/** Credentials posted to POST /api/Auth/login. */
export interface LoginRequest {
  userId: string;
  password: string;
}

/**
 * Profile returned on a successful login and persisted (in SESSION storage) for the browser session.
 * `accessToken` is the signed JWT; the user's roles live inside it as claims, not as a separate field.
 */
export interface AuthProfile {
  userId: string;
  userName: string;
  accessToken: string;
}

/** Body of PUT /api/Auth/profile — the signed-in user editing their own display name. */
export interface UpdateProfileRequest {
  userName: string;
}

/** Returned by PUT /api/Auth/profile: the authenticated identity and the newly stored UserName. */
export interface UpdateProfileResponse {
  userId: string;
  userName: string;
}
