// Mirrors the shape returned by GET /api/passkeys — the data the Blazor
// Manage/Passkeys page read from UserManager.GetPasskeysAsync.
export interface PasskeyInfo {
    credentialId: string;
    name: string | null;
    createdAt: string;
}
