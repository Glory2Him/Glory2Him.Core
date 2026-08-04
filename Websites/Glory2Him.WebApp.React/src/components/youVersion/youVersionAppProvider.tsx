import { ReactNode } from 'react';
import { YouVersionProvider } from '@youversion/platform-react-ui';
import { frontendConfigurationService } from '../../services/foundations/frontendConfigurationService';

// Wraps the app in the YouVersion Platform SDK provider once the app key arrives from
// /api/frontend-configurations. The SDK injects its own CSS through the provider, so no
// stylesheet import is needed. YouVersion user authentication is deliberately not enabled
// (no includeAuth / authRedirectUrl) — the site shows Bible content only.
//
// The children always render: while the key is loading, or when no key is configured, the
// app runs without the provider and the Bible pages degrade to an inline message instead
// of crashing (see useYouVersionAvailability).
export function YouVersionAppProvider({ children }: { children: ReactNode }) {
    const { data } = frontendConfigurationService.useGetFrontendConfiguration();
    const appKey = data?.youVersionAppKey?.trim() ?? '';

    if (appKey === '') {
        return <>{children}</>;
    }

    return (
        <YouVersionProvider appKey={appKey} theme="light">
            {children}
        </YouVersionProvider>
    );
}

// The one message every Bible page shows when no YouVersion app key is configured.
// (The matching availability check lives in src/hooks/useYouVersionAvailability.ts,
// kept out of this file so fast refresh sees only component exports here.)
export function YouVersionUnavailableMessage() {
    return (
        <div className="alert alert-warning" role="alert">
            Bible content is unavailable — no YouVersion app key is configured.
        </div>
    );
}
