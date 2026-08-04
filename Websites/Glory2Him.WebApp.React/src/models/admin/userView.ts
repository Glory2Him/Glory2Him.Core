export interface UserView {
    id: string;
    userName: string;
    email: string;
    phoneNumber: string | null;
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
    emailConfirmed: boolean;
    isLockedOut: boolean;
    accessFailedCount: number;
    twoFactorEnabled: boolean;
    isDisabled: boolean;
    roles: string[];
    hasProfileImage: boolean;
    imageVersion: string | null;
    imageUrl: string | null;
    displayName: string;
}

export interface UpdateUserRequest {
    userName: string;
    email: string;
    phoneNumber: string | null;
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
}
