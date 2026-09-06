import { ReactElement, ReactNode, createContext, useContext } from 'react';
import { accountService } from '../../services/foundations/accountService';
import { CurrentUser } from '../../models/accounts/currentUser';

type AuthContextValue = {
    isAuthenticated: boolean,
    isLoading: boolean,
    user: CurrentUser | undefined,
    userRoles: Array<string>
}

const AuthContext = createContext<AuthContextValue>({
    isAuthenticated: false,
    isLoading: true,
    user: undefined,
    userRoles: []
});

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => useContext(AuthContext);

type AuthProviderParameters = {
    children: ReactNode
}

export const AuthProvider = ({ children }: AuthProviderParameters): ReactElement => {
    const { data: user, isLoading } = accountService.useGetCurrentUser();

    const value: AuthContextValue = {
        isAuthenticated: user?.isAuthenticated ?? false,
        isLoading,
        user,
        userRoles: user?.roles ?? []
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

// A SUBTREE in a CHOSEN security context — for the component reference pages' playgrounds
// and for tests. Every gate in the component family decides RENDERING only; the server
// re-decides each write against the stored row (§14.6, §14.7 posture A) — so letting a doc
// reader step into "a reviewer who is not the owner" shows the controls honestly without
// touching the session, and grants nothing.
type AuthContextOverrideParameters = {
    userId: string;
    displayName: string;

    // Defaults to the display name, which is all most callers need. Pass it where the surface
    // under test renders the two SEPARATELY — the review panel puts the username under the name
    // — because there the default prints one value twice and reads as a rendering fault rather
    // than as the harness having nothing better to say.
    userName?: string;

    roles: ReadonlyArray<string>;
    children: ReactNode;
};

export const AuthContextOverride = ({
    userId,
    displayName,
    userName,
    roles,
    children
}: AuthContextOverrideParameters): ReactElement => {
    const value: AuthContextValue = {
        isAuthenticated: true,
        isLoading: false,

        user: new CurrentUser({
            isAuthenticated: true,
            userId,
            userName: userName ?? displayName,
            displayName,
            roles: [...roles]
        }),

        userRoles: [...roles]
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};
