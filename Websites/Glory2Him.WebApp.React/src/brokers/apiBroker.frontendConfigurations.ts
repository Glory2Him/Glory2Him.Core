import { FrontendConfigurationView } from "../models/frontendConfigurations/frontendConfigurationView";
import ApiBroker from "./apiBroker";

class FrontendConfigurationBroker {
    relativeFrontendConfigurationsUrl = '/api/frontend-configurations';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetFrontendConfigurationAsync(): Promise<FrontendConfigurationView> {
        const result = await this.apiBroker.GetAsync(this.relativeFrontendConfigurationsUrl);

        return result.data as FrontendConfigurationView;
    }
}

export default FrontendConfigurationBroker;
