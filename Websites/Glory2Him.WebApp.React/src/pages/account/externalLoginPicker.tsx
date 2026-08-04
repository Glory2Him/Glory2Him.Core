import { passkeyService } from '../../services/foundations/passkeyService';

// Ported from Blazor's Account/Shared/ExternalLoginPicker.razor. With no
// external providers configured it renders the same "no external
// authentication services configured" copy Blazor showed.
export interface ExternalLoginPickerProps {
    returnUrl?: string | null;
}

export function ExternalLoginPicker({ returnUrl }: ExternalLoginPickerProps) {
    const getExternalProviders = passkeyService.useGetExternalProviders();
    const externalLogins = getExternalProviders.data ?? [];

    if (getExternalProviders.isLoading) {
        return null;
    }

    if (externalLogins.length === 0) {
        return (
            <div>
                <p>
                    There are no external authentication services configured. See this{' '}
                    <a href="https://go.microsoft.com/fwlink/?LinkID=532715">article
                    about setting up this ASP.NET application to support logging in via external services</a>.
                </p>
            </div>
        );
    }

    // Constraint: an external login is an OAuth challenge (a redirect), so this
    // posts the Blazor form endpoint /Account/PerformExternalLogin. That
    // endpoint validates an antiforgery token the SPA does not have, so this
    // path only becomes actionable once a real provider is configured and the
    // challenge endpoint is exposed without the Razor-form antiforgery coupling.
    return (
        <form className="form-horizontal" action="/Account/PerformExternalLogin" method="post">
            <div>
                <input type="hidden" name="ReturnUrl" value={returnUrl ?? ''} />
                <p>
                    {externalLogins.map((provider) => (
                        <button
                            key={provider.name}
                            type="submit"
                            className="btn btn-primary"
                            name="provider"
                            value={provider.name}
                            title={`Log in using your ${provider.displayName} account`}>
                            {provider.displayName}
                        </button>
                    ))}
                </p>
            </div>
        </form>
    );
}
