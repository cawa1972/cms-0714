import { AppRoles, homeRouteFor } from './app-roles';

describe('homeRouteFor', () => {
  it('sends an admin to the Admin landing page', () => {
    expect(homeRouteFor([AppRoles.Admin])).toBe('/app-roles');
  });

  it('sends a non-admin to the default landing page', () => {
    expect(homeRouteFor(['Editor'])).toBe('/featured-promo-items');
  });

  it('sends a user with no roles to the default landing page', () => {
    expect(homeRouteFor([])).toBe('/featured-promo-items');
  });

  it('honours Admin among several roles', () => {
    expect(homeRouteFor(['Editor', 'Admin'])).toBe('/app-roles');
  });

  it('never lands a non-admin on a route the adminGuard would reject', () => {
    // Guards the invariant the '' redirect depends on: the non-admin home must not be an Admin page.
    expect(homeRouteFor(['Editor']).startsWith('/app-')).toBeFalse();
  });
});
