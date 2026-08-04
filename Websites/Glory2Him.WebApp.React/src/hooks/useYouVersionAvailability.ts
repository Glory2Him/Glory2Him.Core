import { frontendConfigurationService } from '../services/foundations/frontendConfigurationService';

// Tells a page whether YouVersion Bible content can render right now. The underlying
// react-query result is cached (staleTime: Infinity), so this shares one request with
// YouVersionAppProvider rather than firing its own.
export function useYouVersionAvailability(): { isLoading: boolean; isAvailable: boolean } {
    const { data, isPending } = frontendConfigurationService.useGetFrontendConfiguration();
    const appKey = data?.youVersionAppKey?.trim() ?? '';

    return { isLoading: isPending, isAvailable: appKey !== '' };
}
