export interface RegisterRequest {
    userName: string;
    email: string;
    name: string;
    surname: string;
    preferredName: string | null;
    dateOfBirth: string | null;
    password: string;
}

// isProhibited is reported separately from isAvailable because they are different problems:
// a username containing "@" is not free-but-taken, it is a name nobody may hold (design §18.3.1),
// and telling someone it is "already taken" would be a lie about an address they own.
// prohibitedReason carries the server's wording so the two do not drift apart.
export interface UsernameAvailability {
    isAvailable: boolean;
    isTooShort: boolean;
    isProhibited: boolean;
    prohibitedReason: string | null;
    minimumLength: number;
}
