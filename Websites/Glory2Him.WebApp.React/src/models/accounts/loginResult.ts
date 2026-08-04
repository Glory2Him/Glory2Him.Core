import { CurrentUser } from './currentUser';

// POST /api/accounts/login answers either with the signed-in user or with
// { requiresTwoFactor: true } when the account has 2FA enabled — mirroring the
// Blazor Login page redirecting to LoginWith2fa.
export class LoginResult {
    public requiresTwoFactor: boolean;
    public currentUser: CurrentUser | null;

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    constructor(data: any) {
        this.requiresTwoFactor = data?.requiresTwoFactor ?? false;
        this.currentUser = this.requiresTwoFactor ? null : new CurrentUser(data);
    }
}

// POST /api/accounts/login-2fa and /login-recovery-code answer with the
// signed-in user or { isLockedOut: true } — the SPA then navigates to
// /Account/Lockout exactly like the Blazor pages did.
export class TwoFactorLoginResult {
    public isLockedOut: boolean;
    public currentUser: CurrentUser | null;

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    constructor(data: any) {
        this.isLockedOut = data?.isLockedOut ?? false;
        this.currentUser = this.isLockedOut ? null : new CurrentUser(data);
    }
}
