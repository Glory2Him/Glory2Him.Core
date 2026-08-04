export interface RegisterRequest {
    userName: string;
    email: string;
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
    password: string;
}

export interface UsernameAvailability {
    isAvailable: boolean;
    isTooShort: boolean;
    minimumLength: number;
}
