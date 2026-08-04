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
