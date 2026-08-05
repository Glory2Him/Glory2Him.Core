import { useQuery } from '@tanstack/react-query';
import FrontendConfigurationBroker from "../../brokers/apiBroker.frontendConfigurations";
import { FrontendConfigurationView } from "../../models/frontendConfigurations/frontendConfigurationView";

export const frontendConfigurationService = {
    useGetFrontendConfiguration: () => {
        const frontendConfigurationBroker = new FrontendConfigurationBroker();

        return useQuery<FrontendConfigurationView>({
            queryKey: ["FrontendConfigurationsGet"],
            queryFn: async () =>
                await frontendConfigurationBroker.GetFrontendConfigurationAsync(),
            staleTime: Infinity
        });
    }
};
