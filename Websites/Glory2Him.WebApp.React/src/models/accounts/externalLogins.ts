// Mirrors the external-login data the Blazor ExternalLoginPicker and
// Manage/ExternalLogins pages surfaced.
export interface ExternalProvider {
    name: string;
    displayName: string;
}

export interface ExternalLoginInfo {
    loginProvider: string;
    providerDisplayName: string;
    providerKey: string;
}

export interface ExternalLoginsView {
    currentLogins: ExternalLoginInfo[];
    otherLogins: ExternalProvider[];
    showRemoveButton: boolean;
}
