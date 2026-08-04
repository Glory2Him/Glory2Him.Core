export class CurrentUser {
    public isAuthenticated: boolean;
    public userId: string;
    public userName: string;
    public email: string;
    public displayName: string;
    public roles: Array<string>;

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    constructor(currentUser: any) {
        this.isAuthenticated = currentUser?.isAuthenticated ?? false;
        this.userId = currentUser?.userId ?? '';
        this.userName = currentUser?.userName ?? '';
        this.email = currentUser?.email ?? '';
        this.displayName = currentUser?.displayName ?? '';
        this.roles = currentUser?.roles ?? [];
    }
}
