// Response shapes of the /api/manage endpoints, mirroring the state the Blazor
// Account/Manage pages read through UserManager/SignInManager.

export interface EmailInfo {
    email: string | null;
    isEmailConfirmed: boolean;
}

export interface TwoFactorInfo {
    canTrack: boolean;
    hasAuthenticator: boolean;
    is2faEnabled: boolean;
    isMachineRemembered: boolean;
    recoveryCodesLeft: number;
}

export interface AuthenticatorSetup {
    sharedKey: string;
    authenticatorUri: string;
}

export interface VerifyAuthenticatorResult {
    message: string;
    recoveryCodes: string[] | null;
}

export interface GenerateRecoveryCodesResult {
    message: string;
    recoveryCodes: string[];
}

export interface PersonalDataInfo {
    requirePassword: boolean;
}
