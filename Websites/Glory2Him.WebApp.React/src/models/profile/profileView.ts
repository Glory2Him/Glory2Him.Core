export interface ProfileView {
    id: string;
    userName: string;
    email: string;
    phoneNumber: string | null;
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
    hasProfileImage: boolean;
    imageVersion: string | null;
    imageUrl: string | null;
}

export interface UpdateProfileRequest {
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
    phoneNumber: string | null;
}
