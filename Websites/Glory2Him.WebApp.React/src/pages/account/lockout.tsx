// Ported from Blazor's Account/Pages/Lockout.razor.
export function Lockout() {
    return (
        <header>
            <h1 className="text-danger">Locked out</h1>
            <p className="text-danger" role="alert">
                This account has been locked out, please try again later.
            </p>
        </header>
    );
}
